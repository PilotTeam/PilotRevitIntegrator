using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Ascon.Pilot.SDK.Extensions;
using Newtonsoft.Json;
using PilotRevitAddin;

namespace Ascon.Pilot.SDK.RevitShareAgregator
{
    [Export(typeof(IDataPlugin))]
    public class RevitShareAgregator : IDataPlugin, IHandle<UnloadedEventArgs>
    {
        private readonly IObjectsRepository _repository;
        private readonly int _currentPersonId;
        private string _sharePath;
        private Dictionary<string, string> _revitProjectAttrsMap;
        private NamedPipeServerStream _updateSettingsPipeServer;

        [ImportingConstructor]
        public RevitShareAgregator(IObjectsRepository repository, IPersonalSettings personalSettings, IEventAggregator eventAggregator)
        {
            _repository = repository;
            _currentPersonId = _repository.GetCurrentPerson().Id;
            personalSettings.SubscribeSetting(SettingsFeatureKeys.RevitAgregatorProjectPathKey).Subscribe(p => _sharePath = p.Value);
            personalSettings.SubscribeSetting(SettingsFeatureKeys.RevitProjectInfoKey).Subscribe(p => _revitProjectAttrsMap = GetRevitProjectAttrsMap(p.Value));
            eventAggregator.Subscribe(this);
            _repository.SubscribeNotification(NotificationKind.StorageObjectCreated).Subscribe(OnNext, OnError);
            Task.Factory.StartNew(StartListeningUpdateSettingsCommand);
        }

        public void OnNext(INotification notification)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => HandleNotification(notification)));
        }

        private async void HandleNotification(INotification notification)
        {
            var token = new CancellationToken();
            var obj = await _repository.AsyncMethods().GetObjectsAsync(new[] { notification.ObjectId }, CreateNode, token);
            var dataObject = obj.FirstOrDefault();
            var isRvt = dataObject != null && dataObject.ActualFileSnapshot.Files.Any(p => p.Name.Contains(".rvt"));
            if (!isRvt)
                return;

            if (!_currentPersonId.Equals(notification.UserId))
                return;

            var storageFilePath = _repository.GetStoragePath(dataObject.Id);
            var filePathWithoutDrive = storageFilePath.Substring(Path.GetPathRoot(storageFilePath).Length);
            if (_sharePath == null)
            {
                MessageBox.Show("Share path is null");
                return;
            }
            var serializedProject = GetSerializedProject(storageFilePath, Path.Combine(_sharePath, filePathWithoutDrive), dataObject.Id);

            using (var namedPipeClient = new NamedPipeClientStream(".", "PilotRevitAddinPipe"))
            {
                try
                {
                    namedPipeClient.Connect(10000);
                }
                catch (TimeoutException)
                {
                    return;
                }
                var messageBytes = Encoding.UTF8.GetBytes(serializedProject);
                namedPipeClient.Write(messageBytes, 0, messageBytes.Length);
            }
        }
       
        private void HandleRevitRequest(NamedPipeServerStream namedPipeServer, string modelPath)
        {
            var iniPath = modelPath + ".ini";

            Guid objId;
            using (var reader = new StreamReader(iniPath))
            {
                var line = reader.ReadToEnd();
                objId = new Guid(line);
            }
            
            var serialisedProject = GetSerializedProject(_repository.GetStoragePath(objId), modelPath, objId);
            var messageBytes = Encoding.UTF8.GetBytes(serialisedProject);
            namedPipeServer.Write(messageBytes, 0, messageBytes.Length);
        }


        private string GetSerializedProject(string storageFilePath, string centralModelPath, Guid objId)
        {
            var project = GetProject(storageFilePath);
            var projectAttributes = project.DataObject.Attributes;
            var revitattrsMap = new Dictionary<string, string>();

            if (_revitProjectAttrsMap != null)
            {
                foreach (var pilotAttr in projectAttributes)
                {
                    var revitAttrs = _revitProjectAttrsMap.Where(m => m.Value.Equals(pilotAttr.Key));
                    foreach (var revitAttr in revitAttrs.Where(revitAttr => revitAttr.Key != null))
                    {
                        revitattrsMap[revitAttr.Key] = pilotAttr.Value.ToString();
                    }
                }
            }

            var revitProject = new RevitProject()
            {
                CentralModelPath = centralModelPath,
                PilotObjectId = objId.ToString(),
                ProjectInfoAttrsMap = revitattrsMap
            };

            var serializedProject = JsonConvert.SerializeObject(revitProject);
            return serializedProject;
        }

        private Dictionary<string, string> GetRevitProjectAttrsMap(string projectInfoMap)
        {
            try
            {
                var xmlSettings = XElement.Parse(projectInfoMap);
                var result = xmlSettings.Descendants("setting").ToDictionary(el => el.Attribute("revit").Value.ToString(), el => el.Attribute("pilot").Value.ToString());
                return result;
            }
            catch
            {
            }
            return null;
        }

        private IStorageDataObject GetProject(string storageFilePath)
        {
            var projectPath = GetProjectFolder(storageFilePath);
            var projectObject = _repository.GetStorageObjects(new[] { projectPath }).FirstOrDefault();
            return projectObject;
        }

        private static string GetProjectFolder(string path)
        {
            var pathes = path.Split(Path.DirectorySeparatorChar);
            return path.Substring(0, pathes[0].Length + pathes[1].Length + 1);
        }
        
        private static string ProcessSingleReceivedMessage(NamedPipeServerStream namedPipeServer)
        {
            var messageBuilder = new StringBuilder();
            var messageBuffer = new byte[853];
            do
            {
                namedPipeServer.Read(messageBuffer, 0, messageBuffer.Length);
                var messageChunk = Encoding.UTF8.GetString(messageBuffer).TrimEnd((char)0);
                messageBuilder.Append(messageChunk);
                messageBuffer = new byte[messageBuffer.Length];
            }
            while (!namedPipeServer.IsMessageComplete);
            return messageBuilder.ToString();
        }
        
        private void StartListeningUpdateSettingsCommand()
        {
            _updateSettingsPipeServer = new NamedPipeServerStream("PilotRevitAddinUpdateSettingsPipe",
                PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);
            _updateSettingsPipeServer.BeginWaitForConnection(UpdateSettings, null);
        }

        private void UpdateSettings(IAsyncResult ar)
        {
            try
            {
                _updateSettingsPipeServer.EndWaitForConnection(ar);

                var modelPath = ProcessSingleReceivedMessage(_updateSettingsPipeServer);
                if (string.IsNullOrEmpty(modelPath))
                    return;
                HandleRevitRequest(_updateSettingsPipeServer, modelPath);
                _updateSettingsPipeServer.Disconnect();
                _updateSettingsPipeServer.BeginWaitForConnection(UpdateSettings, null);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        
        public void Handle(UnloadedEventArgs message)
        {
            _updateSettingsPipeServer?.Close();
        }

        private IDataObject CreateNode(IDataObject dataObject)
        {
            return dataObject;
        }

        private void OnError(Exception obj)
        {

        }
    }
}
using log4net;
using Newtonsoft.Json;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;
using PilotRevitShareListener.Server;
using Ascon.Pilot.Common;
using Ascon.Pilot.SharedProject;

namespace PilotRevitShareListener
{
    public class PipeServer
    {
        private readonly ILog _logger;
        private NamedPipeServerStream _pipe;
        private StreamString _pipeStream;
        private Settings _settings;
        private RevitShareListener _revitShareListener;
        private ReaderWriter _readerWriter;
        private ConnectProvider _reconnectProvider;
        private ObjectUploader _objectUploader;

        private object threadLock = new object();

        private bool _isWaitingResponse;

        public PipeServer(ILog logger, ReaderWriter readerWriter,ConnectProvider reconnectProvider, ObjectUploader objectUploader, RevitShareListener revitShareListener)
        {
            _logger = logger;
            _settings = readerWriter.Settings;
            _readerWriter = readerWriter;
            _reconnectProvider = reconnectProvider;
            _objectUploader = objectUploader;
            _revitShareListener = revitShareListener;
            _isWaitingResponse = false;
        }

        public void Start()
        {
            _logger.InfoFormat("Pipe server start");
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            _pipe = new NamedPipeServerStream("rsl_admin", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, pipeSecurity);
            _logger.InfoFormat("Pipe server wait connection");
            _pipe.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), _pipe);
        }

        public void SendToAdminClient(string message)
        {
            lock (threadLock)
            {
                if (_isWaitingResponse && _pipeStream != null)
                {
                    _pipeStream.SendCommand(message);
                    _logger.InfoFormat("Pipe send message");
                }
                _isWaitingResponse = false;

                _pipe.Close();
                _pipe = null;
                _logger.InfoFormat("Pipe server closed");
                Start();
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            _logger.InfoFormat("Pipe connection callback");
            NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
            pipeServer.EndWaitForConnection(iar);
            _isWaitingResponse = true;

            _pipeStream = new StreamString(pipeServer);
            var stringData = _pipeStream.ReadAnswer();        
            var command = JsonConvert.DeserializeObject<PipeCommand>(stringData);

            string result;
            switch (command.commandName)
            {
                case "--version":
                    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    SendToAdminClient("PilotRvtShareListener service version: " + version.ToString());
                    break;
                case "--getLicenseCode":
                    SendToAdminClient("license code is " + _settings.LicenseCode);
                    break;
                case "--setLicenseCode":
                    result =  SetLicenseCode(command);
                    SendToAdminClient(result);
                    break;
                case "--getPath":
                    result = GetPath();
                    SendToAdminClient(result);
                    break;
                case "--setPath":
                    result = SetPath(command.args["path"]);
                    SendToAdminClient(result);
                    break;
                case "--getDelay":
                    SendToAdminClient("delay: " + _settings.Timeout + " mls");
                    break;
                case "--setDelay":
                    result = SetDelay(command);
                    SendToAdminClient(result);
                    break;
                case "--connection":
                    result = CheckConnection();
                    SendToAdminClient(result);
                    break;
                case "--disconnect":
                    result = Disconnect();  
                    SendToAdminClient(result);
                    break;
                case "--connect":
                    result = Connect(command);
                    SendToAdminClient(result);
                    break;
                default:
                    break;
            }

        }

        private string Disconnect()
        {
            if (!_reconnectProvider.CheckConnection())
                return "already disconnected";
            _reconnectProvider.Disconnect();
            return "disconnected to server";
        }

        private string Connect(PipeCommand command)
        {
            if (_settings.SharePath == "")
                return "Revit shared folder path is not set";

            _reconnectProvider.Reconnect(command);
            if(_reconnectProvider.CheckConnection())
            { 
                 CreateNewListener();
                _readerWriter.Write();
                return "connection is successful";
            }
            else
            {
                _logger.InfoFormat("connection failed: " + _reconnectProvider.ExceptionMessage);
                return "connection failed: " + _reconnectProvider.ExceptionMessage;
            }
        }

        private void CreateNewListener()
        {
            _revitShareListener?.Dispose();
            _revitShareListener = new RevitShareListener(_objectUploader, _settings);
        }

        private string CheckConnection()
        {
            if (_reconnectProvider.CheckConnection())
                return "connected to " + _settings.ServerUrl + "/" + _settings.DbName;
            else
                return "disconnected to server";
        }
        private string SetDelay(PipeCommand command)
        {
            try
            {
                _settings.Timeout = Double.Parse(command.args["delay"]);
                CreateNewListener();
                _readerWriter.Write();
                _logger.InfoFormat("delay successfully changed");
                return "delay successfully changed";
            }
            catch (FormatException ex)
            {
                _logger.InfoFormat("failed: " + ex.Message);
                return "failed: " + ex.Message;
            }
        }

        private string SetPath(string path)
        {
            string shareBuffer = _settings.SharePath;
            _settings.SharePath = path;
            try
            {
                CreateNewListener();
                _readerWriter.Write();
                _logger.InfoFormat("path to Revit share folder successfully changed");
                return "path to Revit share folder successfully changed";
            }
            catch (Exception ex)
            {
                _settings.SharePath = shareBuffer;
                _logger.InfoFormat("failed: " + ex.Message);
                return "failed: " + ex.Message;
            }
        }

        private string GetPath()
        {
            if (_settings.SharePath != "")
                return "Revit shared folder path: " + _settings.SharePath;
            else
                return "Revit shared folder path is not set";
        }

        private string SetLicenseCode(PipeCommand command)
        {
            int codeBuffer = _settings.LicenseCode;
            try
            {
                _settings.LicenseCode = Int16.Parse(command.args["licensecode"]);
            }
            catch (Exception ex)
            {
                _settings.LicenseCode = codeBuffer;
                return "failed: " + ex.Message;
            }
            _readerWriter.Write();
            _reconnectProvider.Reconnect(); 
            if (_reconnectProvider.CheckConnection())
            {
                _readerWriter.Write();
                _logger.InfoFormat("license code successfully changed");
                return "license code successfully changed";
            }
            else
            {
                _logger.InfoFormat("license code changed");
                return "license code changed, but connection failed: " + _reconnectProvider.ExceptionMessage;
            }
        }
    }
}

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
        private RemoteStorageThread _remoteStorageThread;
        private Settings _settings;
        private RevitShareListener _revitShareListener;
        private ObjectUploader _objectUploader;
        private ReaderWriter _readerWriter;

        private object threadLock = new object();

        private bool _isWaitingResponse;

        public PipeServer(ReaderWriter readerWriter , RemoteStorageThread remoteStorageThread,  RevitShareListener revitShareListener ,ObjectUploader objectUploader, ILog logger)
        {
            _objectUploader = objectUploader;
            _remoteStorageThread = remoteStorageThread;
            _revitShareListener = revitShareListener;
            _logger = logger;
            _settings = readerWriter.Settings;
            _readerWriter = readerWriter;
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

        private void SendToAdminClient(string message)
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
                    if (_settings.SharePath == "")
                    {
                        SendToAdminClient("Revit shared folder path is not set");
                        break;
                    }
                    result = Reconnect(command);
                    SendToAdminClient(result);
                    break;
                default:
                    break;
            }

        }

        private string Reconnect(PipeCommand command)
        {
            object[] dataBuffer = new object[] { _settings.ServerUrl, _settings.DbName, _settings.Login, _settings.Password };
            string[] subs = SplitUrl(command.args["url"]);
            if (subs == null)
            {
                return "incorrect url";
            }

            _settings.ServerUrl = subs[0];
            _settings.DbName = subs[1];
            _settings.Login = command.args["login"];
            _settings.Password = command.args["password"].EncryptAes();

            bool result = Reconnect();
            if (result)
            {
                CreateListener();
                _readerWriter.Write();
                return "connection is successful";
            }
            else
            {
                _settings.ServerUrl = (string)dataBuffer[0];
                _settings.DbName = (string)dataBuffer[1];
                _settings.Login = (string)dataBuffer[2];
                _settings.Password = (string)dataBuffer[3];
                return "connection failed: " + _remoteStorageThread.ExceptionMessage;
            }
        }

        private bool Reconnect()
        {
            Disconnect();
            _remoteStorageThread.Start();
            WaitForThread();

            if (_remoteStorageThread.CheckConnection())
            {
                _logger.InfoFormat("connected to server");
                return true;
            }
            else
            {
                _logger.InfoFormat("disconnected to server");
                return false;
            }
        }

        private string Disconnect()
        {
            if (!_remoteStorageThread.CheckConnection())
            {
                return "already disconnected";
            }
            _remoteStorageThread.Stop();
            _logger.InfoFormat("disconnected to server");
            return "disconnected to server";
        }

        private string CheckConnection()
        {
            if (_remoteStorageThread.CheckConnection())
                return "connected to " + _settings.ServerUrl + "/" + _settings.DbName;
            else
               return "disconnected to server";
        }

        private string SetDelay(PipeCommand command)
        {
            try
            {
                _settings.Timeout = Double.Parse(command.args["delay"]);
                CreateListener();
                _readerWriter.Write();
                return "delay successfully changed";
            }
            catch (FormatException ex)
            {
                return "failed: " + ex.Message;
            }
        }

        private string SetPath(string path)
        {
            string shareBuffer = _settings.SharePath;
            _settings.SharePath = path;
            try
            {
                CreateListener();
                _readerWriter.Write();
                return "path to Revit share folder successfully changed";
            }
            catch (Exception ex)
            {
                _settings.SharePath = shareBuffer;
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
            bool result = Reconnect(); 
            if (result)
            {
                _logger.InfoFormat("license code successfully changed");
                return "license code successfully changed";
            }
            else
            {
                _logger.InfoFormat("license code changed");
                return "license code changed, but connection failed: " + _remoteStorageThread.ExceptionMessage;
            }
        }

        private void WaitForThread()
        {
            while (_remoteStorageThread.Thread.ThreadState != (ThreadState.Background | ThreadState.WaitSleepJoin))
            {
                Thread.Sleep(1000);
            }
        }

        private void CreateListener()
     {
            _revitShareListener?.Dispose();
            _revitShareListener = new RevitShareListener(_objectUploader, _settings);
        }
     private string[] SplitUrl (string url)
     {
            int i;
            for (i = url.Length -1; i > 0; i--)
                if (url[i] == '/' || url[i] == '\\')
                    break;

            if (i < 8)
                return null;
            string[] subs = new string [2];
            subs[0] = url.Remove(i, url.Length -i );
            subs[1] = url.Remove(0, subs[0].Length+1); 
           return subs;
     }
    }
}

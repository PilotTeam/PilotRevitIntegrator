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
        private NamedPipeServerStream _pipe;
        private StreamString _pipeStream;
        private RemoteStorageThread _remoteStorageThread;
        private Settings _settings;
        private RevitShareListener _revitShareListener;
        private readonly ILog _logger;
        private ObjectUploader _objectUploader;
        private ReaderWriter _rw;

        private object threadLock = new object();

        private bool _isWaitingResponse;

        public PipeServer(ReaderWriter rw , RemoteStorageThread remoteStorageThread,  RevitShareListener revitShareListener ,ObjectUploader objectUploader, ILog logger)
        {
            _objectUploader = objectUploader;
            _remoteStorageThread = remoteStorageThread;
            _revitShareListener = revitShareListener;
            _logger = logger;
            _settings = rw.settings;
            _rw = rw;
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
                    int codeBuffer = _settings.LicenseCode;
                    try
                    {
                        _settings.LicenseCode = Int16.Parse(command.args["licensecode"]);
                    }
                    catch (Exception ex)
                    {
                        _settings.LicenseCode = codeBuffer;
                        SendToAdminClient("failed: " + ex.Message);
                        break;
                    }
                    if (_remoteStorageThread.IsConnected())
                    {
                        _remoteStorageThread.Stop();
                        _logger.InfoFormat("Disconnected to server");
                    }
                    _remoteStorageThread.Start();
                    WaitThread();

                    if (_remoteStorageThread.IsConnected())
                    {
                        CreateListener();
                        _logger.InfoFormat("connected to server");
                        _logger.InfoFormat("license code successfully changed");
                        SendToAdminClient("license code successfully changed");
                    }
                    else
                    {
                        _logger.InfoFormat("disconnected to server");
                        _logger.InfoFormat("license code changed");
                        SendToAdminClient("license code changed, but connection failed: " + _remoteStorageThread.ExeptionMsg);
                    }
                    break;
                case "--getPath":
                    if (_settings.SharePath != "")
                        SendToAdminClient("Revit shared folder path: " + _settings.SharePath);
                    else
                        SendToAdminClient("Revit shared folder path is not set");
                    break;
                case "--setPath":
                    string shareBuffer = _settings.SharePath;
                    _settings.SharePath = command.args["path"];
                    try
                    {
                        CreateListener();
                        SendToAdminClient("path to Revit share folder successfully changed");
                    }
                    catch (Exception ex)
                    {
                        _settings.SharePath = shareBuffer;
                        SendToAdminClient("failed: " + ex.Message);
                    }
                    break;

                case "--getDelay":
                    SendToAdminClient("delay: " + _settings.Timeout + " mls");
                    break;
                case "--setDelay":
                    try
                    {
                        _settings.Timeout = Double.Parse(command.args["delay"]);
                        CreateListener();
                        SendToAdminClient("time out successfully changed");
                    }
                    catch (FormatException ex)
                    {
                        SendToAdminClient("failed: " + ex.Message);
                    }
                    break;
                case "--connection":
                    if (_remoteStorageThread.IsConnected())
                        SendToAdminClient("connected to " + _settings.ServerUrl + "/" + _settings.DbName);
                    else
                        SendToAdminClient("disconnected to server");
                    break;

                case "--disconnect":
                    if (!_remoteStorageThread.IsConnected())
                    {
                        SendToAdminClient("already disconnected");
                        break;
                    }
                    _remoteStorageThread.Stop();
                    _logger.InfoFormat("disconnected to server");
                    SendToAdminClient("disconnection is successful");
                    break;
                case "--connect":

                    if (_settings.SharePath == "")
                    {
                        SendToAdminClient("Revit shared folder path is not set");
                        break;
                    }

                    object[] dataBuffer = new object[] { _settings.ServerUrl, _settings.DbName, _settings.Login, _settings.Password };
                    if (_remoteStorageThread.IsConnected())
                    {
                        _remoteStorageThread.Stop();
                        _logger.InfoFormat("Disconnected to server");
                    }
                    string[] subs = SplitUrl(command.args["url"]);//gets separate server url and database name
                    if (subs == null)
                    {
                        SendToAdminClient("incorrect url");
                        break;
                    }
                    _settings.ServerUrl = subs[0];
                    _settings.DbName = subs[1];
                    _settings.Login = command.args["login"];
                    _settings.Password = command.args["password"].EncryptAes();

                    _remoteStorageThread.Start();
                    WaitThread();

                    if (_remoteStorageThread.IsConnected())
                    {
                        CreateListener();
                        _logger.InfoFormat("connected to server");
                        SendToAdminClient("connection is successful");
                    }
                    else
                    {
                        _logger.InfoFormat("disconnected to server");
                        _settings.ServerUrl = (string)dataBuffer[0];
                        _settings.DbName = (string)dataBuffer[1];
                        _settings.Login = (string)dataBuffer[2];
                        _settings.Password = (string)dataBuffer[3];
                        SendToAdminClient("connection failed: " + _remoteStorageThread.ExeptionMsg);
                    }
                    break;
                default:
                    break;
            }

        }

        private void WaitThread()
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
            _rw.Write(_settings);
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

using System;
using System.Threading;
using log4net;
using Ascon.Pilot.SharedProject;
using Ascon.Pilot.Common;
using Ascon.Pilot.Server.Api;

namespace PilotRevitShareListener.Server
{
    public class ConnectProvider : IConnectionLostListener
    {
        private readonly ILog _logger;
        private readonly object _lock;
        private IServerConnector _serverConnector;
        private Settings _settings;
        private bool _isConnected;

        public ConnectProvider(ILog logger , Settings settings, IServerConnector serverConnector)
        {
            _lock = new object();
            _logger = logger;
            _settings = settings;
            _serverConnector = serverConnector;
            _isConnected = false;
        }

        public string ExceptionMessage { get; private set;}

        public bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected;
                }

            }
            private set
            {
                lock (_lock)
                {
                    _isConnected = value;
                }
            }
        }

        public bool Reconnect(PipeCommand command)
        {
            object[] dataBuffer = new object[] { _settings.ServerUrl, _settings.DbName, _settings.Login, _settings.Password };
            string[] subs = SplitUrl(command.args["url"]);
            if (subs == null)
            {
                subs = new string[] { command.args["url"], "" }; //if database name wasn't typed
            }
            
            _settings.ServerUrl = subs[0];
            _settings.DbName = subs[1];
            _settings.Login = command.args["login"];
            _settings.Password = command.args["password"].EncryptAes();

            if (!Reconnect())
            { 
                _settings.ServerUrl = (string)dataBuffer[0];
                _settings.DbName = (string)dataBuffer[1];
                _settings.Login = (string)dataBuffer[2];
                _settings.Password = (string)dataBuffer[3];
                return false;
            }
            return true;
        }

        public bool Reconnect()
        {
            try
            {
                Disconnect();
                Connect();
                return true;
            }catch(Exception ex)
            {
                _logger.InfoFormat("ex.Message");
                ExceptionMessage = ex.Message;
                return false;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _serverConnector.Disconnect();
            IsConnected = false;
            _logger.InfoFormat("disconnected to server");
        }

        public void Connect()
        {
            _serverConnector.Connect(this);
            IsConnected = true;
            _logger.InfoFormat("connected to server");
        }

        public void ConnectionLost(Exception ex = null)
        {
            IsConnected = false;
        }

        private string[] SplitUrl(string url)
        {
            int i;
            for (i = url.Length - 1; i > 0; i--)
                if (url[i] == '/' || url[i] == '\\')
                    break;

            if (i < 8)
                return null;
            string[] subs = new string[2];
            subs[0] = url.Remove(i, url.Length - i);
            subs[1] = url.Remove(0, subs[0].Length + 1);
            return subs;
        }
    }
}

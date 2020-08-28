using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ascon.Pilot.Core;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;
using Ascon.Pilot.Transport;

namespace PilotRevitShareListener
{
    public class HttpPilotClient : IImplementationFactory, IDisposable
    {
        private readonly TransportClient _client;
        private readonly CallbackReceiverAdapter _transportCallback;
        protected readonly Marshaller _marshaller;
        private readonly Unmarshaller _unmarshaller;
        protected IServerUpdateCallback _updateCallback;
        private IServerCallback _serverCallback;
        private IServerAdminCallback _adminCallback;
        private bool _isBroken;
        private IConnectionLostListener _connectionLostListener;
        private static readonly Version ClientVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public HttpPilotClient()
        {
            _client = new TransportClient();
            _marshaller = new Marshaller(new CallServiceAdapter(_client));
            _unmarshaller = new Unmarshaller(this);
            _transportCallback = new CallbackReceiverAdapter(_unmarshaller, CallbackError);
        }

        public void SetConnectionLostListener(IConnectionLostListener connectionLostListener)
        {
            _connectionLostListener = connectionLostListener;
        }

        private void CallbackError()
        {
            if (_connectionLostListener != null)
                _connectionLostListener.ConnectionLost(new TransportException("Client connection is disconnected by server."));
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public void BreakConnection()
        {
            _isBroken = true;
            _client.Disconnect();
        }

        public void RestoreConnection()
        {
            _isBroken = false;
        }

        private void CheckBroken()
        {
            if (_isBroken)
                throw new TransportException("Connection is not available");
        }

        public void Connect(string url, IWebProxy proxy = null)
        {
            ConnectBase(url, proxy);
            _client.OpenCallback(_transportCallback);
        }

        protected void ConnectBase(string url, IWebProxy proxy = null)
        {
            CheckBroken();
            _client.SetProxy(proxy);
            _client.Connect(url);
        }

        public virtual void Connect(ConnectionCredentials credentials)
        {
            Connect(credentials.GetConnectionString(), credentials.GetConnectionProxy());
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public IServerApi GetServerApi(IServerCallback callback)
        {
            _serverCallback = callback;
            return _marshaller.Get<IServerApi>();
        }

        public IServerAdminApi GetServerAdminApi(IServerAdminCallback callback)
        {
            _adminCallback = callback;
            return _marshaller.Get<IServerAdminApi>();
        }

        protected IServerUpdateApi GetServerUpdateApi(IServerUpdateCallback callback)
        {
            _updateCallback = callback;
            return _marshaller.Get<IServerUpdateApi>();
        }

        public IImportApi GetImportApi()
        {
            return _marshaller.Get<IImportApi>();
        }

        public IFileArchiveApi GetFileArchiveApi(string database)
        {
            return new DataBaseNameSubstitutor(_marshaller.Get<IFileArchiveApi>(), database);
        }

        private class CallServiceAdapter : ICallService
        {
            private readonly TransportClient _client;
            public CallServiceAdapter(TransportClient client)
            {
                _client = client;
            }

            public byte[] Call(byte[] data)
            {
                return _client.Call(data);
            }
        }

        public object GetImplementation(string interfaceName)
        {
            if (interfaceName == "IServerCallback")
                return _serverCallback;
            if (interfaceName == "IServerAdminCallback")
                return _adminCallback;
            if (interfaceName == "IServerUpdateCallback")
                return _updateCallback;
            throw new NotImplementedException(interfaceName);
        }
         
        private class CallbackReceiverAdapter : ICallbackReceiver
        {
            private readonly Unmarshaller _unmarshaller;
            private readonly Action _action;

            public CallbackReceiverAdapter(Unmarshaller unmarshaller, Action action)
            {
                _unmarshaller = unmarshaller;
                _action = action;
            }

            public void Receive(byte[] data)
            {
                _unmarshaller.Call(data);
            }

            public void Error()
            {
                _action.Invoke();
            }
        }

        private class DataBaseNameSubstitutor : IFileArchiveApi
        {
            private readonly IFileArchiveApi _api;
            private readonly string _database;

            public DataBaseNameSubstitutor(IFileArchiveApi api, string database)
            {
                _database = database;
                _api = api;
            }

            public byte[] GetFileChunk(string databaseName, Guid id, long pos, int count)
            {
                return _api.GetFileChunk(_database, id, pos, count);
            }

            public void PutFileChunk(string databaseName, Guid id, byte[] buffer, long pos)
            {
                _api.PutFileChunk(_database, id, buffer, pos);
            }

            public long GetFilePosition(string databaseName, Guid id)
            {
                return _api.GetFilePosition(_database, id);
            }

            public void PutFileInArchive(string databaseName, DFileBody fileBody)
            {
                _api.PutFileInArchive(_database, fileBody);
            }
        }
    }
}

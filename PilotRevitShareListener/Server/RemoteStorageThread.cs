using System;
using System.Collections.Generic;
using System.Threading;
using Ascon.Pilot.Core;
using Ascon.Pilot.Transport;
using log4net;

namespace PilotRevitShareListener.Server
{
    public class RemoteStorageThread
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RemoteStorageThread));
        private static int ReconnectTimeout = 5000;

        private readonly IServerConnector _serverConnector;

        private readonly Queue<Action> _actions = new Queue<Action>();
        private bool _isConnected;

        public RemoteStorageThread(IServerConnector serverConnector)
        {
            _serverConnector = serverConnector;
        }

        public void Start()
        {
            var thread = new Thread(Processing) { IsBackground = true, Name = GetType().Name };
            thread.Start();
            Enqueue(() => { });
        }

        public void Stop()
        {
            lock (_actions)
            {
                _actions.Clear();
                _actions.Enqueue(null);
                Monitor.Pulse(_actions);
            }
        }

        public void ForceOffline()
        {
            Stop();
            Disconnect();
        }

        private void Processing()
        {
            Action action = Peek();
            while (action != null)
            {
                if (!_isConnected)
                    Connect();

                if (!_isConnected)
                {
                    Thread.Sleep(ReconnectTimeout);
                }
                else
                {
                    Execute(action);
                    if (_isConnected)
                        Remove(action);
                }
                action = Peek();
            }

            if (_isConnected)
                Disconnect();
        }

        private void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (TransportException)
            {
                _isConnected = false;
            }
            catch (PilotServerOfflineException)
            {
                _isConnected = false;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Remove(Action action)
        {
            lock (_actions)
            {
                if (_actions.Count == 0)
                    return;
                if (_actions.Peek() == action)
                    _actions.Dequeue();
            }
        }

        private Action Peek()
        {
            lock (_actions)
            {
                while (_actions.Count == 0)
                    Monitor.Wait(_actions);
                return _actions.Peek();
            }
        }

        private void Connect()
        {
            try
            {
                _serverConnector.Connect();
                _isConnected = true;
            }
            catch (TransportException)
            {
                Disconnect();
            }
            catch (PilotServerOfflineException)
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Logger.Error("Возникло иключение, блокирующее попытки переподключения к серверу", ex);
                ForceOffline();
            }
        }

        private void Disconnect()
        {
            try
            {
                _serverConnector.Disconnect();
                _isConnected = false;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Enqueue(Action action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
                Monitor.Pulse(_actions);
            }
        }
    }
}
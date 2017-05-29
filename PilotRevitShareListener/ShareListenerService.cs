using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using log4net;
using PilotRevitShareListener.Server;

namespace PilotRevitShareListener
{
    public partial class ShareListenerService : ServiceBase
    {
        private static ILog _logger;

        private RevitShareListener _revitShareListener;
        private IServerConnector _serverConnector;
        private Settings _settings;
        private RemoteStorageThread _remoteStorage;

        public ShareListenerService()
        {
            InitializeComponent();
        }

        public void Start(ILog logger)
        {
            _logger = logger;
            try
            {
                _settings = SettingsReader.Read();
                _serverConnector = new ServerConnector(_settings);

                _remoteStorage = new RemoteStorageThread(_serverConnector);
                _remoteStorage.Start();

                var objectModifier = new ObjectModifier(_serverConnector, _serverConnector.PersonId);
                var objectUploader = new ObjectUploader(_remoteStorage, objectModifier, _serverConnector);

                _revitShareListener = new RevitShareListener(objectUploader, _settings);

                _logger.InfoFormat("{0} Started Successfully", ServiceName);
            }
            catch (Exception ex)
            {
                _logger.Error("OnStart Failed", ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            _logger = LogManager.GetLogger(typeof(ShareListenerService));
            _logger.InfoFormat("{0} OnStart", ServiceName);
            SubscribeOnUnhandledExceptions();
            Start(_logger);
        }

        protected override void OnStop()
        {
            _logger.InfoFormat("{0} OnStop", ServiceName);

            try
            {
                _revitShareListener?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error("OnStop Failed", ex);
            }
        }

        protected override void OnShutdown()
        {
            _logger.InfoFormat("{0} OnShutdown", ServiceName);

            try
            {
                _revitShareListener?.Dispose();
                _logger.InfoFormat("{0} Shutdown Successfully", ServiceName);
            }
            catch (Exception ex)
            {
                _logger.Error("OnShutdown", ex);
            }
        }

        private static void SubscribeOnUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    _logger.Error(e.ExceptionObject);
                }
                catch (IOException)
                {
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    _logger.Error("UnobservedTaskException", e.Exception);
                }
                catch (IOException)
                {
                }
                e.SetObserved();
            };

#if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceException;
#endif
        }

        private static void HandleFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException -= HandleFirstChanceException;
            try
            {
                _logger.Warn(e.ToString(), e.Exception);
            }
            catch (IOException)
            {
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceException;
            }
        }
    }
}

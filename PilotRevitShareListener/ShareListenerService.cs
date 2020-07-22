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
        private ObjectUploader _objectUploader;
        private ConnectProvider _connectProvider;
        private PipeServer _pipeServer;

        public ShareListenerService()
        {
            InitializeComponent();
        }

        public async void Start(ILog logger)
        {
            _logger = logger;
            ReaderWriter readerWriter = new ReaderWriter(ServiceName);
            _settings = readerWriter.Read();
            try
            {
                _serverConnector = new ServerConnector(_settings);

                var objectModifier = new ObjectModifier(_serverConnector);
                _objectUploader = new ObjectUploader( objectModifier, _serverConnector);

                _connectProvider = new ConnectProvider(_logger, _settings, _serverConnector);
                await _connectProvider.ConnectAsync();

                _revitShareListener = new RevitShareListener(_objectUploader, _settings);
                
                _pipeServer = new PipeServer(_logger, readerWriter,_connectProvider,_objectUploader, _revitShareListener);
                _pipeServer.Start();

                _logger.InfoFormat("{0} Started Successfully", ServiceName);
            }
            catch (Exception)//in case of incorrect settings.xml 
            {
                _pipeServer = new PipeServer(_logger, readerWriter, _connectProvider, _objectUploader ,null);
                _pipeServer.Start();
            }
        }

        protected override void OnStart(string[] args)
        {
            _logger = LogManager.GetLogger(typeof(ShareListenerService));
            var appender = new log4net.Appender.RollingFileAppender();
            appender.AppendToFile = true;
            appender.File = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\"+ServiceName+@"\Logs\listener.log"; 
            appender.MaxFileSize = 100000;
            appender.MaxSizeRollBackups = 10;
            appender.RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Size;
            log4net.Config.BasicConfigurator.Configure(appender);
            appender.Threshold = log4net.Core.Level.All;
            appender.ActivateOptions();
            appender.Layout = new log4net.Layout.PatternLayout("%date{dd-MM-yyyy HH:mm:ss} %5level [%2thread] %message %n");
            _logger.Info("event occurred");
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

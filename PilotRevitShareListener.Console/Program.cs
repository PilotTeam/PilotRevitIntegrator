using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using log4net;

namespace PilotRevitShareListener.Console
{
    class Program
    {
        private static ShareListenerService _service;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            SubscribeOnUnhandledExceptions();
            SubscribeOnFirstChangeException();
            Logger.InfoFormat("PilotRevitShareListener.Console starting...");
            _service = new ShareListenerService();
            _service.Start(Logger);

            System.Console.ReadKey();
        }

        private static void SubscribeOnUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    Logger.Error(e.ExceptionObject);
                }
                catch (IOException)
                {
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    Logger.Error("UnobservedTaskException", e.Exception);
                }
                catch (IOException)
                {
                }
                e.SetObserved();
            };
        }

        [Conditional("DEBUG")]
        private static void SubscribeOnFirstChangeException()
        {
            AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceException;
        }

        private static void HandleFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException -= HandleFirstChanceException;
            try
            {
                Logger.Warn(e.ToString(), e.Exception);
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
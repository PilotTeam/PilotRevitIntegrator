using System.ServiceProcess;

namespace PilotRevitShareListener
{
    static class Program
    {
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new ShareListenerService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
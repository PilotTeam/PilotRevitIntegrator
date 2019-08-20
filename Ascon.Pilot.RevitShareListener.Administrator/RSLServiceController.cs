using System.ServiceProcess;

namespace Ascon.Pilot.RevitShareListener.Administrator
{
    class RSLServiceController
    {
        ServiceController _service;

        public RSLServiceController(string serviceName)
        {
            _service = new ServiceController(serviceName);
        }

        public ServiceController GetService()
        {
            return _service;
        }

        public string GetStatus()
        {
            if (_service == null)
                return "not installed";

            switch (_service.Status)
            {
                case ServiceControllerStatus.Running:
                    return "running";
                case ServiceControllerStatus.Stopped:
                    return "stopped";
                case ServiceControllerStatus.Paused:
                    return "paused";
                case ServiceControllerStatus.StopPending:
                    return "stopping";
                case ServiceControllerStatus.StartPending:
                    return "starting";
                default:
                    return "status changing";
            }
        }

    }

}

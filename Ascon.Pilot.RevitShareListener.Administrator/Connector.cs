using System;
using System.IO.Pipes;
using Ascon.Pilot.SharedProject;
using Newtonsoft.Json;

namespace Ascon.Pilot.RevitShareListener.Administrator
{
    internal class Connector
    {
        private string _pipeName;
        private RSLServiceController _service;
        public Connector(string pipeName, RSLServiceController service)
        {
            _pipeName = pipeName;
            _service = service;
        }

        public void SendToServer(PipeCommand command, int TimeOut = 5000)
        {
            try
            {
                //if (_service.GetStatus() == "stopped")
                //{
                //    Console.WriteLine( "start service first");
                //    return;
                //}

                var serializedCommand = JsonConvert.SerializeObject(command);
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                pipeStream.Connect(TimeOut);
                StreamString ss = new StreamString(pipeStream);

                ss.SendCommand(serializedCommand);
                var answer = ss.ReadAnswer();

                Console.WriteLine(answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + "[" + ex.Source + "] " + ex.Message);
            }
        }
    }
}

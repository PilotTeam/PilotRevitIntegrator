using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using Ascon.Pilot.SharedProject;
using Newtonsoft.Json;

namespace Ascon.Pilot.RevitShareListener.Administrator
{
    internal class Program
    {

        private const string SERVICE_NAME = "PilotRvtShareListener";
        private const string SERVER_NAME = "Pilot-Server";
        private static string _appName;
        private static RSLServiceController ServiceController = new RSLServiceController(SERVICE_NAME);
        private static Dictionary<string, CommandParams> commands = new Dictionary<string, CommandParams>();

        internal class CommandParams
        {
            public string Description;
            public List<string> Params = new List<string>();

            public delegate void FunctionDelegate(string[] args);
            public FunctionDelegate Function;
        }

        private static void Main(string[] args)
        {
            RegisterCommands();
            _appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            if (args.GetLength(0) > 0)
            {
                args[0] = FixLetters(args[0].ToLower());
                if (commands.ContainsKey(args[0]))
                {
                    if (ServiceController.GetStatus() == "stopped" && args[0] != "--help" &&
                        args[0] != "--status" && args[0] != "--stop" && args[0] != "--start")
                        StartService(args);
                    commands[args[0]].Function.Invoke(args);
                    return;
                }
            }

            if (ServiceController.GetStatus() == "stopped")
                StartService(args);

            commands["--help"].Function.Invoke(new string[]{ "--help"});
            commands["--getPath"].Function.Invoke(new string[] { "--getPath" });
            commands["--getDelay"].Function.Invoke(new string[] { "--getDelay" });
            commands["--getLicenseCode"].Function.Invoke(new string[] { "--getLicenseCode" });
            commands["--connection"].Function.Invoke(new string[] { "--connection" });
        }

        private static string FixLetters(string arg)
        {
            switch (arg)
            {
                case "--getlicensecode":
                    return "--getLicenseCode";
                case "--setlicensecode":
                    return "--setLicenseCode";
                case "--getpath":
                    return "--getPath";
                case "--setpath":
                    return "--setPath";
                case "--getdelay":
                    return "--getDelay";
                case "--setdelay":
                    return "--setDelay";
                default:
                    return arg;
            }
                
        }
            
        public static void SendToServer(PipeCommand command, string PipeName, int TimeOut = 5000)
        {
            try
            {
                var serializedCommand = JsonConvert.SerializeObject(command);
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

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



        static void RegisterCommands()
        {
            commands["--help"] = new CommandParams()
            {
                Description = "help",
                Function = PrintHelp
            };

            commands["--version"] = new CommandParams()
            {
                Description = "show "+SERVICE_NAME+" service version",
                Function = PrintVersion
            };


            commands["--status"] = new CommandParams()
            {
                Description = "show " + SERVICE_NAME + " service status",
                Function = PrintStatus
            };


            commands["--start"] = new CommandParams()
            {
                Description = "start " + SERVICE_NAME + " service",
                Function = StartService
            };


            commands["--stop"] = new CommandParams()
            {
                Description = "stop "+SERVICE_NAME+" service",
                Function = StopService
            };

            commands["--getLicenseCode"] = new CommandParams()
            {
                Description = "get code of license type",
                Function = GetLicenseCode
            };

            commands["--setLicenseCode"] = new CommandParams()
            {
                Description = "set code of license type",
                Params = new List<string>() { "[license code]" },
                Function = SetLicenseCode
            };
            commands["--getPath"] = new CommandParams()
            {
                Description = "get Revit shared folder path",
                Function = GetShareFolder
            };

            commands["--setPath"] = new CommandParams()
            {
                Description = "set Revit shared folder path",
                Params = new List<string>() { "[shared folder path]" },
                Function = SetSharedFolder
            };

            commands["--getDelay"] = new CommandParams()
            {
                Description = "get delay in sending changes to "+SERVER_NAME,
                Function = GetDelay
            };

            commands["--setDelay"] = new CommandParams()
            {
                Description = "set delay in mls",
                Params = new List<string>() { "[delay]" },
                Function = SetDelay
            };

            commands["--connection"] = new CommandParams()
            {
                Description = "check connection to "+SERVER_NAME,
                Function = CheckConnection
            };

            commands["--disconnect"] = new CommandParams()
            {
                Description = "disconnect from " + SERVER_NAME,
                Function = OnDisconnect
            };
            commands["--connect"] = new CommandParams()
            {
                Description = "connect to "+SERVER_NAME+" with new parameters",
                Params = new List<string>() { "[database url]" },
                Function = Connect
            };
        }

        private static void SetLicenseCode(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            if (args.Length > 1)
                command.args["licensecode"] = args[1];
            else
            {
                Console.WriteLine("specify license code in the command");
                return;
            }
            SendToServer(command, "rsl_admin");
        }

        private static void GetLicenseCode(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            SendToServer(command, "rsl_admin");
        }

        private static void PrintHelp(string[] args)
        {
            Console.WriteLine($"usage: {_appName} <command> [args]");
            Console.WriteLine(SERVICE_NAME+" command-line client.");
            Console.WriteLine("Available commands:");

            foreach (var command in commands)
            {
                string commandDesc;
                commandDesc = command.Key;
                foreach (var arg in command.Value.Params)
                {
                    commandDesc += " " + arg;
                }
                commandDesc += " - " + command.Value.Description;

                Console.WriteLine(commandDesc);
            }
        }

        private static void GetShareFolder(string[] args)
        {
     
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            SendToServer(command, "rsl_admin");
        }

        private static void SetSharedFolder(string[] args)
        {

            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            if (args.Length > 1)
                command.args["path"] = args[1];
            else
            {
                Console.WriteLine("specify path in the command");
                return;
            }
            SendToServer(command, "rsl_admin");

        }

        private static void GetDelay(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            SendToServer(command, "rsl_admin");
        }

        private static void SetDelay(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            if (args.Length > 1)
                command.args["delay"] = args[1];
            else
            {
                Console.WriteLine("specify time out in the command");
                return;
            }

            SendToServer(command, "rsl_admin");
        }


        private static void CheckConnection(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            SendToServer(command, "rsl_admin");
        }



        private static void PrintVersion(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            SendToServer(command, "rsl_admin");
        }

        private static void PrintStatus(string[] args)
        {
            Console.WriteLine(SERVICE_NAME + " service is " + ServiceController.GetStatus());
        }

        private static void StartService(string[] args)
        {
            if (ServiceController.GetStatus() == "running")
            {
                Console.WriteLine("already running");
                return;
            }
            ServiceController.GetService().Start();
            Console.WriteLine("starting...");
            ServiceController.GetService().WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running);
            PrintStatus(args);
        }

        private static void StopService(string[] args)
        {
            if (ServiceController.GetStatus() == "stopped")
            {
                Console.WriteLine("already stopped");
                return;
            }
            ServiceController.GetService().Stop();
            Console.WriteLine("stopping...");
            ServiceController.GetService().WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped);
            PrintStatus(args);
        }

        private static void OnDisconnect(string[] args)
        {
            PipeCommand command = new PipeCommand();
            command.commandName = args[0];
            Console.WriteLine("disconnecting...");
            SendToServer(command, "rsl_admin");
        }
        private static void Connect(string[] args)
        {


            PipeCommand command = new PipeCommand();
            command.commandName = args[0];

            if (args.Length > 1)
            {
                string databaseUrl = args[1], login, password;

                Console.Write("login: ");
                login = Console.ReadLine();

                Console.Write("password: ");
                password = GetPassword();

                string[] bimServerArgs = new string[3];
                bimServerArgs[0] = databaseUrl;
                bimServerArgs[1] = login;
                bimServerArgs[2] = password;

                command.args["url"] = databaseUrl;
                command.args["login"] = login;
                command.args["password"] = password;
            }
            else
            {
                Console.WriteLine("specify database url in the command");
                return;
            }
            Console.WriteLine("connecting to " + command.args["url"]);
            SendToServer(command, "rsl_admin");
        }

        private static string GetPassword()
        {
            string password = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return password;
        }
    }
}

using System.Linq;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;

namespace PilotRevitShareListener.Server
{
    public interface IServerConnector
    {
        IServerApi ServerApi { get; }
        IFileArchiveApi FileArchiveApi { get; }
        void Connect();
        void Disconnect();
        int PersonId { get; }
    }

    public class ServerConnector : IServerConnector
    {
        private readonly Settings _settings;
        private HttpPilotClient _client;

        public int PersonId { get; private set; }
        public IServerApi ServerApi { get; private set; }
        public IAuthenticationApi AuthenticationApi { get; private set; }
        public IFileArchiveApi FileArchiveApi { get; private set; }

        public ServerConnector(Settings settings)
        {
            _settings = settings;
        }
        
        public void Connect()
        {
            _client = new HttpPilotClient(_settings.ServerUrl);
            try
            {
                _client.Connect(false);
                ServerApi = _client.GetServerApi(new NullableServerCallback());
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(ex.Message);
            }
            try
            { 
                AuthenticationApi = _client.GetAuthenticationApi();
                AuthenticationApi.Login(_settings.DbName, _settings.Login, _settings.Password, false, _settings.LicenseCode);

            }
            catch (System.Exception ex) //catches chaotic ex.Message
            {
                if(ex.Message.Contains("Database"))
                    throw new System.Exception("database ["+ _settings.DbName+"] not found");
                if (ex.Message.Contains("The user name"))
                    throw new System.Exception("the user name or password is incorrect");               
                throw new System.Exception(ex.Message);
            }
            ServerApi.OpenDatabase();

            var people = ServerApi.LoadPeople();
            var person = people.FirstOrDefault(p => !p.IsDeleted && p.Login == _settings.Login);
            if (person != null)
                PersonId = person.Id;

            FileArchiveApi = _client.GetFileArchiveApi();
        }

        public void Disconnect()
        {
            _client?.Disconnect();
        }
    }
}
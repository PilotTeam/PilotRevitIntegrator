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
        private readonly HttpPilotClient _client;

        public int PersonId { get; private set; }
        public IServerApi ServerApi { get; private set; }
        public IAuthenticationApi AuthenticationApi { get; private set; }
        public IFileArchiveApi FileArchiveApi { get; private set; }

        public ServerConnector(Settings settings)
        {
            _settings = settings;
            _client = new HttpPilotClient(_settings.ServerUrl);
        }
        
        public void Connect()
        {
            _client.Connect();

            ServerApi = _client.GetServerApi(new NullableServerCallback());
            AuthenticationApi = _client.GetAuthenticationApi();
            AuthenticationApi.Login(_settings.DbName, _settings.Login, _settings.Password, false, 100);
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
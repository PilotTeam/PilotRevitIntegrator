using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Ascon.Pilot.Common;

namespace PilotRevitShareListener
{
    public class ReaderWriter
    {
        private const string SETTINGS_NAME = "settings.xml";
        private readonly string _serviceName;

        public ReaderWriter(string serviceName)
        {
            _serviceName = serviceName;
        }

        public Settings Settings { get; set; }
       

        public void Write()
        {
            Settings.Password = Settings.Password.DecryptAes();
            var serializer = new XmlSerializer(typeof(Settings));
            Stream fStram = new FileStream(GetSettingsPath(), FileMode.Create, FileAccess.Write, FileShare.None);
            var writer = new StreamWriter(fStram);
            serializer.Serialize(writer, Settings);
            writer.Close();
            fStram.Close();
            Settings.Password = Settings.Password.EncryptAes();
        }
        public Settings Read()
        {
            var serializer = new XmlSerializer(typeof(Settings));
            try
            {
                using (var reader = new StreamReader(GetSettingsPath()))
                    Settings = (Settings)serializer.Deserialize(reader);
                if (Settings != null)
                    Settings.Password = Settings.Password.EncryptAes();
            }catch(FileNotFoundException)
            {
                Settings = new Settings();
                Settings.LicenseCode = 100;
                Settings.Timeout = 30000;
                Settings.SharePath = "";
                Settings.ServerUrl = "";
                Settings.DbName = "";
                Settings.Login = "";
                Settings.Password = "";
            }
            return Settings;
        }
        private string GetSettingsPath()
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData) + @"\" + _serviceName;
            return Path.Combine(path, SETTINGS_NAME);
        }
    }
}


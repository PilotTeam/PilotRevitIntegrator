using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Ascon.Pilot.Common;

namespace PilotRevitShareListener
{
    public class ReaderWriter
    {
        protected const string SettingsName = "settings.xml";
        public Settings settings { get; set; }
        readonly string _ServiceName;

        public ReaderWriter(string ServiceName)
        {
            _ServiceName = ServiceName;
        }
         string GetSettingsPath()
        {
            var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData) + @"\" + _ServiceName;
            return Path.Combine(path, SettingsName);
        }

        public void Write(Settings settings)
        {
            settings.Password = settings.Password.DecryptAes();
            var serializer = new XmlSerializer(typeof(Settings));
            Stream fStram = new FileStream(GetSettingsPath(), FileMode.Create, FileAccess.Write, FileShare.None);
            var writer = new StreamWriter(fStram);
            serializer.Serialize(writer, settings);
            writer.Close();
            fStram.Close();
            settings.Password = settings.Password.EncryptAes();
        }
        public Settings Read()
        {
            var serializer = new XmlSerializer(typeof(Settings));
            try
            {
                using (var reader = new StreamReader(GetSettingsPath()))
                    settings = (Settings)serializer.Deserialize(reader);
                if (settings != null)
                    settings.Password = settings.Password.EncryptAes();
            }catch(FileNotFoundException)
            {
                settings = new Settings();
                settings.LicenseCode = 100;
                settings.Timeout = 30000;
                settings.SharePath = "";
                settings.ServerUrl = "";
                settings.DbName = "";
                settings.Login = "";
                settings.Password = "";
            }
            return settings;
        }
    }
}


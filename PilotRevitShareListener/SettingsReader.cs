using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Ascon.Pilot.Core;

namespace PilotRevitShareListener
{
    public static class SettingsReader
    {
        private const string SettingsName = "settings.xml";

        private static string GetSettingsPath()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            // ReSharper disable once AssignNullToNotNullAttribute
            return Path.Combine(Path.GetDirectoryName(exePath), SettingsName);
        }

        public static Settings Read()
        {
            Settings settings;

            var serializer = new XmlSerializer(typeof(Settings));
            using (var reader = new StreamReader(GetSettingsPath()))
                settings = (Settings)serializer.Deserialize(reader);

            if (settings != null)
                settings.Password = settings.Password.EncryptAes();
            return settings;
        }
    }
}
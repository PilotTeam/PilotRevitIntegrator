using System;

namespace PilotRevitShareListener
{
    [Serializable]
    public class Settings
    {
        public string ServerUrl { get; set; }
        public string DbName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int LicenseType { get; set; }
        public string SharePath { get; set; }
        public double Timeout { get; set; }
    }
}

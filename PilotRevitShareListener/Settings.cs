using System;

namespace PilotRevitShareListener
{
    [Serializable]
    public class Settings
    {
        private static object _locker = new object();
        private string _serverUrl;
        private string _dbName;
        private string _login;
        private string _password;
        private int _licenseCode;
        private string _sharePath;
        private double _timeout;

        public string ServerUrl
        {
            get
            {
                lock (_locker)
                    return _serverUrl;
            }

            set
            {
                lock (_locker)
                    _serverUrl = value;
            }
        }

        public string DbName
        {
            get
            {
                lock (_locker)
                    return _dbName;
            }

            set
            {
                lock (_locker)
                    _dbName = value;
            }
        }

        public string Login
        {
            get
            {
                lock (_locker)
                    return _login;
            }

            set
            {
                lock (_locker)
                    _login = value;
            }
        }

        public string Password
        {
            get
            {
                lock (_locker)
                    return _password;
            }

            set
            {
                lock (_locker)
                    _password = value;
            }
        }

        public int LicenseCode
        {
            get
            {
                lock (_locker)
                    return _licenseCode;
            }

            set
            {
                lock (_locker)
                    _licenseCode = value;
            }
        }

        public string SharePath
        {
            get
            {
                lock (_locker)
                    return _sharePath;
            }

            set
            {
                lock (_locker)
                    _sharePath = value;
            }
        }

        public double Timeout
        {
            get
            {
                lock (_locker)
                    return _timeout;
            }

            set
            {
                lock (_locker)
                    _timeout = value;
            }
        }
    }
}

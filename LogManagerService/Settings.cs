using System;
using System.Collections.Generic;
using System.IO;

namespace LogManagerService
{
    public static class Settings
    {
        static Common.Settings _settings = Common.Settings.GetInstance();
            
        public const string OpLogPath = "state\\oplog";
        private static List<string> _sortedLogsPaths = null;
        public static List<string> SortedLogsPaths
        {
            get
            {
                if (_sortedLogsPaths == null)
                {
                    List<string> result = new List<string>();
                    var dir1 = _settings.TryGetValue("SortedLogsDirectory");
                    if (String.IsNullOrEmpty(dir1))
                        throw new Exception("SortedLogsDirectory not specified");
                    result.Add(dir1);
                    var dir2 = _settings.TryGetValue("SecondarySortedLogsDirectory");
                    if (!String.IsNullOrEmpty(dir2))
                        result.Add(dir2);
                    //new List<string>() {".\\logs\\sorted\\", "F:\\nginxLogs\\logs\\sorted\\"};
                    _sortedLogsPaths = result;
                }
                return _sortedLogsPaths;
                 
            }
        }

        public const string LastLogHashesFileName = "state\\lastLogHashes";
        public const double LogListRefreshInterval = 2*60*60;
        private static string _weblogIndexDbConnectionString;
        public static string WeblogIndexDbConnectionString
        {
            get
            {

                if (String.IsNullOrEmpty(_weblogIndexDbConnectionString))
                {
                    var connString = _settings.TryGetValue("WeblogIndexDbConnectionString");
                    if (String.IsNullOrEmpty(connString))
                        throw new Exception("WeblogIndexDbConnectionString not specified");

                    _weblogIndexDbConnectionString = connString;
                }
                return _weblogIndexDbConnectionString;
            }
        }

        private static string _serviceBindingAddress;
        public static string ServiceBindingAddress
        {
            get
            {
                if (String.IsNullOrEmpty(_serviceBindingAddress))
                {
                    var bindAddress = _settings.TryGetValue("ServiceBindingAddress");
                    if (String.IsNullOrEmpty(bindAddress))
                        throw new Exception("ServiceBindingAddress not specified");

                    _serviceBindingAddress = bindAddress;
                }
                return _serviceBindingAddress; 
                //"http://+:1819/logManager/";
            }
        }
    }
}
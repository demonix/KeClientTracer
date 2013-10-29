using System;
using System.Collections.Generic;
using System.IO;

namespace LogManagerService
{
    public static class Settings
    {
        static Common.Settings _settings = Common.Settings.GetInstance();
            
        public const string OpLogPath = "state\\oplog";
        public const string LastLogHashesFileName = "state\\lastLogHashes";

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

        
        public const double LogListRefreshInterval = 2*60*60;

        private static string _weblogIndexMongoDbConnectionString;
        public static string WeblogIndexMongoDbConnectionString
        {
            get
            {
                if (String.IsNullOrEmpty(_weblogIndexMongoDbConnectionString))
                {
                    var connString = _settings.TryGetValue("WeblogIndexMongoDbConnectionString");
                    if (String.IsNullOrEmpty(connString))
                        throw new Exception("WeblogIndexMongoDbConnectionString not specified");

                    _weblogIndexMongoDbConnectionString = connString;
                }
                return _weblogIndexMongoDbConnectionString;
            }
            
        }

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

        private static string[] _logDirectories = new string[0];
        public static string[] LogDirectories
        {
            get
            {
                if (_logDirectories.Length == 0)
                {
                    var logDirectories = _settings.TryGetValue("LogDirectories");
                    
                    if (String.IsNullOrEmpty(logDirectories))
                        throw new Exception("LogDirectories not specified");
                    _logDirectories = logDirectories.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return _logDirectories;
            }
        }

        private static string[] _logfileMasks = new string[0];
        public static string[] LogfileMasks
        {
            get
            {
                if (_logfileMasks.Length == 0)
                {
                    var logfileMasks = _settings.TryGetValue("LogfileMasks");

                    if (String.IsNullOrEmpty(logfileMasks))
                        throw new Exception("LogfileMasks not specified");
                    _logfileMasks = logfileMasks.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return _logfileMasks;
            }
        }
    }
}
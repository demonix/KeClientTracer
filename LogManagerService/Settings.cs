using System;
using System.Collections.Generic;
using System.IO;

namespace LogManagerService
{
    public class Settings
    {

        public const string OpLogPath = "oplog";
        public static List<string> SortedLogsPaths = new List<string>(){".\\logs\\sorted\\","F:\\nginxLogs\\logs\\sorted\\"};

        public const string LastLogHashesFileName = "lastLogHashes.txt";
        public const double LogListRefreshInterval = 2*60*60;
        private static string _weblogIndexDbConnectionString;
        public static string WeblogIndexDbConnectionString
        {
            get
            {
                if (String.IsNullOrEmpty(_weblogIndexDbConnectionString))
                    _weblogIndexDbConnectionString = File.ReadAllText("settings\\WeblogIndexDbConnectionString");
                return _weblogIndexDbConnectionString;
            }
        }
    }
}
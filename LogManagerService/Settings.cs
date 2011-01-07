using System;
using System.IO;

namespace LogManagerService
{
    public class Settings
    {

        public const string OpLogPath = "oplog";
        public const string LastLogHashesFileName = "lastLogHashes.txt";
        public const double LogListRefreshInterval = 2*60*60;
        private static string _weblogIndexDbConnectionString;
        public string WeblogIndexDbConnectionString
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
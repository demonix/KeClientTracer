using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common;
using LogManagerService.DbLayer;

namespace LogManagerService
{
    public class ServiceState
    {


        public ConcurrentDictionary<string,RotatedLog> AllLogs { get; private set; }
        public ConcurrentDictionary<string, string> LastKnownLogHashes { get; private set; }
        public ConcurrentQueue<string> HashesOfPendingLogs { get; private set; }
        public ConcurrentHashSet<string> AllHashes { get; private set; }
        private static ServiceState _instance;
        private static object _instanceLocker = new object();
        public IDb Db { get; private set; }
        Timer _tm;
        private ServiceState()
        {
            AllLogs = new ConcurrentDictionary<string, RotatedLog>();
            LastKnownLogHashes = new ConcurrentDictionary<string, string>();
            HashesOfPendingLogs = new ConcurrentQueue<string>();
            AllHashes = new ConcurrentHashSet<string>();
            Db = new MongoDb();
        }

        private void SetupGetFilesTimer()
        {
            _tm = new Timer(GetFiles, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(Settings.LogListRefreshInterval));
        }

        public static void Init()
        {
            _instance = new ServiceState();
            _instance.LoadLastLogHashes();
            _instance.ProcessOpLog();
            _instance.SetupGetFilesTimer();
        }

        public static ServiceState GetInstance()
        {
            if (_instance == null)
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        Init();
                    }
                }
            return _instance;
        }

        private void ProcessOpLog()
        {
            Console.Out.WriteLine("Begin processing oplog...");
            if (!File.Exists(Settings.OpLogPath)) return;
            string[] oplogFile = File.ReadAllLines(Settings.OpLogPath);
            HashSet<string> hashesOfNotYetProcessedLogs = new HashSet<string>();

            foreach (string oplogLine in oplogFile)
            {
                var hash = oplogLine.Split('\t')[1];
                AllHashes.AddIfNotExists(hash);
                if (oplogLine[0] == 'r')
                    hashesOfNotYetProcessedLogs.Remove(hash);
                if (oplogLine[0] == 'a')
                    hashesOfNotYetProcessedLogs.Add(hash);
            }
            foreach (string hash in hashesOfNotYetProcessedLogs)
            {
                HashesOfPendingLogs.Enqueue(hash);
            }
           Console.Out.WriteLine("Finished processing oplog...");
        }

        private void GetFiles(object state)
        {
            lock (LastKnownLogHashes)
            {
                AllLogs.Clear();
                Console.Out.WriteLine("Reread files...");
                string[] fileMasks = Settings.LogfileMasks;
                string[] logDirectories = Settings.LogDirectories;
                foreach (string logDirectory in logDirectories)
                {
                    if (!Directory.Exists(logDirectory))
                    {
                        Console.Out.WriteLine("Directory {0} does not exists.", logDirectory);
                        continue;
                    }

                    foreach (string fileMask in fileMasks)
                    {
                        ReadFilesByMaskFromFolder(logDirectory, fileMask);
                    }
                }
                FlushLogHashes();
            }
            Console.Out.WriteLine(PendingLogList);
            Console.Out.WriteLine("Reread files finished...");
        }

        private void ReadFilesByMaskFromFolder(string logDirectory, string fileMask)
        {
            string key = String.Format("{0}\\{1}", logDirectory, fileMask);
            RotatedLogFolder fld;
            try
            {
                fld = new RotatedLogFolder(logDirectory, fileMask);
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine("Error: {0}", exception);
                return;
            }
            foreach (var log in fld.LogFiles)
            {
                AllLogs.TryAdd(log.Hash, log);
            }

            string lastHash;
            List<RotatedLog> freshLogs =
                fld.GetFreshLogs(LastKnownLogHashes.TryGetValue(key, out lastHash) ? lastHash : "");

            foreach (RotatedLog freshRotatedLog in freshLogs)
            {
                if (!AllHashes.AddIfNotExists(freshRotatedLog.Hash))
                    continue;
                HashesOfPendingLogs.Enqueue(freshRotatedLog.Hash);
                OpLog.Add(freshRotatedLog.Hash, freshRotatedLog.FileName);
            }
            if (freshLogs.Count > 0)
                LastKnownLogHashes[key] = freshLogs[0].Hash;
        }

        private void LoadLastLogHashes()
        {
            if (File.Exists(Settings.LastLogHashesFileName))
            {
                string[] lines = File.ReadAllLines(Settings.LastLogHashesFileName);
                foreach (string line in lines)
                {
                    if (!String.IsNullOrEmpty(line.Trim()))
                        LastKnownLogHashes.TryAdd(line.Split('\t')[0], line.Split('\t')[1]);
                }
            }
        }

        private void FlushLogHashes()
        {
            File.WriteAllText(Settings.LastLogHashesFileName, "");
                foreach (KeyValuePair<string, string> logHash in LastKnownLogHashes)
                {
                    File.AppendAllText(Settings.LastLogHashesFileName, String.Format("{0}\t{1}\r\n", logHash.Key, logHash.Value));
                }
        }

        public string PendingLogList
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                string[] hashList = HashesOfPendingLogs.ToArray();
                foreach (string hash in hashList)
                {
                    RotatedLog rotatedLog;
                    var hasLog = AllLogs.TryGetValue(hash, out rotatedLog);
                    sb.AppendFormat("{0}: {1}\r\n", hash, hasLog ? rotatedLog.FileName : "Unavaliable file");
                }
                return sb.ToString();
            }
        }
    }
}
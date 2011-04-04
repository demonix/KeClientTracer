using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common.ThreadSafeObjects;
using LogManagerService.DbLayer;

namespace LogManagerService
{
    public class ServiceState
    {

        
        public ThreadSafeList<RotatedLog> AllLogs { get; private set; }
        public ThreadSafeDictionary<string, string> LastKnownLogHashes { get; private set; }
        //private static Queue<string> _pendingLogHashes = new Queue<string>();
        public  ThreadSafeQueue<string> HashesOfPendingLogs { get; private set; }
        private static ServiceState _instance;
        private static object _instanceLocker = new object();
        public IDb Db { get; private set; }
        Timer _tm ;
        private ServiceState()
        {
            AllLogs = new ThreadSafeList<RotatedLog>();
            LastKnownLogHashes = new ThreadSafeDictionary<string, string>();
            HashesOfPendingLogs = new ThreadSafeQueue<string>();
        }

        private void SetupGetFilesTimer()
        {
            _tm = new Timer(GetFiles, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(Settings.LogListRefreshInterval));
        }

        public static ServiceState GetInstance()
        {
            lock (_instanceLocker)
            {
                if (_instance == null)
                {
                    _instance = new ServiceState();
                    _instance.LoadLastLogHashes();
                    _instance.ProcessOpLog();
                    _instance.SetupGetFilesTimer();
                }
            }
           return _instance;
        }

        private void ProcessOpLog()
        {
            if (!File.Exists(Settings.OpLogPath)) return;
            string[] oplogFile = File.ReadAllLines(Settings.OpLogPath);
            List<string> hashesOfNotYetProcessedLogs = new List<string>();

            foreach (string oplogLine in oplogFile)
            {
                if (oplogLine[0] == 'r')
                    hashesOfNotYetProcessedLogs.Remove(oplogLine.Split('\t')[1]);
                if (oplogLine[0] == 'a')
                    hashesOfNotYetProcessedLogs.Add(oplogLine.Split('\t')[1]);
            }
                ServiceState.GetInstance().HashesOfPendingLogs.EnqueueMany(hashesOfNotYetProcessedLogs);
                //_pendingLogHashes.Enqueue(hash);
        }

        private void GetFiles(object state)
        {
            lock (LastKnownLogHashes)
            {
                AllLogs.Clear();
                Console.Out.WriteLine("Reread files...");
                string[] fileMasks = File.ReadAllLines("fileMasks.txt");
                string[] logDirectories = File.ReadAllLines("logDirectories.txt");
                foreach (string logDirectory in logDirectories)
                {
                    if (!Directory.Exists(logDirectory))
                    {
                        Console.Out.WriteLine("Directory {0} does not exists.", logDirectory);
                        continue;
                    }

                    foreach (string fileMask in fileMasks)
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
                            continue;
                        }
                        AllLogs.AddRange(fld.LogFiles);
                        
                        string lastHash;
                        LastKnownLogHashes.TryGet(key, out lastHash);
                        List<RotatedLog> freshLogs = fld.GetFreshLogs(lastHash ?? "");

                        foreach (RotatedLog freshRotatedLog in freshLogs)
                        {
                            if (HashesOfPendingLogs.Items.Contains(freshRotatedLog.Hash))
                                continue;
                            HashesOfPendingLogs.Enqueue(freshRotatedLog.Hash);
                            OpLog.Add(freshRotatedLog.Hash,freshRotatedLog.FileName);
                        }
                        if (freshLogs.Count > 0)
                            if (LastKnownLogHashes.ContainsKey(key))
                                LastKnownLogHashes[key] = freshLogs[0].Hash;
                            else
                                LastKnownLogHashes.Add(key, freshLogs[0].Hash);
                    }
                }
                FlushLogHashes();
            }
            Console.Out.WriteLine(PendingLogList);
        }

        private void LoadLastLogHashes()
        {
            lock (LastKnownLogHashes)
            {
                if (File.Exists(Settings.LastLogHashesFileName))
                {
                    string[] lines = File.ReadAllLines(Settings.LastLogHashesFileName);
                    foreach (string line in lines)
                    {
                        if (!String.IsNullOrEmpty(line.Trim()))
                            ServiceState.GetInstance().LastKnownLogHashes.Add(line.Split('\t')[0], line.Split('\t')[1]);
                    }
                }
            }
        }

        private void FlushLogHashes()
        {
            File.WriteAllText(Settings.LastLogHashesFileName, "");
            lock (LastKnownLogHashes)
            {
                List<KeyValuePair<string, string>> snapshot = ServiceState.GetInstance().LastKnownLogHashes.GetSnapshot();
                foreach (KeyValuePair<string, string> logHash in snapshot)
                {
                    File.AppendAllText(Settings.LastLogHashesFileName, String.Format("{0}\t{1}\r\n", logHash.Key, logHash.Value));
                }
            }
        }

        public string PendingLogList
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                List<string> hashList = HashesOfPendingLogs.Items;

                foreach (string hash in hashList)
                {
                    string hash1 = hash;
                    RotatedLog rotatedLog = AllLogs.FirstOrDefault(l => l.Hash == hash1);
                    sb.AppendFormat("{0}: {1}\r\n", hash1, rotatedLog == null ? "Unavaliable file" : rotatedLog.FileName);
                }
            
                /*foreach (string pendingLogHash in _pendingLogHashes)
                {
                    string pendingLogHash1 = pendingLogHash;
                    RotatedLog rotatedLog = _allLogs.FirstOrDefault(l => l.Hash == pendingLogHash1);
                    sb.AppendFormat("{0}: {1}\r\n",pendingLogHash1,rotatedLog == null ? "Deleted" : rotatedLog.FileName);
                }*/
                return sb.ToString();
            }
        }
    }
}
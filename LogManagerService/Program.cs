using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.ThreadSafeObjects;

namespace LogManagerService
{
    class Program
    {
        static Dictionary<string, string> _lastKnownLogHashes = new Dictionary<string, string>();
        //private static Queue<string> _pendingLogHashes = new Queue<string>();
        private static ThreadSafeQueue<string> _hashesOfPendingLogs = new ThreadSafeQueue<string>();
        //static Dictionary<string,string> _hashToFile = new Dictionary<string, string>();


        static void Main(string[] args)
        {
            LoadLastLogHashes();
            ProcessOpLog();
            Timer tm = new Timer(GetFiles, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromHours(2));
            //GetFiles(null);
            using (WebServer webServer = new WebServer("http://+:1818/logManager/"))
            {
                webServer.IncomingRequest += WebServer_IncomingRequest;
                webServer.Start();
                Console.WriteLine("WebServer started. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ProcessOpLog()
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
            foreach (string hash in hashesOfNotYetProcessedLogs)
            {
                _hashesOfPendingLogs.Enqueue(hash);
                //_pendingLogHashes.Enqueue(hash);
            }
        }

        const string LastLogHashesFileName = "lastLogHashes.txt";
        private static void LoadLastLogHashes()
        {
            lock (_lastKnownLogHashes)
            {
                if (File.Exists(LastLogHashesFileName))
                {
                    string[] lines = File.ReadAllLines(LastLogHashesFileName);
                    foreach (string line in lines)
                    {
                        if (!String.IsNullOrEmpty(line.Trim()))
                            _lastKnownLogHashes.Add(line.Split('\t')[0], line.Split('\t')[1]);
                    }
                }
            }
        }

        private static void FlushLogHashes()
        {
            File.WriteAllText(LastLogHashesFileName, "");
            foreach (KeyValuePair<string, string> logHash in _lastKnownLogHashes)
            {
                File.AppendAllText(LastLogHashesFileName, String.Format("{0}\t{1}\r\n", logHash.Key, logHash.Value));
            }
        }

        static List<RotatedLog> _allLogs = new List<RotatedLog>();
        private static void GetFiles(object state)
        {
            lock (_lastKnownLogHashes)
            {
               _allLogs.Clear(); 
                Console.Out.WriteLine("Reread files...");
                string[] fileMasks = File.ReadAllLines("fileMasks.txt");
                string[] logDirectories = File.ReadAllLines("logDirectories.txt");
                foreach (string logDirectory in logDirectories)
                {
                    if (!Directory.Exists(logDirectory))
                    {
                        Console.Out.WriteLine("Directory {0} does not exists.",logDirectory);
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
                        catch(Exception exception)
                        {
                            Console.Out.WriteLine("Error: {0}", exception);
                            continue;
                        }
                        _allLogs.AddRange(fld.LogFiles);
                        List<RotatedLog> freshLogs = fld.GetFreshLogs(_lastKnownLogHashes.ContainsKey(key)? _lastKnownLogHashes[key]:"");
                        foreach (RotatedLog freshRotatedLog in freshLogs)
                        {
                            _hashesOfPendingLogs.Enqueue(freshRotatedLog.Hash);
                            File.AppendAllText(Settings.OpLogPath,String.Format("a\t{0}\r\n", freshRotatedLog.Hash));
                        }
                        if (freshLogs.Count > 0)
                        if (_lastKnownLogHashes.ContainsKey(key))
                            _lastKnownLogHashes[key] = freshLogs[0].Hash;
                        else
                            _lastKnownLogHashes.Add(key, freshLogs[0].Hash);
                    }
                }
                FlushLogHashes();
            }
            Console.Out.WriteLine(GetPendingLogList());
        }


        


        static string GetPendingLogList()
        {
            StringBuilder sb = new StringBuilder();
            List<string> hashList = _hashesOfPendingLogs.Items;

            foreach (string hash in hashList)
            {
                string hash1 = hash;
                RotatedLog rotatedLog = _allLogs.FirstOrDefault(l => l.Hash == hash1);
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
     

        private static void WebServer_IncomingRequest(object sender, HttpRequestEventArgs e)
        {
            string [] requestParts = e.RequestContext.Request.Url.AbsolutePath.Split(new []{"/"},StringSplitOptions.RemoveEmptyEntries);
            if (requestParts.Length == 2)
                switch (requestParts[1].ToLower())
                {
                    case "stats":
                        GetStats(e);
                        break;

                    case "getfilename":
                        GetFileName(e);
                        break;

                    case "results":
                        PrintResults(e);
                        break;
                    default:
                        ReturnError(e);
                        break;
                }
            else
                ReturnError(e);
            



        }

        private static void PrintResults(HttpRequestEventArgs httpRequestEventArgs)
        {
           
        }

        private static void ReturnError(HttpRequestEventArgs httpRequestEventArgs)
        {
            HttpListenerResponse response = httpRequestEventArgs.RequestContext.Response;
            string content = "Unknown command";
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.StatusDescription = "Unknown command";
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private static void GetFileName(HttpRequestEventArgs httpRequestEventArgs)
        {
            HttpListenerResponse response = httpRequestEventArgs.RequestContext.Response;
            string content = GetNextAlaviableFileName();
            if (String.IsNullOrEmpty(content))
            {
                ReturnNoFiles(httpRequestEventArgs);
                return;
            }
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private static void ReturnNoFiles(HttpRequestEventArgs httpRequestEventArgs)
        {
            HttpListenerResponse response = httpRequestEventArgs.RequestContext.Response;
            string content = "No new files to process";
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.StatusDescription = "No new files to process";
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }

        //static List<string> files = new List<string>();
        
        private static string GetNextAlaviableFileName()
        {
            string result = "";
            string hash = "";
            List<string> hashesOfUnalavaliableFiles = new List<string>();
            while (_hashesOfPendingLogs.TryDequeue(out hash))
            {
                string hash1 = hash;
                RotatedLog rotatedLog = _allLogs.FirstOrDefault(l => l.Hash == hash1);
                if (rotatedLog != null)
                {
                    result = rotatedLog.FileName;
                    File.AppendAllText(Settings.OpLogPath, String.Format("r\t{0}\r\n", hash));
                    break;
                }
                hashesOfUnalavaliableFiles.Add(hash1);
            }

            _hashesOfPendingLogs.EnqueueMany(hashesOfUnalavaliableFiles);
            return result;
        }

        private static void GetStats(HttpRequestEventArgs httpRequestEventArgs)
        {
            HttpListenerResponse response = httpRequestEventArgs.RequestContext.Response;
            string content = String.Format("<html><pre>{0}</pre></html>",GetPendingLogList());
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Common.Web;
using KeClientTracing.LogReading;
using LogProcessors;
using Networking;


namespace LogReaderTest
{
    class Program
    {
        private static Guid _instanceId = Guid.NewGuid();
        private static NetClient _clnt;
        private static HashSet<string> _sessions = new HashSet<string>();
        private static AutoResetEvent _finishedReading = new AutoResetEvent(false);
        private static Stopwatch stopwatch = new Stopwatch();
        private static string outPath = "";


        static void Main(string[] args)
        {
            Console.Out.WriteLine("InstanceId: {0}", _instanceId);
            TimeSpan ts = new TimeSpan();
            if (File.Exists("outPath"))
                outPath = File.ReadAllText("outPath");
            outPath = outPath.TrimEnd('\\');

            string inputFile;
            while (GetNextFileNameToProcess(out inputFile, args[0]))
            {
                stopwatch.Reset();
                stopwatch.Start();

                Console.Out.WriteLine("Begin read " + inputFile);

                ILogReader lr = null;
                try
                {
                    if (Path.GetExtension(inputFile) == ".gz")
                        lr = new GzWebLogReader(inputFile, Encoding.Default);
                    else
                        lr = new WebLogReader(inputFile, Encoding.Default);

                    lr.LineReaded += OnLineReaded_writeNginx;
                    lr.FinishedReading += OnFinishedReading_write;

                    try
                    {
                        lr.BeginRead();
                        _finishedReading.WaitOne();
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.ToString());
                        continue;
                    }

                }
                catch (Exception ex)
                {
                    WriteError(ex.ToString());
                }
                finally
                {
                    if (lr != null)
                        lr.Close();
                }
                ts = ts.Add(stopwatch.Elapsed);
                Console.Out.WriteLine("Finished read {0} in {1}\r\nTotal elapsed: {2}", inputFile, stopwatch.Elapsed, ts);
            }

            Console.Out.WriteLine("Finished all in " + ts);
            CloseFileStreams();
            Console.ReadKey();

        }

        private static bool GetNextFileNameToProcess(out string inputFile, string serverUrl)
        {
            
           
                Response response = Request.Get(serverUrl + "/getnextfile");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    inputFile = response.ResponseText;
                    return true;
                }
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    WriteError(String.Format("Error while getting next file name to process. Response code: {0}, response text: {1}",response.StatusCode,response.ResponseText));
                }
                    inputFile = ""; 
                    return false;
        }


        static Dictionary<string, FileStream> _fileHandlesCache = new Dictionary<string, FileStream>();
        
        static FileStream GetResultFile(string date)
        {
            if (!_fileHandlesCache.ContainsKey(date))
                _fileHandlesCache.Add(date, new FileStream(GetNextAvaliableFileName(date), FileMode.Append, FileAccess.Write, FileShare.Read));
            return _fileHandlesCache[date];
        }

        private static string GetNextAvaliableFileName(string date)
        {
            string fileName = "";
            
            string[] dateParts = date.Split('.');
            DateTime dt = new DateTime(Convert.ToInt32(dateParts[2]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[0]));
            for (int counter = 0; counter <=1000; counter++ )
            {
                fileName = String.Format("{0}\\{1}.{2}.{3}.{4}.requestData", outPath, dt.Year.ToString("D4"), dt.Month.ToString("D2"), dt.Day.ToString("D2"), counter.ToString("D4"));
                if (!File.Exists(fileName))
                    try
                    {
                        File.Create(fileName);
                        return fileName;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

            }
            throw new IOException(String.Format("Перебрали 1000 файлов, но все они уже существуют: {0}",fileName));
        }

        static void CloseFileStreams()
        {
            foreach (KeyValuePair<string, FileStream> keyValuePair in _fileHandlesCache)
            {
                keyValuePair.Value.Flush();
                keyValuePair.Value.Close();
                keyValuePair.Value.Dispose();
            }
        }

        private static void OnFinishedReading_write(object sender, EventArgs e)
        {
            _finishedReading.Set();
        }
        private static void OnFinishedReading_send(object sender, EventArgs e)
        {
            lock (_list)
            {
                Flush();    
            }
            
        }

        static List<byte[]> _list = new List<byte[]>();
        private static int _totalMessageLength;
        private static int i;
        static AutoResetEvent _wait = new AutoResetEvent(true);


        static KeFrontLogProcessor lp = new KeFrontLogProcessor();
        static object locker = new object();

        private static void OnLineReaded_writeSimple(object sender, LineReadedEventArgs e)
        {
            
            lock (locker)
            {
               Console.WriteLine(e.Line);
            }
        }

        

        private static void OnLineReaded_writeNginx(object sender, LineReadedEventArgs e)
        {
            string meta;
            string requestData;
            string error;
            if (!lp.Process(e.Line, out meta, out requestData, out error))
            {
                WriteError(error);
                return;
            }

            lock (locker)
            {
                
                string key = meta.Replace('\t','^');
                
                /*if (!_sessions.Contains(key))
                {
                    _sessions.Add(key);
                    byte[] lineB1 = Encoding.Default.GetBytes(meta + "\r\n");
                    bf1.Write(lineB1, 0, lineB1.Length);
                }*/

                byte[] lineB2 = Encoding.Default.GetBytes(key + "\t" + requestData + "\r\n");
                
                GetResultFile(meta.Split('\t')[0]).Write(lineB2, 0, lineB2.Length);    
                
                //bf.Write(CLRF,0,2);
            }
        }

        private static void WriteError(string error)
        {
            try
            {
                File.AppendAllText("errors", _instanceId+ " "+ error + "\r\n");
            }
            catch (IOException)
            {
                try
                {
                    File.AppendAllText("errors.1", _instanceId+" "+error + "\r\n");
                }
                catch (IOException)
                {
                    try
                    {
                        File.AppendAllText("errors.2", _instanceId +" "+ error + "\r\n");
                    }
                    catch
                    {
                        Console.Error.WriteLine(error);
                    }

                }

            }
        }

        private static void OnLineReaded_send(object sender, LineReadedEventArgs e)
        {
            lock (_list)//_wait.WaitOne();
            {
                byte[] mb = new NetworkMessage(e.Line).GetBytesForTransfer();

                //if (_list.Count > 1000)
                //    _wait.WaitOne();
                _list.Add(mb);
                Interlocked.Add(ref _totalMessageLength, mb.Length);

                if (_list.Count > 10000)
                {

                    Flush();
                    //_wait.Set();
                }

            }
        }

        private static void Flush()
        {
            int offset = 0;
            //if (i < 3)
            {
                byte[] allMessages = new byte[_totalMessageLength];

                foreach (byte[] b in _list)
                {
                    Buffer.BlockCopy(b, 0, allMessages, offset, b.Length);
                    Interlocked.Add(ref offset, b.Length);
                }

                _clnt.SendQ(allMessages);

                Console.Out.WriteLine("Line {0} Sent", i++);
            }

            _list.Clear();
            _totalMessageLength = 0;
            
        }
    }
}

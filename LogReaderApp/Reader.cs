using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Common;
using KeClientTracing.LogReading;
using LogProcessors;

namespace LogReaderApp
{
    public class Reader
    {
        public Reader(string outPath)
        {
            InstanceId = Guid.NewGuid();
            _outPath = outPath;
        }

        KeFrontLogProcessor lp = new KeFrontLogProcessor();
        Dictionary<string, FileStream> _fileHandlesCache = new Dictionary<string, FileStream>();
        public Guid InstanceId { get; private set; }
        private Stopwatch _stopwatch = new Stopwatch();
        private AutoResetEvent _finishedReading = new AutoResetEvent(false);
        private TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }
        private int _isReading; //0 - not reading, 1 - reading
        private string _logFileName ="";
        private object _resultWriteLocker = new object();
        private string _outPath;

        public void Read(string logFileName, bool stopAtEof)
        {
            if (Interlocked.Exchange(ref _isReading,1) == 1)
                throw new Exception(string.Format("This reader already reading file {0}", _logFileName));
            _logFileName = logFileName;
          
            _stopwatch.Start();
            ILogReader lr = null;
            try
            {
               
                if (Path.GetExtension(_logFileName) == ".gz")
                    lr = new GzWebLogReader(_logFileName, Encoding.Default);
                else
                    lr = new WebLogReader(_logFileName, Encoding.Default);

                lr.LineReaded += OnLineReaded;
                lr.FinishedReading += OnFinishedReading;

                    lr.BeginRead();
                    _finishedReading.WaitOne();

            }
            finally
            {
                if (lr != null)
                    lr.Close();
                _stopwatch.Stop();
                _stopwatch.Reset();
            }
            _isReading = 0;
        }

    

        private  void OnFinishedReading(object sender, EventArgs e)
        {
            _finishedReading.Set();
        }

        private void OnLineReaded(object sender, LineReadedEventArgs e)
        {
            string meta;
            string requestData;
            string error;
            if (!lp.Process(e.Line, out meta, out requestData, out error))
            {
                Common.Logging.Logger.WriteErrorToFile("logreader",error);
            }
 string key = meta.Replace('\t', '^');
            byte[] lineB2 = Encoding.Default.GetBytes(key + "\t" + requestData + "\r\n");
            FileStream fs = GetResultFile(meta.Split('\t')[0]);
            lock (_resultWriteLocker)
            {
                fs.Write(lineB2, 0, lineB2.Length);
            }
        }

        private FileStream GetResultFile(string date)
        {
            if (!_fileHandlesCache.ContainsKey(date))
                _fileHandlesCache.Add(date, new FileStream(GetNextAvaliableFileName(date), FileMode.Append, FileAccess.Write, FileShare.Read));
            return _fileHandlesCache[date];
        }

        private string GetNextAvaliableFileName(string date)
        {
            DateTime dt = DateConversions.DmyToDate(date);
            for (int counter = 0; counter <= 1000; counter++)
            {
                string fileName = String.Format("{0}\\{1}.{2}.requestData", _outPath, DateConversions.DateToYmd(dt), counter.ToString("D4"));
                if (!File.Exists(fileName))
                    try
                    {
                        File.Create(fileName);
                        return fileName;
                    }
                    catch (IOException)
                    {
                        continue;
                    }

            }
            throw new IOException(String.Format("Для даты {0} в каталоге {1} уже создано более 1000 фалйов.", date, _outPath));
        }
    }
}
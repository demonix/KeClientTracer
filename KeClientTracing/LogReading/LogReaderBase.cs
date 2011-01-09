using System;
using System.IO;
using System.Text;

namespace KeClientTracing.LogReading
{
    public abstract class LogReaderBase : ILogReader
    {
        protected string _logFileName;
        protected long _currentPosition;
        protected StreamReader _logFileStreamReader;
        private byte[] _clrfCheck = new byte[2];
        protected Encoding _logFileEncoding ;
        private bool _reading = false;
        private object _locker = new object();
        protected FileSystemWatcher _watcher;
        public event EventHandler<LineReadedEventArgs> LineReaded;
        public event EventHandler<EventArgs> FinishedReading;

        public LogReaderBase(string logFileName, Encoding encoding)
        {
            _logFileName = logFileName;
            _logFileEncoding = encoding;
            _currentPosition = 0;
            CreateWatcher();
        }

        public LogReaderBase(string logFileName, long currentPosition, Encoding encoding)
        {
            _logFileName = logFileName;
            _logFileEncoding = encoding;
            _currentPosition = currentPosition;
            CreateWatcher();
        }

        void CreateWatcher()
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(_logFileName)), Path.GetFileName(_logFileName));
            _watcher.NotifyFilter = NotifyFilters.Size & NotifyFilters.LastWrite;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed += FileSystemWatcherFired;
            
        }

        protected delegate void ReadInternalDelegate();

        private ReadInternalDelegate _readInternal;

        public void BeginRead()
        {
            _readInternal = new ReadInternalDelegate(ReadInternal);
            _readInternal();
        }


        protected void FileSystemWatcherFired (object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            //Console.Out.WriteLine("FileSystemWatcher Fired");
            _readInternal();
        }

        protected void ReadInternal()
        {
            if (_reading) return;
            lock (_locker)
            {
                if (_reading) return;
                _reading = true;
                _watcher.EnableRaisingEvents = false;
                //Console.Out.WriteLine("Stop watching...");
                string line;
                while ((line = _logFileStreamReader.ReadLine()) != null)
                {
                    _currentPosition += _logFileEncoding.GetByteCount(line);
                    if (!_logFileStreamReader.EndOfStream)
                        if (_logFileStreamReader.Peek() != 10)
                            _currentPosition += 2; // CLRF - 1310
                        else
                            _currentPosition += 1; //RF - 10

                    if (LineReaded != null)
                        LineReaded(this, new LineReadedEventArgs(line));
                }
            }
            if (FinishedReading != null)
                FinishedReading(this, new EventArgs());

            _watcher.EnableRaisingEvents = true;
            //Console.Out.WriteLine("Begin watching...");
            _reading = false;
        }

        protected abstract void CreateReader();


        public void Close()
        {
            if (_logFileStreamReader != null)
            {
                _logFileStreamReader.Close();
                _logFileStreamReader.Dispose();
            }
        }
    }
}
using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace LogReader
{
    public sealed class GzWebLogReader : LogReaderBase
    {
        private GZipInputStream _logFileStream;
        public GzWebLogReader(string logFileName, Encoding encoding):base(logFileName,0,encoding)
        {
            CreateReader();
        }

        public GzWebLogReader(string logFileName, long currentPosition, Encoding encoding): base(logFileName, currentPosition, encoding)
        {
            CreateReader();
        }

        protected override void CreateReader()
        {
            _logFileStream = new GZipInputStream(File.Open(_logFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            if (_currentPosition > 0)
            {
                long seekPosition = 0;
                int readBlockSize = 2;//1024*1024;
                byte[] temp = new byte[readBlockSize];
                while (seekPosition < _currentPosition - readBlockSize)
                {
                    _logFileStream.Read(temp, 0, readBlockSize);
                    seekPosition += readBlockSize;
                }
                _logFileStream.Read(temp, 0, (int)(_currentPosition - seekPosition));
            }
            _logFileStreamReader = new StreamReader(_logFileStream, _logFileEncoding);
        }

        public new void Close()
        {
            base.Close();
            if (_logFileStream != null)
            {
                _logFileStream.Close();
                _logFileStream.Dispose();
            }
        }

    }
}
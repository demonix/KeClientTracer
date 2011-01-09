using System.IO;
using System.Text;

namespace KeClientTracing.LogReading
{
    public sealed class WebLogReader : LogReaderBase
    {
        private FileStream _logFileStream;

        public WebLogReader(string logFileName, Encoding encoding): base(logFileName, 0, encoding)
        {
            CreateReader();
        }

        public WebLogReader(string logFileName, long currentPosition, Encoding encoding)
            : base(logFileName, currentPosition, encoding)
        {
            CreateReader();
        }


        protected override void CreateReader()
        {
            _logFileStream = File.Open(_logFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _logFileStream.Seek(_currentPosition, SeekOrigin.Begin);
            _logFileStreamReader = new StreamReader(_logFileStream, _logFileEncoding);
        }

        public new void Close()
        {
           if (_logFileStreamReader !=null)
                _logFileStreamReader.Dispose();
           
        }
    }
}

using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace KeClientTracing.LogReading
{
    public sealed class ParsedLogReader : LogReaderBase
    {
        private Stream _logFileStream;

        public ParsedLogReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
            _logFileStream = stream;
            CreateReader();
        }

        protected override void CreateReader()
        {
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
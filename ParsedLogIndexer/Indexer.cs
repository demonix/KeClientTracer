using System;
using System.IO;
using System.Text;

namespace ParsedLogIndexer
{
    public class Indexer : IDisposable
    {
        private FileStream _fileStream;
        private StreamReader _streamReader;
        private char _keyDelimiter;
        public string CurrentKey { get; private set; }

        //public string Key { get; private set; }
        public string FirstKeyLine { get; private set; }
        public string LastKeyLine { get; private set; }

        public string LastReadedLine { get; private set; }
        public string LastReadedKey { get; private set; }


        //public string NextKey { get; private set; }
        //public string NextFirstKeyLine { get; private set; }

        public long StartPosition { get; private set; }
        public long EndPosition { get; private set; }
        //public long NextStartPosition { get; private set; }
        private long _currentPosition = 0;
        private long _previousCurrentPosition = 0;
        private int _lineFeedOffset = 0;
        private long _nextStartPos = 0;


        //public string CurrentLine { get; private set; }
        //public string PreviousKey { get; private set; }
        public string PreviousLine { get; private set; }



        public Indexer(FileInfo file, char keyDelimiter)
        {
            _fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _streamReader = new StreamReader(_fileStream, Encoding.Default);
            _keyDelimiter = keyDelimiter;
        }

        public void Dispose()
        {
            _fileStream.Dispose();
            _streamReader.Dispose();
        }

        public bool ReadUpToNextKey()
        {
            if (_streamReader.EndOfStream)
                return false;
            FirstKeyLine = LastReadedLine;
            CurrentKey = LastReadedKey;
            StartPosition = _previousCurrentPosition;
            while ((LastReadedLine = _streamReader.ReadLine()) != null)
            {
                if (LastReadedKey != "" || !LastReadedLine.StartsWith(LastReadedKey))
                {

                    LastReadedKey = LastReadedLine.Substring(0, LastReadedLine.IndexOf(_keyDelimiter)); ;
                }
                //LastReadedKey = LastReadedLine.Split(_keyDelimiter)[0];
                _previousCurrentPosition = _currentPosition;
                _currentPosition += Encoding.Default.GetByteCount(LastReadedLine);
                
                EndPosition = _previousCurrentPosition - _lineFeedOffset;
                
                if (!_streamReader.EndOfStream)
                {
                    _lineFeedOffset = _streamReader.Peek() != 10 ? 2 : 1;
                    _currentPosition += _lineFeedOffset;
                }

                if (CurrentKey != LastReadedKey)
                {
                    if (!String.IsNullOrEmpty(CurrentKey))
                    {
                        LastKeyLine = PreviousLine;
                        PreviousLine = LastReadedLine;
                        break;
                    }
                    CurrentKey = LastReadedKey;
                    FirstKeyLine = LastReadedLine;
                }
                PreviousLine = LastReadedLine;
            }
            if (_streamReader.EndOfStream)
            {
                EndPosition = _currentPosition;
                LastKeyLine = PreviousLine;
            }
            return true;
        }
    }
}
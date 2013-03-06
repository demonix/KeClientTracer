using System;
using System.IO;
using System.Text;
using Common;

namespace KeClientTracing.LogIndexing
{
    public class Indexer: IDisposable
    {
        private Stream _fileStream;
        private StreamReader _streamReader;
        private char _keyDelimiter;
        
        public Indexer(FileInfo file, char keyDelimiter)
        {
            _fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _streamReader = new StreamReader(_fileStream, Encoding.Default);
            _keyDelimiter = keyDelimiter;
        }
        public Indexer(Stream file, char keyDelimiter)
        {
            _fileStream = file;
            _streamReader = new StreamReader(_fileStream, Encoding.Default);
            _keyDelimiter = keyDelimiter;
        } 
          public void Dispose()
        {
            _fileStream.Dispose();
            _streamReader.Dispose();
        }

        private string lastLine = null;
        private long previousEndPos = -1;
        public bool ReadUpToNextKey(out IndexKeyInfo indexKeyInfo)
        {
            indexKeyInfo = null;
            if (_streamReader.EndOfStream)
                return false;
            long startPos = previousEndPos==-1?_streamReader.GetRealPosition():previousEndPos;
            string firstLine = lastLine ?? _streamReader.ReadLine();
            if (firstLine == null)
                return false;
            long endPos = _streamReader.GetRealPosition();
            string key = firstLine.Substring(0, firstLine.IndexOf(_keyDelimiter));
            string sessionStart = firstLine.Split('\t')[1];
            string line;
            string sessionEnd;
            string previousLine = firstLine;
            while ((line = _streamReader.ReadLine()) != null)
            {
                if (line.Substring(0, line.IndexOf(_keyDelimiter)) != key )
                {
                    previousEndPos = endPos;
                    lastLine = line;
                    sessionEnd = previousLine.Split('\t')[1];
                    indexKeyInfo = new IndexKeyInfo(key, startPos, (int) (endPos - startPos), sessionStart, sessionEnd);
                    return true;
                }
                endPos = _streamReader.GetRealPosition();
                previousLine = line;
            }
            sessionEnd = previousLine.Split('\t')[1];
            indexKeyInfo = new IndexKeyInfo(key, startPos, (int)(endPos - startPos), sessionStart, sessionEnd);
            return true;
        }
    }

    public class IndexKeyInfo
    {
        public IndexKeyInfo(string key, long offest, int length, string sessionStartTime, string sessionEndTime)
        {
            Key = key;
            Offest = offest;
            Length = length;
            SessionStartTime = sessionStartTime;
            SessionEndTime = sessionEndTime;
        }

        public string Key { get; private set; }
        public long Offest { get; private set; }
        public int Length { get; private set; }
        public string SessionStartTime{ get; private set; }
        public string SessionEndTime{ get; private set; }
    }
}
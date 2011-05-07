using System;
using System.Text;

namespace KeClientTracing.LogReading
{
    public abstract class LogLine
    {
        internal string[] SplittedLine;

        public LogLine()
        {
        }

        public LogLine(string line)
        {
            Init(line);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SplittedLine.Length; i++)
            {
                sb.AppendFormat("{0}: {1}\r\n", i, SplittedLine[i]);
            }
            return sb.ToString();
        }

        public abstract DateTime RequestDateTime { get; }
        public abstract string ClientIP { get; }
        public abstract string Host { get; }
        public abstract string Uri { get; }
        public abstract string QueryString { get; }
        public abstract string Backend { get; }
        public abstract string Token { get; }
        public abstract string Method { get; }
        public abstract string SessionId { get; }
        public abstract string Sid { get; }
        public abstract string Result { get; }
        public abstract string TimeTaken { get; }
        public abstract string UserAgent { get; }
        public abstract bool HasAllFields { get; }


        public  virtual void FillFromString(string line)
        {
            Init(line);
        }

        private void Init(string line)
        {
            SplittedLine = line.Split('\t');
        }
    }
}
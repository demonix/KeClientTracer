using System;
using System.Globalization;
using System.Text;

namespace KeClientTracing.LogReading
{
    public class ParsedLogLine : LogLine
    {

        public ParsedLogLine()
        {
        }

        public ParsedLogLine(string line)
        {
            Init(line);
        }

        public bool IsStatic ()
        {
            string uri = Uri;
            return uri.EndsWith(".gif") ||
                   uri.EndsWith(".js") ||
                   uri.EndsWith(".css") ||
                   uri.EndsWith(".jpg") ||
                   uri.EndsWith(".png") ||
                   uri.EndsWith("static.ashx");

        }
        public override void FillFromString(string line)
        {
            Init(line);
        }

        private void Init(string line)
        {
            string[] tmp = line.Split('\t');
            if (tmp.Length < 3)
                throw new FormatException(String.Format("[{0}] is not a correct parsed log line", line));
            string[] tmp1 = tmp[0].Split('^');
            SplittedLine = new string[tmp.Length + tmp1.Length];
            Array.Copy(tmp1,0,SplittedLine,0,tmp1.Length);
            Array.Copy(tmp,1,SplittedLine,tmp1.Length,tmp.Length-1);
            //tmp1.CopyTo(SplittedLine, 0);
            //tmp.CopyTo(SplittedLine, tmp1.Length);
        }

        public override bool HasAllFields
        {
            get { return SplittedLine.Length > 17; }
        }
        
        public override DateTime RequestDateTime
        {
            get
            {
                DateTime dt;
                try
                {
                    dt = DateTime.Parse(GetField(0) + " " + GetField(5));
                }
                catch (Exception ex)
                {
                    throw new Exception(ToString(), ex);
                }
                return dt;
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SplittedLine.Length; i++)
            {
                sb.AppendFormat("{0}: {1}\r\n", i,  GetField(i));
            }
            return sb.ToString();
        }

        
        public override string Host
        {
            get { return GetField(1); }
        }
        public override string Method
        {
            get { return GetField(6); }
        }
        public override string Uri
        {
            get { return GetField(7); }
        }
        public override string QueryString
        {
            get { return GetField(8); }
        }
        public override string ClientIP
        {
            get { return GetField(2); }
        }
        public override string UserAgent
        {
            get { return ""; }
        }
        public override string Sid
        {
            get { return GetField(11); }
        }
        public override string Result
        {
            get { return GetField(9); }
        }
        public override string TimeTaken
        {
            get { return GetField(10); }
        }
        public override string SessionId
        {
            get { return GetField(5); }
        }
        public override string Token
        {
            get { return ""; }
        }
        public override string Backend
        {
            get { return GetField(12); }
        }

        private string GetField (int fieldNumber)
        {

            return (SplittedLine[fieldNumber] == "-" ? "" : SplittedLine[fieldNumber]);
        }
        



       

       
    }
}
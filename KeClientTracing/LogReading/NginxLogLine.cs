using System;
using System.Globalization;
using System.Text;

namespace KeClientTracing.LogReading
{
    public class NginxLogLine : LogLine
    {

        public NginxLogLine()
        {
        }

        public NginxLogLine(string line) : base(line)
        {
        }

        public override bool HasAllFields
        {
            get { return SplittedLine.Length > 17; }
        }

        public new void FillFromString(string line)
        {
            base.FillFromString(line);
        }



        public override DateTime RequestDateTime
        {
            get
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dt.AddSeconds(Double.Parse(GetField(1), CultureInfo.InvariantCulture));
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
            get { return GetField(2); }
        }
        public override string Method
        {
            get { return GetField(3); }
        }
        public override string Uri
        {
            get { return GetField(4); }
        }
        public override string QueryString
        {
            get { return GetField(5); }
        }
        public override string ClientIP
        {
            get { return GetField(9); }
        }
        public override string UserAgent
        {
            get { return GetField(10); }
        }
        public override string Sid
        {
            get { return GetField(11); }
        }
        public override string Result
        {
            get { return GetField(12); }
        }
        public override string TimeTaken
        {
            get { return GetField(13); }
        }
        public override string SessionId
        {
            get { return GetField(15); }
        }
        public override string Token
        {
            get { return GetField(16); }
        }
        public override string Backend
        {
            get { return GetField(17); }
        }

        private string GetField (int fieldNumber)
        {

            return (SplittedLine[fieldNumber] == "-" ? "" : SplittedLine[fieldNumber]);
        }
        



       

       
    }
}
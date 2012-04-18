using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KeClientTracing.LogReading
{
    public class NginxLogLine : LogLine
    {
        Dictionary<string, int> _fieldMap = new Dictionary<string, int>();

        const string LogFormat = "$time_local\t$msec\t$host\t$request_method\t$uri\t$args\t$http_referer\t$remote_addr\t$http_user_agent\t$cookie_sid\t$status\t$request_time\t$request_length\t$asp_net_sessionid\t$cookie_token\t$upstream_addr\t$ke_upstream\t$bytes_sent";

        public NginxLogLine()
        {
            string[] logFormat = LogFormat.Split('\t');
            for (int i = 0; i < logFormat.Length; i++)
            {
                string fieldName = logFormat[i].TrimStart('$').Trim();
                if (String.IsNullOrEmpty(fieldName) || _fieldMap.ContainsKey(fieldName))
                    continue;
                _fieldMap.Add(fieldName, i);
            }
        }

        /*public NginxLogLine(string line) : base(line)
        {
        }
        */
        public override bool HasAllFields
        {
            get { return SplittedLine.Length > 15; }
        }


        public override DateTime RequestDateTime
        {
            get
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dt.AddSeconds(Double.Parse(GetField("msec"), CultureInfo.InvariantCulture));
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SplittedLine.Length; i++)
            {
                sb.AppendFormat("{0}: {1}\r\n", i, GetField(i));
            }
            return sb.ToString();
        }

        public override string Host
        {
            get { return GetField("host"); }
        }
        
       
        
        public override string Method
        {
            get { return GetField("request_method"); }
        }
        public override string Uri
        {
            get { return GetField("uri"); }
        }
        public override string QueryString
        {
            get { return GetField("args"); }
        }
        public override string ClientIP
        {
            get { return GetField("remote_addr"); }
        }
        public override string UserAgent
        {
            get { return GetField("http_user_agent"); }
        }
        public override string Sid
        {
            get { return GetField("cookie_sid"); }
        }
        public override string Result
        {
            get { return GetField("status"); }
        }
        public override string TimeTaken
        {
            get { return GetField("request_time"); }
        }
        public override string SessionId
        {
            get { return GetField("asp_net_sessionid"); }
        }
        public override string Token
        {
            get { return GetField("cookie_token"); }
        }
        public override string Backend
        {
            get { return GetField("upstream_addr"); }
        }

        private string GetField(string fieldName)
        {
            if (_fieldMap.ContainsKey(fieldName))
                return GetField(_fieldMap[fieldName]);
            return "";

        }
        private string GetField(int fieldNumber)
        {
            try
            {
                return (SplittedLine[fieldNumber] == "-" ? "" : SplittedLine[fieldNumber]);
            }
            catch (IndexOutOfRangeException ex)
            {

                throw new IndexOutOfRangeException(String.Format("There is no field number {0} in string [{1}]. It has only {2} fields.", fieldNumber, Line, SplittedLine.Length));
            }

        }
    }
}
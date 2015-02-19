using System;
using System.Text;

namespace LogManagerService.DbLayer
{
    public class FindResultEntry
    {
        public string Id { get; private set; }

        public DateTime Date { get; private set; }

        public string Host { get; private set; }

        public string IP { get; private set; }

        public string Inn { get; private set; }

        public TimeSpan SessionEnd { get; private set; }

        public string SessionId { get; private set; }

        public TimeSpan SessionStart { get; private set; }

        public FindResultEntry(string id, DateTime date, string host, string ip, string inn, string sessionId, TimeSpan sessionStart, TimeSpan sessionEnd)
        {
            this.Id = id;
            this.Date = date;
            this.Host = host;
            this.IP = ip;
            this.Inn = inn;
            this.SessionId = sessionId;
            this.SessionStart = sessionStart;
            this.SessionEnd = sessionEnd;
        }

        public string ToSimpleHtml()
        {
            StringBuilder result = new StringBuilder();
            result.Append("<tr>");
            result.AppendFormat("<td><a href=\"../logdata/?id={0}&date={1}\">{0}</a> (<a href=\"../logdata/?id={0}&date={1}&outtype=parsed&showStatic=no\">с описанием</a>)</td>", Id, Date.ToString("dd.MM.yyyy"));
            result.AppendFormat("<td>{0}</td>", Date.ToString("dd.MM.yyyy (ddd)"));
            result.AppendFormat("<td>{0}</td>", Host);
            result.AppendFormat("<td>{0}</td>", Settings.IsKonturIp(IP)? IP +" Контуровский!":IP);
            result.AppendFormat("<td>{0}</td>", Inn);
            result.AppendFormat("<td>{0}</td>", SessionId);
            result.AppendFormat("<td>{0}:{1}:{2}</td>", SessionStart.Hours.ToString("D2"), SessionStart.Minutes.ToString("D2"), SessionStart.Seconds.ToString("D2"));
            result.AppendFormat("<td>{0}:{1}:{2}</td>", SessionEnd.Hours.ToString("D2"), SessionEnd.Minutes.ToString("D2"), SessionEnd.Seconds.ToString("D2"));
            result.Append("</tr>");
            return result.ToString();

        }

    }
}
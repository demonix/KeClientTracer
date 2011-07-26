using System;
using System.Text;

namespace LogManagerService.DbLayer
{
    public class FindResultEntry
    {
        string id;
        DateTime date;
        string host;
        string ip;
        string inn;
        string sessionId;
        TimeSpan sessionStart;
        TimeSpan sessionEnd;

        public FindResultEntry(string id, DateTime date, string host, string ip, string inn, string sessionId, TimeSpan sessionStart, TimeSpan sessionEnd)
        {
            this.id = id;
            this.date = date;
            this.host = host;
            this.ip = ip;
            this.inn = inn;
            this.sessionId = sessionId;
            this.sessionStart = sessionStart;
            this.sessionEnd = sessionEnd;
        }

        public string ToSimpleHtml()
        {
            StringBuilder result = new StringBuilder();
            result.Append("<tr>");
            result.AppendFormat("<td><a href=\"../logdata/?id={0}\">{0}</a> (<a href=\"../logdata/?id={0}&outtype=parsed&showStatic=no\">с описанием</a>)</td>", id);
            result.AppendFormat("<td>{0}</td>", date.ToString("dd.MM.yyyy (ddd)"));
            result.AppendFormat("<td>{0}</td>", host);
            result.AppendFormat("<td>{0}</td>", ip);
            result.AppendFormat("<td>{0}</td>", inn);
            result.AppendFormat("<td>{0}</td>", sessionId);
            result.AppendFormat("<td>{0}:{1}:{2}</td>", sessionStart.Hours.ToString("D2"), sessionStart.Minutes.ToString("D2"), sessionStart.Seconds.ToString("D2"));
            result.AppendFormat("<td>{0}:{1}:{2}</td>", sessionEnd.Hours.ToString("D2"), sessionEnd.Minutes.ToString("D2"), sessionEnd.Seconds.ToString("D2"));
            result.Append("</tr>");
            return result.ToString();

        }
    }
}
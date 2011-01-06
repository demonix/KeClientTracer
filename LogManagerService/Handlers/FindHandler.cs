using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.Text;

namespace LogManagerService.Handlers
{
    public class FindHandler : HandlerBase
    {
        private readonly List<string> _conditions;

        public FindHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
            _conditions = new List<string> { "datebegin", "dateend", "host", "ip", "inn", "sessionid" };
        }

        public override void Handle()
        {
            try
            {
                switch (_httpContext.Request.HttpMethod.ToUpper())
                {
                    case "GET": GetFind();
                        break;
                    default: MethodNotAllowed();
                        break;
                }

            }
            catch (Exception exception)
            {
                WriteResponse(exception.ToString(), HttpStatusCode.InternalServerError, "");
                throw;
            }
        }

        private void GetFind()
        {

            FindResult results = new FindResult();
            using (SqlConnection connection = new SqlConnection("Data Source=;Initial Catalog=WeblogIndex;Integrated security=SSPI"))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText =
                        @"SELECT 
                                        [id]
                                        ,[date]
                                        ,[host]
                                        ,[ip]
                                        ,[inn]
                                        ,[sessionId]
                                        ,[sessionStart]
                                        ,[sessionEnd]
                                        FROM [WeblogIndex].[dbo].[LogIndex]";
                    CreateCondition(command, _httpContext.Request.QueryString);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Guid id = reader.GetGuid(0);
                        DateTime date = reader.GetDateTime(1);
                        string host = reader.GetString(2);
                        string ip = reader.GetString(3);
                        string inn = reader.GetString(4); ;
                        string sessionId = reader.GetString(5); ;
                        DateTime sessionStart = reader.GetDateTime(6);
                        DateTime sessionEnd = reader.GetDateTime(7);
                        results.Add(new FindResultEntry(id,date,host,ip,inn,sessionId,sessionStart,sessionEnd));
                    }
                }
            }
            if (results.Count() >0)
            {
                WriteResponse(results.ToSimpleHtml(),HttpStatusCode.OK,"OK");
            }
            else
            {
                WriteResponse("Nothing found", HttpStatusCode.NotFound, "Nothing found");
            }
        }

        public void CreateCondition(SqlCommand command, NameValueCollection queryString)
        {
            command.CommandText += " where 1=1";

            foreach (var queryStringParameter in queryString.AllKeys)
            {
                if (!String.IsNullOrEmpty(queryString[queryStringParameter]) && _conditions.Contains(queryStringParameter.ToLower()))
                    AddCondition(command, queryStringParameter, queryString[queryStringParameter]);
            }


        }

        private static void AddCondition(SqlCommand command, string name, string value)
        {
            name = name.ToLower();
            switch (name)
            {
                case "datebegin":
                    {
                        command.CommandText += " and date >= @datebegin";
                        command.Parameters.Add("@datebegin", SqlDbType.Date).Value = value;
                        break;
                    }

                case "dateend":
                    {
                        command.CommandText += " and date <= @dateend";
                        command.Parameters.Add("@dateend", SqlDbType.Date).Value = value;
                        break;
                    }
                default:
                    {
                        command.CommandText += String.Format(" and {0} {2} @{1}", name, name, value.Contains("*") ? "like" : "=");
                        command.Parameters.Add(String.Format("@{0}", name), SqlDbType.VarChar).Value = value.Replace("*", "%");
                        break;
                    }
            }
        }

       
    }

    internal class FindResult
    {
        List<FindResultEntry> _findResults= new List<FindResultEntry>();

        public int Count()
        {
            return _findResults.Count;
        }

        public void Add (FindResultEntry findResultEntry)
        {
            _findResults.Add(findResultEntry);
        }
        public string ToSimpleHtml()
        {
            StringBuilder result = new StringBuilder();
            result.Append("<html><table><tr>");
            result.Append("<td>Id</td>");
            result.Append("<td>Date</td>");
            result.Append("<td>Host</td>");
            result.Append("<td>Ip</td>");
            result.Append("<td>INN</td>");
            result.Append("<td>Session Id</td>");
            result.Append("<td>Session Start</td>");
            result.Append("<td>Session End</td>");
            foreach (FindResultEntry findResultEntry in _findResults)
            {
                result.Append(findResultEntry.ToSimpleHtml());
            }
            result.Append("</tr></table></html>");
            return result.ToString();
        }
    }
    internal class FindResultEntry
    {
        Guid id;
        DateTime date;
        string host;
        string ip;
        string inn;
        string sessionId;
        DateTime sessionStart;
        DateTime sessionEnd;

        public FindResultEntry(Guid id, DateTime date, string host, string ip, string inn, string sessionId, DateTime sessionStart, DateTime sessionEnd)
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
            result.AppendFormat("<td>{0}</td>", id);
            result.AppendFormat("<td>{0}</td>", date.ToString("dd.MM.yyyy (ddd)"));
            result.AppendFormat("<td>{0}</td>", host);
            result.AppendFormat("<td>{0}</td>", ip);
            result.AppendFormat("<td>{0}</td>", inn);
            result.AppendFormat("<td>{0}</td>", sessionId);
            result.AppendFormat("<td>{0}</td>", sessionStart.ToString("HH:mm:ss (K)"));
            result.AppendFormat("<td>{0}</td>", sessionEnd.ToString("HH:mm:ss (K)"));
            result.Append("</tr>");
            return result.ToString();

        }
    }
}
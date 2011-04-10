using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using LogManagerService.DbLayer;

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
            List<Condition> conditions = Condition.Parse(_httpContext.Request.QueryString);
            if (conditions.Count == 0)
                BadRequest();
            FindResult results = ServiceState.GetInstance().Db.Find(conditions);
            
            if (results.Count() >0)
            {
                WriteResponse(results.ToSimpleHtml(),HttpStatusCode.OK,"OK");
            }
            else
            {
                WriteResponse("Nothing found", HttpStatusCode.NotFound, "Nothing found");
            }
        }

        

       
    }
}
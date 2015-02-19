using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
                Console.WriteLine(exception);
                Console.WriteLine("Query was: " + _httpContext.Request.RawUrl);
                WriteResponse(exception.ToString(), HttpStatusCode.InternalServerError, "");    
                throw;
            }
        }

        private void GetFind()
        {
            List<Condition> conditions = Condition.Parse(_httpContext.Request.QueryString);
            if (conditions.Count == 0)
            {
                BadRequest();
                return;
            }
            if (!conditions.Any(c => c.Name == "datebegin" || c.Name == "dateend"))
            {
                WriteResponse("Filter by date must be specified", HttpStatusCode.BadRequest, "Bad request");
                return;
            }
            if (!conditions.Any(c => c.Name == "ip" || c.Name == "inn" ||c.Name == "sessionid"))
            {
                WriteResponse("Filter by IP, INN or SessionId must be specified",HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            FindResult results = ServiceState.GetInstance().Db.Find(conditions);

            
             
                    

            if (HasParam("filter"))
            {

                FindResult filteredResults = new FindResult();
                string filterValue = RequestParams("filter");
                Regex regex = new Regex(filterValue, RegexOptions.IgnoreCase);
                foreach (var findResultEntry in results.Items)
                {
                    LogDataPlacementDescription ldpd;
                    string file;
                    LogDataPlacementDescription.GetLdpdAndLogPathInfo(findResultEntry.Id, findResultEntry.Date.ToString("dd-MM-yyyy"), out ldpd, out file);
                    if (ldpd == null || file == null)
                    {
                        continue;
                    }

                    bool matchFound = false;
                    int lineCount = 0;
                    using (var stream = LogDataPlacementDescription.GetOutputDataStream(file, ldpd.Offset, ldpd.Length))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                lineCount++;
                                if (regex.IsMatch(line))
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                        }   
                    }
                    if (matchFound)
                        filteredResults.Items.Add(findResultEntry);
                }
                results = filteredResults;
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

        

       
    }
}
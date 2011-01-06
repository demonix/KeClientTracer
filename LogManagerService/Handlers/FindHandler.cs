using System;
using System.Collections.Generic;
using System.Net;

namespace LogManagerService.Handlers
{
    internal class FindHandler: HandlerBase
    {
        public FindHandler(HttpListenerContext httpContext) : base(httpContext)
        {
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
            List<FindResult> results = new List<FindResult>();
            ConditionCreator conditionCreator = new ConditionCreator(_httpContext);
        }

        
    }

    internal class ConditionCreator
    {
        public ConditionCreator(HttpListenerContext httpContext)
        {
            throw new NotImplementedException();
        }
    }

    internal class FindResult
    {
    }
}
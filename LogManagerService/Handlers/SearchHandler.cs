using System.IO;
using System.Net;

namespace LogManagerService.Handlers
{
    internal class SearchHandler : HandlerBase
    {
        public SearchHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
        }

        public override void Handle()
        {
            WriteResponse(File.ReadAllBytes("search.html"), HttpStatusCode.OK, "Ok");
        }
    }
}
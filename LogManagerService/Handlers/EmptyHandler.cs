using System.Net;

namespace LogManagerService.Handlers
{
    class EmptyHandler : HandlerBase
    {
        public EmptyHandler(HttpListenerContext httpContext) : base(httpContext)
        {
        }

        public override void Handle()
        {
            MethodNotAllowed();
        }
    }
}
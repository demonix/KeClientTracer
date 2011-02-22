using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.ThreadSafeObjects;
using LogManagerService.Handlers;

namespace LogManagerService
{
    class Program
    {
        //static Dictionary<string, string> _lastKnownLogHashes = new Dictionary<string, string>();
        //private static Queue<string> _pendingLogHashes = new Queue<string>();
        //private static ThreadSafeQueue<string> _hashesOfPendingLogs = new ThreadSafeQueue<string>();
        //static Dictionary<string,string> _hashToFile = new Dictionary<string, string>();


        static void Main(string[] args)
        {

           
            //GetFiles(null);
            using (WebServer webServer = new WebServer("http://+:1818/logManager/"))
            {
                ServiceState.GetInstance();
                webServer.IncomingRequest += WebServer_IncomingRequest;
                webServer.Start();
                Console.WriteLine("WebServer started. Press any key to exit.");
                Console.ReadKey();
            }
        }

     



        private static void WebServer_IncomingRequest(object sender, HttpRequestEventArgs e)
        {
            HttpListenerContext httpContext = e.RequestContext;

            try
            {
                HandleRequest(httpContext);
            }
            catch (Exception exception)
            {
                
                Console.WriteLine(exception);
                
            }
            
           
            
        }

        private static void HandleRequest(HttpListenerContext httpContext)
        {
            string[] requestParts = httpContext.Request.Url.AbsolutePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (requestParts.Length < 2)
                new EmptyHandler(httpContext).Handle();
            switch (requestParts[1].ToLower())
            {
                case "stats":
                    new StatsHandlerBase(httpContext).Handle();
                    break;
                case "getnextfile":
                    new NextFileHandler(httpContext).Handle();
                    break;
                case "index":
                    new IndexHandler(httpContext).Handle();
                    break;
                case "find":
                    new FindHandler(httpContext).Handle();
                    break;
                case "logdata":
                    new LogDataHandler(httpContext).Handle();
                    break;
                case "static":
                    new StaticHandler(httpContext).Handle();
                    break;
                default:
                    new EmptyHandler(httpContext).Handle();
                    break;
            }
        }

        private static void PrintResults(HttpRequestEventArgs httpRequestEventArgs)
        {
           
        }

       
    }
}

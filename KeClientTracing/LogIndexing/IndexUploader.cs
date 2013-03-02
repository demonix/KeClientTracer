using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using Common;
using Common.ThreadSafeObjects;
using Common.Web;

namespace KeClientTracing.LogIndexing
{
    public class IndexUploader
    {
        private string _server;

        public IndexUploader(string server)
        {
            _server = server;
        }
        ConcurrentQueue<string> _indexFilesToUploadQueue = new ConcurrentQueue<string>();

        public void Enqueue(string fileName)
        {
            _indexFilesToUploadQueue.Enqueue(fileName);
        }
        
        //TODO: make async and move extension changing to client
        public void StartUpload()
        {
            string fileName;
            while (_indexFilesToUploadQueue.TryDequeue(out fileName))
            {
                DateTime indexDate = FileUtils.FileNameToDate(fileName);
                string data = File.ReadAllText(fileName);
                string uri = String.Format("{0}/index/?date={1}", _server, DateConversions.DateToYmd(indexDate));
                Response response = Request.Post(uri, data);
                if (response.StatusCode != HttpStatusCode.OK)
                    Console.WriteLine("Upload of file {0} was not succeeded. Response code is: {1}. Response text is: {2}.", fileName,
                        response.StatusCode, response.ResponseText);
                else
                {
                       Console.WriteLine("File {0} was succcessfully uploaded",fileName);
                    FileUtils.ChangeExtension(fileName, "uploadedIndex", 10);
                }
            }
        }
    }

}
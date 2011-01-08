using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Common;
using Common.ThreadSafeObjects;
using Common.Web;

namespace KeClientTracing
{
    public class IndexUploader
    {
        private string _server;

        public IndexUploader(string server)
        {
            _server = server;
        }
        ThreadSafeQueue<string> _indexFilesToUploadQueue = new ThreadSafeQueue<string>();

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
                string uri = String.Format("{0}/index/{1}.{2}.{3}", _server, indexDate.Year.ToString("D4"),
                                           indexDate.Month.ToString("D2"), indexDate.Day.ToString("D2"));
                Response response = Request.Post(uri, data);
                if (response.StatusCode != HttpStatusCode.OK)
                    Console.WriteLine("Upload of file {0} was not succeeded. Response code is: {1}. Response text is: {2}.", fileName,
                        response.StatusCode, response.ResponseText);
                else
                    FileUtils.ChangeExtension(fileName, "uploadedIndex", 10);
            }
        }
    }

}
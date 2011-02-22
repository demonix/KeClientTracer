using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using Common;

namespace LogManagerService.Handlers
{
    public class LogDataHandler : HandlerBase
    {
        public LogDataHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
        }

        public override void Handle()
        {
            try
            {
                switch (_httpContext.Request.HttpMethod.ToUpper())
                {
                    case "GET": GetLogData();
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

        private void GetLogData()
        {
            Guid entryId = new Guid(RequestParams("id"));
            string outType = HasParam("outType") ?RequestParams("outType").ToLower():"";
            DateTime date = new DateTime();
            long offset = 0;
            long length = 0;
            using (SqlConnection connection = new SqlConnection(Settings.WeblogIndexDbConnectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText =
                        @"SELECT [date], [startLogPos], [endLogPos] FROM [WeblogIndex].[dbo].[LogIndex] where id = @id";
                    command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = entryId;

                    SqlDataReader reader = command.ExecuteReader();
                    
                    if (!reader.HasRows)
                    {
                        WriteResponse("no such id", HttpStatusCode.NotFound, "no such id");
                        return;
                    }
                    while (reader.Read())
                    {
                      
                        date = reader.GetDateTime(0);
                        offset = reader.GetInt64(1);
                        length = reader.GetInt64(2);

                    }
                }


            }
            string file = FileUtils.DateToFileName(".\\logs\\sorted\\", date, "sorted");
            
            switch (outType)
            {
                case "simpletxt":
                    SimpleTxtOutput(file, offset, length);
                    break;
                default:
                    DefaultOutput(file, offset, length);
                    break;
            }
            
        }

        private void SimpleTxtOutput(string file, long offset, long length)
        {
            StringBuilder sb = new StringBuilder();
            using (Stream stream = GetOutputDataStream(file, offset, length))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] split = line.Split(new[] { '\t' }, 2);
                        if (split.Length == 2)
                            sb.AppendFormat("{0}\r\n",split[1]);
                    }
                }
            }
            WriteResponse(Encoding.UTF8.GetBytes(sb.ToString()), HttpStatusCode.OK, "OK");
        }

        private void DefaultOutput(string file, long offset, long length)
        {
            using (Stream stream = GetOutputDataStream(file, offset, length))
            {
                WriteResponse(((MemoryStream)stream).GetBuffer(), HttpStatusCode.OK, "OK");
            }
        }

        private Stream GetOutputDataStream(string file, long offset, long length)
        {
            Stream st = new MemoryStream();
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.CopyTo(st,length);
            }
            st.Seek(0, SeekOrigin.Begin);
            return st;
        }
    }
}
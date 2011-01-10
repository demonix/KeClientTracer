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
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                byte[] output = new byte[length];
                fs.Read(output, 0, (int)length);
                WriteResponse(output,HttpStatusCode.OK,"OK");
            }
        }
    }
}
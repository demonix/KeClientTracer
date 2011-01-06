using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace LogManagerService.Handlers
{
    public class IndexHandler : HandlerBase
    {
        public IndexHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
        }

        public override void Handle()
        {
            try
            {
                switch (_httpContext.Request.HttpMethod.ToUpper())
                {
                    case "POST": PutIndex();
                        break;
                    default: MethodNotAllowed();
                        break;
                }

            }
            catch (Exception exception)
            {
                WriteResponse(exception.ToString(),HttpStatusCode.InternalServerError,"");
                throw;
            }
            
        }

        private void PutIndex()
        {
            RemoveExistingIndex();
            SaveIndex();
            WriteResponse("OK",HttpStatusCode.OK,"OK");
        }

        private void SaveIndex()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("id", typeof(Guid));
            dataTable.Columns.Add("date", typeof(DateTime));
            dataTable.Columns.Add("host", typeof(string));
            dataTable.Columns.Add("ip", typeof(string));
            dataTable.Columns.Add("inn", typeof(string));
            dataTable.Columns.Add("sessionId", typeof(string));
            dataTable.Columns.Add("startLogPos", typeof(Int64));
            dataTable.Columns.Add("endLogPos", typeof(Int64));
            dataTable.Columns.Add("sessionStart", typeof(TimeSpan));
            dataTable.Columns.Add("sessionEnd", typeof(TimeSpan));
            StreamReader streamReader = new StreamReader(_httpContext.Request.InputStream);
            string line;
            while ( (line = streamReader.ReadLine()) != null)
            {
                string[] data = line.Split('\t');
                DataRow dataDow = dataTable.NewRow();
               
                    dataDow["id"] = Guid.NewGuid();
                    dataDow["date"] = data[0];
                    dataDow["host"] = data[1];
                    dataDow["ip"] = data[2];
                    dataDow["inn"] = data[3];
                    dataDow["sessionId"] = data[4];
                    dataDow["startLogPos"] = data[5];
                    dataDow["endLogPos"] = data[6];
                    dataDow["sessionStart"] =  DateTime.Parse(data[7]).ToUniversalTime().TimeOfDay;
                    dataDow["sessionEnd"] = DateTime.Parse(data[8]).ToUniversalTime().TimeOfDay;
                    dataTable.Rows.Add(dataDow);
                
               
            }
           
                SqlBulkCopy bulk = new SqlBulkCopy("Data Source=;Initial Catalog=WeblogIndex;Integrated security=SSPI");
                bulk.DestinationTableName = "LogIndex";
                bulk.WriteToServer(dataTable);
           
            
        }

        private void RemoveExistingIndex()
        {
            using (SqlConnection connection = new SqlConnection("Data Source=;Initial Catalog=WeblogIndex;Integrated security=SSPI"))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand( "delete from LogIndex where date = @dateToDelete", connection))
                {
                    //command.Connection = connection;
                    string date = RequestParams("date");
                    if (String.IsNullOrEmpty(date))
                        throw new Exception("date parameter must be specified");
                    string[] dateParts = RequestParams("date").Split('.');
                    DateTime dt = new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
                    //command.CommandText =;
                    command.Parameters.Add("@dateToDelete", SqlDbType.Date).Value = dt;
                    
                    int i = command.ExecuteNonQuery();
                    Console.Out.WriteLine(i);
                }
            }
        }
    }
}
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
                    case "POST": PostIndex();
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

        private string _date;
        private void PostIndex()
        {
            _date = RequestParams("date");
            if (String.IsNullOrEmpty(_date))
                throw new Exception("date parameter must be specified");
            Console.Out.WriteLine("Uploading index for " + _date);
            Console.Out.WriteLine("Removing old data for " + _date);
            RemoveExistingIndex();
            Console.Out.WriteLine("Old data removed for " + _date);
            Console.Out.WriteLine("Saving new data for " + _date);
            SaveIndex();
            Console.Out.WriteLine("New data saved for " + _date);
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
            while ((line = streamReader.ReadLine()) != null)
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
                TimeSpan timeSpan;
                try
                {
                    timeSpan = data.Length >= 8
                                   ? DateTime.Parse(data[7]).ToUniversalTime().TimeOfDay
                                   : TimeSpan.Parse("00:00:00");

                    
                }catch(Exception exception)
                {
                    //throw new Exception("Error while reading sessionStart from string " + line + "; value "+ data[7] , exception);
                    Console.Out.WriteLine("Error while reading sessionStart from string " + line + "; value " + data[7] + "\r\n" + exception);
                    timeSpan = TimeSpan.Parse("00:00:00");
                }
                dataDow["sessionStart"] = timeSpan;

                try
                {
                    timeSpan = data.Length >= 9
                                   ? DateTime.Parse(data[8]).ToUniversalTime().TimeOfDay
                                   : TimeSpan.Parse("00:00:00");

                    
                }
                catch (Exception exception)
                {
                    //throw new Exception("Error while reading sessionEnd from string " + line + "; value " + data[7], exception);
                    Console.Out.WriteLine("Error while reading sessionEnd from string " + line + "; value " + data[7] +"\r\n"+ exception);
                    timeSpan = TimeSpan.Parse("00:00:00");
                }
                dataDow["sessionEnd"] = timeSpan;
                dataTable.Rows.Add(dataDow);


            }


            using (SqlBulkCopy bulk = new SqlBulkCopy(Settings.WeblogIndexDbConnectionString))
            {
                bulk.DestinationTableName = "LogIndex";
                bulk.ColumnMappings.Clear();
                bulk.ColumnMappings.Add("id", "id");
                bulk.ColumnMappings.Add("date", "date");
                bulk.ColumnMappings.Add("host", "host");
                bulk.ColumnMappings.Add("ip", "ip");
                bulk.ColumnMappings.Add("inn", "inn");
                bulk.ColumnMappings.Add("sessionId", "sessionId");
                bulk.ColumnMappings.Add("startLogPos", "startLogPos");
                bulk.ColumnMappings.Add("endLogPos", "endLogPos");
                bulk.ColumnMappings.Add("sessionStart", "sessionStart");
                bulk.ColumnMappings.Add("sessionEnd", "sessionEnd");
                bulk.BulkCopyTimeout = 10000;
                bulk.WriteToServer(dataTable);
            }


        }

        private void RemoveExistingIndex()
        {
            
           

            using (SqlConnection connection = new SqlConnection(Settings.WeblogIndexDbConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand( "delete from LogIndex where date = @dateToDelete", connection))
                {
                    //command.Connection = connection;
                    command.CommandTimeout = 1000;
                    
                    
                    string[] dateParts = RequestParams("date").Split('.');
                    DateTime dt = new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
                    //command.CommandText =;
                    command.Parameters.Add("@dateToDelete", SqlDbType.Date).Value = dt;
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
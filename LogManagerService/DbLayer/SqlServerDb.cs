using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace LogManagerService.DbLayer
{
    public class SqlServerDb : IDb
    {
        public void RemoveIndexEntires(DateTime date)
        {
            using (SqlConnection connection = new SqlConnection(Settings.WeblogIndexDbConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("delete from LogIndex where date = @dateToDelete", connection))
                {
                    command.CommandTimeout = 1000;
                    command.Parameters.Add("@dateToDelete", SqlDbType.Date).Value = date;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveIndexEntries(Stream stream)
        {
            DataTable dataTable = CreateDataTable();
            StreamReader streamReader = new StreamReader(stream);
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                DataRow dataDow = dataTable.NewRow();
                FillDataRow(line, dataDow);
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
                bulk.BulkCopyTimeout = 100000;
                bulk.WriteToServer(dataTable);
            }
        }

        public LogDataPlacementDescription GetLogDataPlacementDescription(Guid entryId)
        {
            DateTime date = DateTime.MinValue;
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
                        return null;
                    }
                    while (reader.Read())
                    {
                        date = reader.GetDateTime(0);
                        offset = reader.GetInt64(1);
                        length = reader.GetInt64(2);
                    }

                }
            }
            return  new LogDataPlacementDescription(date,offset,length);
        }

        private void FillDataRow(string line, DataRow dataDow)
        {
            string[] data = line.Split('\t');
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
            }
            catch (Exception exception)
            {
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
                Console.Out.WriteLine("Error while reading sessionEnd from string " + line + "; value " + data[7] + "\r\n" + exception);
                timeSpan = TimeSpan.Parse("00:00:00");
            }
            dataDow["sessionEnd"] = timeSpan;
        }

        private DataTable CreateDataTable()
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
            return dataTable;
        }
    }
}
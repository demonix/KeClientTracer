using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace LogManagerService.DbLayer
{
    public class SqlServerDb :  IDb
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

        public FindResult Find(List<Condition> conditions)
        {
             FindResult results = new FindResult();
            using (SqlConnection connection = new SqlConnection(Settings.WeblogIndexDbConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText =
                        @"SELECT [id] ,[date] ,[host] ,[ip] ,[inn] ,[sessionId] ,[sessionStart] ,[sessionEnd] FROM [WeblogIndex].[dbo].[LogIndex] where 1=1";
                    foreach (Condition condition in conditions)
                    {
                        AddCondition(command, condition);
                    }   
                    command.CommandText += " order by date, host";
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Guid id = reader.GetGuid(0);
                        DateTime date = reader.GetDateTime(1);
                        string host = reader.GetString(2);
                        string ip = reader.GetString(3);
                        string inn = reader.GetString(4); ;
                        string sessionId = reader.GetString(5); ;
                        TimeSpan sessionStart = reader.GetTimeSpan(6);
                        TimeSpan sessionEnd = reader.GetTimeSpan(7);
                        results.Add(new FindResultEntry(id, date, host, ip, inn, sessionId, sessionStart, sessionEnd));
                    }
                }
            }
            return results;
        }

        private static void AddCondition(SqlCommand command, Condition condition)
        {
            switch (condition.Name)
            {
                case "datebegin":
                    {
                        command.CommandText += " and date >= @datebegin";
                        command.Parameters.Add("@datebegin", SqlDbType.Date).Value = condition.Value;
                        break;
                    }

                case "dateend":
                    {
                        command.CommandText += " and date <= @dateend";
                        command.Parameters.Add("@dateend", SqlDbType.Date).Value = condition.Value;
                        break;
                    }
                default:
                    {
                        command.CommandText += String.Format(" and {0} {1} @{0}", condition.Name,
                                                             condition.ComparisonType == ComparisonType.Like ? "like" : "=");
                        command.Parameters.Add(String.Format("@{0}", condition.Name), SqlDbType.VarChar).Value = condition.Value.Replace("*", "%");
                        break;
                    }
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
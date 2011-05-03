using System;

namespace KeClientTracing.LogIndexing
{
    public class IndexLineHelper
    {
        public static DateTime GetDate(string[] data)
        {
            return DateTime.SpecifyKind(DateTime.Parse(data[0]), DateTimeKind.Utc);
        }
        public static string GetHost(string[] data)
        {
            return data[1];
        }

        public static string GetIP(string[] data)
        {
            return data[2];
        }

        public static string GetINN(string[] data)
        {
            return data[3];
        }

        public static string GetSessionId(string[] data)
        {
            return data[4];
        }

        public static long GetStartLogPos(string[] data)
        {
            return Convert.ToInt64(data[5]);
        }
        
        public static long GetEndLogPos(string[] data)
        {
            return Convert.ToInt64(data[6]);
        }

        public static TimeSpan GetSessionStartTime(string[] data)
        {
            TimeSpan timeSpan;
            try
            {
                timeSpan = data.Length >= 8
                               ? DateTime.Parse(data[7]).ToUniversalTime().TimeOfDay
                               : TimeSpan.Parse("00:00:00");
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine("Error while reading sessionStart from string " + "; value " + data[7] + "\r\n" + exception);
                timeSpan = TimeSpan.Parse("00:00:00");
            }
            return timeSpan;
        }
         public static TimeSpan GetSessionEndTime(string[] data)
        {
            TimeSpan timeSpan;
            try
            {
                timeSpan = data.Length >= 9
                               ? DateTime.Parse(data[8]).ToUniversalTime().TimeOfDay
                               : TimeSpan.Parse("00:00:00");
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine("Error while reading sessionStart from string " + "; value " + data[8] + "\r\n" + exception);
                timeSpan = TimeSpan.Parse("00:00:00");
            }
            return timeSpan;
        }

    }
}
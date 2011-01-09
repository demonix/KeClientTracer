using System;

namespace Common
{
    public class DateConversions
    {
        public static DateTime YmdToDate(string ymd)
        {
            string[] dateParts = ymd.Split('.');
            return new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
        }

        public static DateTime DmyToDate(string dmy)
        {
            string[] dateParts = dmy.Split('.');
            return new DateTime(Convert.ToInt32(dateParts[2]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[0]));
        }


        public static string DateToYmd(DateTime date)
        {
            return String.Format("{0}.{1}.{2}", date.Year.ToString("D4"), date.Month.ToString("D2"),
                                 date.Day.ToString("D2"));
        }
        public static string DateToDmy(DateTime date)
        {
            return String.Format("{0}.{1}.{2}", date.Day.ToString("D2"), date.Month.ToString("D2"),
                                 date.Day.ToString("D4"));
        }

    }
}
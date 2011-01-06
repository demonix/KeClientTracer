using System;
using System.IO;
using System.Text;

namespace LogSorter
{
    public class FileNameHelpers
    {
        public static DateTime FileNameToDate(string file)
        {
            string fileName = Path.GetFileName(file);
            if (fileName == null)
                throw new ArgumentException(String.Format("Некорректное имя файла: {0}", file), file);

            string date = fileName.Substring(0, 10);
            string[] dateParts = date.Split('.');
            return new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]),
                                Convert.ToInt32(dateParts[2]));

        }

        public static string DateToFileName(string folder, DateTime date, string extension)
        {
            StringBuilder fileNameBuilder = new StringBuilder();
            if (!String.IsNullOrEmpty(folder))
                fileNameBuilder.AppendFormat("{0}\\", folder.TrimEnd('\\'));
            fileNameBuilder.AppendFormat("{0}.{1}.{2}", date.Year.ToString("D4"), date.Month.ToString("D2"), date.Day.ToString("D2"));
            if (!String.IsNullOrEmpty(extension))
                fileNameBuilder.AppendFormat(".{0}", extension);
            return fileNameBuilder.ToString();
        }
    }
}
using System;
using System.IO;
using System.Text;

namespace Common
{
    public class FileUtils
    {
        public static DateTime FileNameToDate(string file)
        {
            string fileName = Path.GetFileName(file);
            if (fileName == null)
                throw new ArgumentException(String.Format("Некорректное имя файла: {0}", file), file);

            string date = fileName.Substring(0, 10);
            return DateConversions.YmdToDate(date);
        }

        public static string DateToFileName(string folder, DateTime date, string extension)
        {
            StringBuilder fileNameBuilder = new StringBuilder();
            if (!String.IsNullOrEmpty(folder))
                fileNameBuilder.AppendFormat("{0}\\", folder.TrimEnd('\\'));
            fileNameBuilder.AppendFormat(DateConversions.DateToYmd(date));
            if (!String.IsNullOrEmpty(extension))
                fileNameBuilder.AppendFormat(".{0}", extension);
            return fileNameBuilder.ToString();
        }

        public static string ChangeExtension(string file, string newExtension, int tries)
        {
            for (int i = 0; i < tries; i++)
            {
                string destFileName;
                destFileName = Path.ChangeExtension(file, i != 0 ? String.Format("{0}.{1}", i.ToString("D4"), newExtension) : newExtension);
                if (File.Exists(destFileName)) continue;
                File.Move(file, destFileName);
                return destFileName;
            }
            throw new Exception(String.Format("Can't change extension of file {0} to {1} after {2} tries. File with new extension already exists.", file, newExtension, tries));
        }
    }
}
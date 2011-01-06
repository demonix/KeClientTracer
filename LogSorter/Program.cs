using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace LogSorter
{
    class Program
    {
        static List<Sorter> _sorters = new List<Sorter>();
        static Semaphore sem = new Semaphore(7, 7);
        static void Main(string[] args)
        {
            string folder = "logs";
            List<DateTime> fileDates = GetFileDates(folder);
            foreach (DateTime fileDate in fileDates)
            {

                if (fileDate < DateTime.Now.AddDays(-1))
                {
                    Sorter sorter = new Sorter(folder, fileDate, 200, sem);
                    _sorters.Add(sorter);
                }
            }

            foreach (Sorter sorter in _sorters)
            {
                sem.WaitOne();
                sorter.Start();

            }
            foreach (Sorter sorter in _sorters)
                sorter.WaitForExit();
            
        }

       
        static List<DateTime> GetFileDates(string folderPath)
        {
            List<DateTime> result = new List<DateTime>();
            string[] files = Directory.GetFiles(folderPath,"*.requestData");
            foreach (string file in files)
            {
                DateTime dt = FileNameHelpers.FileNameToDate(file);
                if (!result.Contains(dt))
                    result.Add(dt);
            }
            return result;
        }


       
            
    }
}

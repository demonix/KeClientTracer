using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Common;

namespace LogSorter
{
    class Program
    {
        static List<Sorter> _sorters = new List<Sorter>();
        private const int SimultaneousProcessCount = 5;
        private static Semaphore sem;
        static void Main(string[] args)
        {
            int simultaneousProcessCount = 0;
            if (args.Length != 0)
                Int32.TryParse(args[0], out simultaneousProcessCount);

            if (simultaneousProcessCount == 0)
                simultaneousProcessCount = SimultaneousProcessCount;
            sem = new Semaphore(simultaneousProcessCount, simultaneousProcessCount);
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
                DateTime dt = FileUtils.FileNameToDate(file);
                if (!result.Contains(dt))
                    result.Add(dt);
            }
            return result;
        }


       
            
    }
}

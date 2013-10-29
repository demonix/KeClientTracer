using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Common;
using KeClientTracing.LogIndexing;

namespace ParsedLogIndexer
{
    class Program
    {

        static void Main(string[] args)
        {
            Settings settings = Settings.GetInstance();
            string dir = settings.TryGetValue("UnsortedLogsDirectory");
            if (String.IsNullOrEmpty(dir))
                throw new Exception("UnsortedLogsDirectory not specified");

            string fullDirectoryPath = Path.GetFullPath(dir);

            if (!Directory.Exists(fullDirectoryPath)) return;

            FileInfo[] files = new DirectoryInfo(fullDirectoryPath).GetFiles("*.sorted");
            foreach (FileInfo file in files)
            {
                string indexFileName = String.Format("{0}\\{1}.index", file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName));
                string uploadedIndexFileName = String.Format("{0}\\{1}.uploadedIndex", file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName));
                
                if (File.Exists(indexFileName))
                {
                    
                    FileInfo indexFileInfo = new FileInfo(indexFileName);
                    if (indexFileInfo.LastWriteTimeUtc < file.LastWriteTimeUtc)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Index for " + Path.GetFileNameWithoutExtension(file.FullName) +
                                          " already exists, but log file is newer.");
                        File.Delete(indexFileName);
                    }
                    else
                        continue;
                }

                if (File.Exists(uploadedIndexFileName))
                {

                    FileInfo uploadedIndexFileInfo = new FileInfo(uploadedIndexFileName);
                    if (uploadedIndexFileInfo.LastWriteTimeUtc < file.LastWriteTimeUtc)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Uploaded index for " + Path.GetFileNameWithoutExtension(file.FullName) + " already exists, but log file is newer.");
                        File.Delete(uploadedIndexFileName);
                    }
                    else
                        continue;
                }

                
                RunNewIndexer(file, indexFileName);
                
            }
        }
        private static void RunNewIndexer(FileInfo file, string indexFileName)
        {
            using (StreamWriter indexFile = new StreamWriter(new FileStream(indexFileName,FileMode.Create,FileAccess.Write,FileShare.Read)))
            using (Indexer indexer = new Indexer(file, '\t'))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                long i = 0;
                IndexKeyInfo indexKeyInfo;
                while (indexer.ReadUpToNextKey(out indexKeyInfo))
                {
                    i++;
                    if (i % 1000 == 0)
                        Console.Write("\rSpeed: {0} mb/min                      ",
                                      ((double)(indexKeyInfo.Offest+indexKeyInfo.Length) / 1024 / 1024) /
                                      ((double)sw.ElapsedMilliseconds / 100 / 60));
                    try
                    {
                        string s = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n",
                                                 indexKeyInfo.Key.Replace("^", "\t"),
                                                 indexKeyInfo.Offest,
                                                 indexKeyInfo.Length,
                                                 indexKeyInfo.SessionStartTime,
                                                 indexKeyInfo.SessionEndTime);
                        indexFile.Write(s);
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(
                            "Error {0} while indexing line with key {1}. First line: {2}. Last line: {3}",
                            ex.Message, "-", "-", "-");
                            //indexer.CurrentKey, indexer.FirstKeyLine, indexer.LastKeyLine);
                    }
                    sw.Stop();

                }
            }
        }

    }
}

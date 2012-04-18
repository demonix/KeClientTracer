using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ParsedLogIndexer
{
    class Program
    {

        static void Main(string[] args)
        {
            string fullDirectoryPath = Path.GetFullPath(args[0]);

            if (args.Length != 1 || !Directory.Exists(fullDirectoryPath)) return;

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
                        Console.WriteLine("Index for " + Path.GetFileNameWithoutExtension(file.FullName) + " already exists, but log file is newer.");
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
                        Console.WriteLine("Uploaded index for " + Path.GetFileNameWithoutExtension(file.FullName) + " already exists, but log file is newer.");
                        File.Delete(uploadedIndexFileName);
                    }
                    else
                        continue;
                }

                long i = 0;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                using (Indexer indexer = new Indexer(file, '\t'))
                {
                    
                    while (indexer.ReadUpToNextKey())
                    {
                    if (i%1000 ==0)
                        Console.Write("\rSpeed: {0} mb/min                      ",((double)indexer.EndPosition/1024/1024)/((double)sw.ElapsedMilliseconds/100/60));   
                        try
                        {
                            string s = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n",
                                                 indexer.CurrentKey.Replace("^", "\t"),
                                                 indexer.StartPosition,
                                                 indexer.EndPosition -
                                                 indexer.StartPosition + 2,
                                                 indexer.FirstKeyLine.Split('\t')[1],
                                                 indexer.LastKeyLine.Split('\t')[1]);
                            WriteIndexEntry(indexFileName, s);
                        }
                        catch(Exception ex)
                        {
                            Console.Out.WriteLine(
                                "Error {0} while indexing line with key {1}. First line: {2}. Last line: {3}",
                                ex.Message, indexer.CurrentKey, indexer.FirstKeyLine, indexer.LastKeyLine);
                        }
                    }
                }
                sw.Stop();

            }
        }
     /*   static void OldMain(string[] args)
        {
            string fullDirectoryPath = Path.GetFullPath(args[0]);

            if (args.Length != 1 || !Directory.Exists(fullDirectoryPath)) return;

            FileInfo[] files = new DirectoryInfo(fullDirectoryPath).GetFiles("*.sorted");
            foreach (FileInfo file in files)
            {
                string indexFileName = String.Format("{0}\\{1}.index", file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName));
                if (File.Exists(indexFileName))
                    continue;

                using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fileStream, Encoding.Default))
                    {
                        string previousKey = "";
                        string previousLine = "";
                        long previousPosition = 0;
                        string line;
                        string fkl = "";

                        long currentPosition = 0;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string currentKey = line.Split('\t')[0];
                            if (currentKey != previousKey)
                            {

                                if (previousKey != "")
                                    try
                                    {
                                        WriteIndexEntry(indexFileName, previousKey, previousPosition,
                                                        currentPosition - previousPosition, fkl.Split('\t')[1],
                                                        previousLine.Split('\t')[1]);
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(line);
                                        Console.WriteLine(previousLine);
                                        throw;
                                    }
                                fkl = line;
                                previousKey = currentKey;
                                previousPosition = currentPosition;
                            }


                            currentPosition += Encoding.Default.GetByteCount(line);
                            if (!sr.EndOfStream)
                            {
                                if (sr.Peek() != 10)
                                    currentPosition += 2; //CLRF - 1310
                                else
                                    currentPosition += 1; //RF - 10

                                previousLine = line;
                            }
                        }
                        if (previousKey != "")
                            try
                            {
                                WriteIndexEntry(indexFileName, previousKey, previousPosition,
                                                currentPosition - previousPosition, fkl.Split('\t')[1],
                                                previousLine.Split('\t')[1]);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(line);
                                Console.WriteLine(previousLine);
                                throw;
                            }
                    }
                }
            }
        }
        */
      /*  private static void WriteIndexEntry(string indexFileName, string key, long position, long length, string startTime, string endTime)
        {
            File.AppendAllText(indexFileName, key.Replace("^", "\t") + "\t" + position + "\t" + length + "\t" + startTime + "\t" + endTime + "\r\n");
        }
        */
        private static void WriteIndexEntry(string indexFileName,string data)
        {
            File.AppendAllText(indexFileName, data);
        }

        private static void WriteIndexEntry(string indexFileName, string key, long position, long length)
        {
            File.AppendAllText(indexFileName, key.Replace("^", "\t") + "\t" + position + "\t" + length + "\r\n");
        }
    }
}

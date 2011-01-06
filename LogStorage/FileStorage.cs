using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogStorage
{
    public class FileStorage
    {
        public string FileName { get; private set; }

        public FileStorage(string fileName)
        {
            FileName = fileName;
        }

        public FileStorageWriteResult Append(string data)
        {
            try
            {
                long offset;
                byte[] binaryData = Encoding.UTF8.GetBytes(data);
                using (FileStream fileStream = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    offset = fileStream.Position;
                    
                    fileStream.Write(binaryData, 0, binaryData.Length);
                }
                return new FileStorageWriteResult(true,null,offset,binaryData.Length);
            }
            catch (Exception exception)
            {
                return new FileStorageWriteResult(false, exception.ToString(),0,0);
            }
            
        }
    }
}

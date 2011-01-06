namespace LogStorage
{
    public class FileStorageWriteResult
    {
        public bool Success { get; private set; }
        public string Error { get; private set; }
        public long Offset { get; private set; }
        public long Size { get; private set; }

        public FileStorageWriteResult(bool success, string error, long offset,long size)
        {
            Success = success;
            Error = error;
            Offset = offset;
            Size = size;
        }
    }
}
using System;

namespace LogManagerService.DbLayer
{
    public class LogDataPlacementDescription
    {
        public LogDataPlacementDescription(DateTime date, long offset, long length)
        {
            Date = date;
            Offset = offset;
            Length = length;
        }

        public DateTime Date { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }
    }
}
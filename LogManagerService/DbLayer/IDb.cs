using System;
using System.IO;

namespace LogManagerService.DbLayer
{
    public interface IDb
    {
        void RemoveIndexEntires(DateTime date);
        void SaveIndexEntries(Stream stream);
        LogDataPlacementDescription GetLogDataPlacementDescription(Guid id);
    }
}
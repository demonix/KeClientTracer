using System;
using System.Collections.Generic;
using System.IO;

namespace LogManagerService.DbLayer
{
    public interface IDb
    {
        void RemoveIndexEntires(DateTime date);
        void SaveIndexEntries(Stream stream);
        FindResult Find(List<Condition> conditions);
        LogDataPlacementDescription GetLogDataPlacementDescription(string entryId);
    }
}
using System;

namespace LogSorter
{
    public interface ISorter
    {
        void Start();
        void WaitForExit();
        event EventHandler Finished;
    }
}
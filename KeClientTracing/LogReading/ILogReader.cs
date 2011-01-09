using System;

namespace KeClientTracing.LogReading
{
    public interface ILogReader
    {
        event EventHandler<LineReadedEventArgs> LineReaded;
        event EventHandler<EventArgs> FinishedReading;
        void BeginRead();
        void Close();
    }
}
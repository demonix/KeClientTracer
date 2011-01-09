using System;

namespace KeClientTracing.LogReading
{
    public class LineReadedEventArgs : EventArgs
    {
        public string Line { get; private set; }

        public LineReadedEventArgs(string line)
        {
            Line = line;
        }
    }
}
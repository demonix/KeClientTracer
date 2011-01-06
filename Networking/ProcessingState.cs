namespace Networking
{
    public class ProcessingState
    {
        public int BytesLeft { get; set; }
        public int DataBufferOffset { get; set; }
        public int BytesOfPreviousPart { get; set; }

        public ProcessingState()
        {
        }

        public ProcessingState(int dataRead, int dataOffset, int restOfData)
        {
            BytesLeft = dataRead;
            DataBufferOffset = dataOffset;
            BytesOfPreviousPart = restOfData;
        }
    }
}
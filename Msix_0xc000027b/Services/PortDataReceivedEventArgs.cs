namespace Msix_0xc000027b.Services
{
    public class PortDataReceivedEventArgs : EventArgs
    {
        public PortDataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
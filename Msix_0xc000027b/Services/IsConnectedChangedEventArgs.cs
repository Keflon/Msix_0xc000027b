namespace Msix_0xc000027b.Services
{
    public class IsConnectedChangedEventArgs : EventArgs
    {
        public IsConnectedChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }
}
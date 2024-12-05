using Msix_0xc000027b.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;

namespace Msix_0xc000027b
{
    public partial class MainPage : ContentPage
    {
        private SerialPortAdapter _portAdapter;

        public ObservableCollection<string> IncomingSerialData { get; }

        public string Test => "TestString";

        public MainPage()
        {
            IncomingSerialData = new();

            InitializeComponent();

            _portAdapter = new SerialPortAdapter();

            _portAdapter.PortDataReceived += _portAdapter_PortDataReceived;
        }

        // DSAFE: We're on the UI thread.
        private void _portAdapter_PortDataReceived(object? sender, PortDataReceivedEventArgs e)
        {
            foreach (var ch in e.Data)
            {
                if (ch == 0x10)
                    IncomingSerialData.Add("");

                IncomingSerialData[IncomingSerialData.Count - 1] += (char)ch;

                if (IncomingSerialData.Count > 100)
                    IncomingSerialData.Add("");
            }
        }

        private void theButton_Clicked(object sender, EventArgs e)
        {
            if (_portAdapter.IsConnected)
            {
                _portAdapter.Detach();
            }

            IncomingSerialData.Clear();
            if (_portAdapter.TryAttachTo(theEntry.Text) == false)
            {
                IncomingSerialData.Add("Fail");
            }
            else
            {
                IncomingSerialData.Clear();
                IncomingSerialData.Add("> ");
                _ = _portAdapter.WriteAsync(Encoding.UTF8.GetBytes(".DID"));
            }


            SemanticScreenReader.Announce("Click");
        }
    }

}

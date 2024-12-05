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

            // Dispatch incoming data onto the UI thread.
            _portAdapter = new SerialPortAdapter(invoker => this.Dispatcher.Dispatch(invoker));
            _portAdapter.PortDataReceived += _portAdapter_PortDataReceived;
        }

        // SAFE: We're on the UI thread.
        private void _portAdapter_PortDataReceived(object? sender, PortDataReceivedEventArgs e)
        {
            foreach (var ch in e.Data)
            {
                if (ch == 13)
                    IncomingSerialData.Add("");
                else if(ch==10)
                {

                }
                else
                {
                    IncomingSerialData[IncomingSerialData.Count - 1] += (char)ch;
                }
            }
        }

        private void theButton_Clicked(object sender, EventArgs e)
        {
            string comPortName = theComPort.Text;
            int baudRate;
            bool respectCts = theRespectCts.IsChecked;
            Parity parity;

            if (int.TryParse(theBaudRate.Text, out baudRate) == false)
            {
                theBaudRate.Text = "Bad value";
                return;
            }
            if (int.TryParse(theBaudRate.Text, out baudRate) == false)
            {
                theBaudRate.Text = "Bad value";
                return;
            }
            if (Enum.TryParse(theParity.Text, out parity) == false)
            {
                theParity.Text = "Bad value";
                return;
            }

            if (_portAdapter.IsConnected)
            {
                _portAdapter.Detach();
            }

            IncomingSerialData.Clear();
            if (_portAdapter.TryAttachTo(comPortName, baudRate, parity, respectCts) == false)
            {
                IncomingSerialData.Add("Fail");
            }
            else
            {
                IncomingSerialData.Clear();
                IncomingSerialData.Add("> ");
            }
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (_portAdapter.IsConnected)
            {
                _ = _portAdapter.WriteAsync(Encoding.UTF8.GetBytes(theMessageToSend.Text));

            }
        }
    }
}

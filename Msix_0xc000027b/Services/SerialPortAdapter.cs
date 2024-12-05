using System.Diagnostics;
using System.IO.Ports;

namespace Msix_0xc000027b.Services
{
    public class SerialPortAdapter
    {
        public bool IsConnected => _serialPort?.IsOpen ?? false;

        private SerialPort? _serialPort;

        public event EventHandler<PortDataReceivedEventArgs>? PortDataReceived;
        // TODO: DeviceType has no business here.
        public bool TryAttachTo(string name)
        {
            if (_serialPort != null)
                throw new InvalidOperationException("Attempt MDeviceComms.TryAttachTo when already attached to a port. Call Detach() first.");

            bool retval = false;

            try
            {
                _serialPort = new SerialPort(name);

                // This will throw if the port cannot be opened.
                Debug.WriteLine($"Attempting to connect to: [{name}]");
                _serialPort.ReadBufferSize = 256 * 1024;
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DataReceived += _serialPort_DataReceived;
                retval = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception opening serial port: [{name}]");
                Debug.WriteLine(ex.ToString());
                _serialPort?.Dispose();
                _serialPort = null;
            }
            return retval;
        }

        public bool Detach()
        {
            try
            {
                // TODO: If this can take ages, wrap it in a Task.

                _serialPort.Close();
                _serialPort.DataReceived -= _serialPort_DataReceived;
                _serialPort.Dispose();
                _serialPort = null;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uh oh! {ex.Message}");
                throw;
            }
        }

        public async Task<bool> WriteAsync(byte[] bytes)
        {
            if (_serialPort.IsOpen != true)
                return false;

            try
            {
                await _serialPort.BaseStream.WriteAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        bool _reentrancy = false;
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_reentrancy)
                throw new InvalidOperationException("BUG: Didn't expect that");

            _reentrancy = true;

            // We're on a worker thread.
            // Get the data *before* we marshall to the UI thread, so the caller can't close the port or some other horrible.
            // Do not use BaseStream.ReadAsync in case we introduce re-entrancy.

            // If we get an exception on this Debug.WriteLine, the fix HASN'T worked.
            //Debug.WriteLine("Reading {0} bytes from serial port", _serialPort.BytesToRead);

            switch (e.EventType)
            {
                case SerialData.Chars:
                    try
                    {
                        // ReadByte returns -1 for EOF, otherwise a byte cast to int.

                        // Horrible memory churn here.
                        var data = new byte[_serialPort.BytesToRead];
                        _serialPort.BaseStream.Read(data);

                        // TODO: Inject the UI thread id, or an Action that allows us to run on the UI thread without having to know about the UI thread here.
                        Device.BeginInvokeOnMainThread(() => PortDataReceived?.Invoke(this, new PortDataReceivedEventArgs(data)));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    break;

                case SerialData.Eof:
                    throw new NotImplementedException("_serialPort_DataReceived got EOF from module. What to do here?");
            }

            if (!Device.IsInvokeRequired)
            {
                throw new InvalidOperationException("_serialPort_DataReceived callback called from UI thread!");
            }

            _reentrancy = false;
        }
    }
}

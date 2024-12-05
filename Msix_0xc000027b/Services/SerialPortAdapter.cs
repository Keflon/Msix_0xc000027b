using System.Diagnostics;
using System.IO.Ports;

namespace Msix_0xc000027b.Services
{
    public class SerialPortAdapter
    {
        private bool _respectCts;
        private readonly Action<Action> _invoker;
        private SerialPort _serialPort;
        public event EventHandler<PortDataReceivedEventArgs>? PortDataReceived;

        public SerialPortAdapter(Action<Action> invoker)
        {
            _invoker = invoker;
        }

        public event EventHandler<IsConnectedChangedEventArgs>? SerialPortConnectedChanged;

        private bool _isConnected;

        public bool IsConnected
        {
            get
            {
                if (_isConnected == true)
                {
                    // Detect if the serial port has dropped connection.
                    if ((_serialPort?.IsOpen ?? false) == false)
                        IsConnected = false;
                    else if (_respectCts && _serialPort!.CtsHolding == false)
                        IsConnected = false;
                }

                return _isConnected;
            }
            private set
            {
                if (value != (_serialPort?.IsOpen ?? false))
                    Debug.WriteLine($"SerialPortAdapter IsConnected mismatch. Coding error.");

                if (value != _isConnected)
                {
                    _isConnected = value;
                    IsConnectedChanged();
                }
            }
        }

        private void IsConnectedChanged()
        {
            try
            {
                if (IsConnected == false)
                {
                    if (_serialPort != null)
                    {
                        // If _serialPort is null, this will throw. Let the caller catch it.
                        _serialPort.Close();
                        _serialPort.DataReceived -= _serialPort_DataReceived;
                        _serialPort.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            try
            {
                _invoker(() => SerialPortConnectedChanged?.Invoke(this, new IsConnectedChangedEventArgs(_isConnected)));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool TryAttachTo(string name, int baudRate, Parity parity, bool respectCts)
        {
            _respectCts = respectCts;

            bool retval = false;

            try
            {
                if (_serialPort == null)
                {
                    _serialPort = new SerialPort(name, baudRate, parity);
                    // This will throw if the port cannot be opened.
                    _serialPort.ReadBufferSize = 16 * 1024;
                }
                else
                {
                    _serialPort.PortName = name;
                    _serialPort.BaudRate = baudRate;
                    _serialPort.Parity = parity;
                }

                _serialPort.Open();

                if (_respectCts && _serialPort.CtsHolding == false)
                {
                    _serialPort.Close();
                    //_serialPort.Dispose();
                    //_serialPort = null;
                    retval = false;
                }
                else
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DataReceived += _serialPort_DataReceived;
                    retval = true;
                }
            }
            catch (Exception ex)
            {
            }
            IsConnected = retval;
            return retval;
        }

        public void Detach()
        {
            _serialPort.Close();
            _serialPort.DataReceived -= _serialPort_DataReceived;
            IsConnected = false;
        }

        public async Task<bool> WriteAsync(byte[] bytes)
        {
            if ((_serialPort?.IsOpen ?? false) != true)
            {
                IsConnected = false;
                return false;
            }

            try
            {
                await _serialPort.BaseStream.WriteAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                Detach();
                return false;
            }
        }

        /// <summary>
        /// This is called on a worker thread by the serialPort.
        /// Get out of here as fast as possible because I don't know what
        /// potential horribleness may happen if we block long enough for the next 
        /// call to be made.
        /// Does the serialport queue things up or call this method 
        /// re-entrantly on another thread? Or even just chuck data away?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // We're on a worker thread.
            // Get the data *before* we marshall to the UI thread, so the caller can't close the port or some other horrible.
            // Do not use BaseStream.ReadAsync in case we introduce re-entrancy.

            switch (e.EventType)
            {
                case SerialData.Chars:
                    try
                    {
                        // ReadByte returns -1 for EOF, otherwise a byte cast to int.

                        // Horrible memory churn here. Use a DataLumpFactory/cache if we want to take pressure off the GC.
                        var data = new byte[_serialPort!.BytesToRead];
                        _serialPort.BaseStream.Read(data);
                        _invoker(() => PortDataReceived?.Invoke(this, new PortDataReceivedEventArgs(data)));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    break;

                case SerialData.Eof:
                    throw new NotImplementedException("_serialPort_DataReceived got EOF. What to do here?");
            }
        }


        public void Flush()
        {
            _serialPort.BaseStream.Flush();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;

namespace com.miniteknoloji
{
    /// <summary>
    /// MertBus rs485 asenkron seri iletişim protokolü - Taşıma katmanı
    /// MertBus rs485 asyncronius serial communication protocol - Transport layer
    /// </summary>
    public class MertBus:IDisposable {
        #region |:.Locals.:|
        SerialPort _port;
        byte _id;
        Task _poller;
        Boolean _canPoll;
        private const byte BROADCAST_ID = (Byte)0xFF ;
        #endregion

        #region |:.Public.:|
        /// <summary>
        /// Defines the method signature for DataReceived event
        /// </summary>
        /// <param name="data">Received Data</param>
        /// <param name="from_id">Sender id</param>
        public delegate void DataReceivedDelegate(Byte[] data, Byte from_id);

        public event DataReceivedDelegate OnDataReceived;
        #endregion

        #region |:.Constructors.:|
        /// <summary>
        /// Initializes the communitation object. 
        /// </summary>
        /// <param name="port">Serial Port to use</param>
        /// <param name="node_id">The self id or self address</param>
        /// <remarks>
        /// The port should be open and available. Any baud can be used but over 28800 baud
        /// is recommended! 
        /// </remarks>
        public MertBus( SerialPort port, byte node_id ) {
            if (!port.IsOpen)
                throw new InvalidOperationException( "Serial port should be open!" );

            _port = port;
            _id = node_id;
            _canPoll = true;

            //check serial port to receive data
            _poller = Task.Factory.StartNew( new Action( () => {
                #region |:.Poller thread.:|
                while (_canPoll) {
                    if (_port.BytesToRead > 5) {
                        Frame f = null;
                        try {
                            f = Frame.CreateFromReceivedData( _port );
                        } catch (CrcNotMatchException ex) {
                            //invalid crc
                            System.Diagnostics.Debug.Print( "Serial port frame reader: Invalid CRC" );
                            continue;
                        } catch (TimeoutException ex) {
                            //read timeout
                            System.Diagnostics.Debug.Print( "Serial port frame reader: Read Operation Timeout" );
                            continue;
                        } catch (Exception ex) {
                            //if some error happens   
                            System.Diagnostics.Debug.Print( "Serial port frame reader: error: " + ex.Message);
                            continue;
                        }

                        if (System.Diagnostics.Debugger.IsAttached) {
                            System.Diagnostics.Debug.Write( "(MertBus) Received Data: " );
                            for (int i = 0; i < f.Payload.Length; i++) {
                                System.Diagnostics.Debug.Write( f.Payload[i].ToString( "X2" ) );
                                System.Diagnostics.Debug.Write( " " );
                            }
                            System.Diagnostics.Debug.WriteLine(" ");
                        }
                        

                        if (this.OnDataReceived != null)
                            try {
                                if (f.ReceiverId == _id || f.ReceiverId == BROADCAST_ID) //receive just only data for me and broadcasted
                                    this.OnDataReceived( f.Payload, f.SenderId );
                            } catch (Exception) {
                                //do nothing                                    
                            }

                    } else
                        System.Threading.Thread.Sleep( 12 );
                }                
                #endregion
            } ) );
        }
        #endregion

        #region |:.Methods.:|
        /// <summary>
        /// Sends data to specific node
        /// </summary>
        /// <param name="to_node">node id to send data</param>
        /// <param name="data">it is obvious aint it?</param>
        public void SendData( byte to_node, Byte[] data ) {
            //Prepare frame
            Frame f = new Frame();
            f.SenderId = _id;
            f.ReceiverId = to_node;
            f.Payload = data;

            _port.Write( f.Buffer, 0, f.Size );            
        }

        /// <summary>
        /// Broadcasts data to all nodes.
        /// </summary>
        /// <param name="data">Data</param>
        public void BroadcastData( Byte[] data ) {
            this.SendData( BROADCAST_ID, data );
        }
        #endregion

        #region |:.Inner/Private Classes.:|
        private class Frame {
            public Byte SenderId { get; set; } 
            public Byte ReceiverId { get; set; }
            public Byte Size { get; private set; }

            private byte _crc;
            private Byte[] _payload;
            private const byte CRC_SEED = 0xE6;

            public byte Crc { get { return _crc; } }

            public Byte[] Payload {
                get { return _payload; }
                set {
                    if (value.Length > 32)
                        throw new ArgumentOutOfRangeException( "Payload (data) cannot be longer than 32 bytes" );

                    if(value.Length == 0)
                        throw new ArgumentOutOfRangeException( "Payload (data) cannot be empty" );

                    _payload = value;
                    _crc = CRC_SEED;
                    for (int i = 0; i < _payload.Length; i++) {
                        _crc ^= _payload[i];
                    }
                    this.Size = Convert.ToByte( _payload.Length + 5 );
                }
            }

            public Byte[] Buffer {
                get {
                    byte size = Convert.ToByte(5 + _payload.Length);
                    Byte[]  _buffer = new Byte[size];
                    _buffer[0] = 0x00;
                    _buffer[1] = this.ReceiverId;
                    _buffer[2] = this.SenderId; //me
                    _buffer[3] = Convert.ToByte( _payload.Length );
                    Array.ConstrainedCopy( _payload, 0, _buffer, 4, _payload.Length ); //insert payload
                    _buffer[size - 1] = _crc;
                    return _buffer;
                }
            }

            public static Frame CreateFromReceivedData( SerialPort _p ) {
                byte start = Convert.ToByte(_p.ReadByte()); //skip silence
                Frame _f = new Frame();
                _f.ReceiverId = Convert.ToByte( _p.ReadByte() );
                _f.SenderId = Convert.ToByte( _p.ReadByte() );
                Byte _payloadSize = Convert.ToByte( _p.ReadByte() );

                int tmout = 0;
                while (_p.BytesToRead<_payloadSize) {
                    if (tmout > 50)
                        throw new TimeoutException( "Read operation timed out" );
                    System.Threading.Thread.Sleep( 2 );
                    tmout++;
                }

                Byte[] _data = new Byte[_payloadSize];
                _p.Read( _data, 0, _payloadSize );
                Byte _crc_incoming = Convert.ToByte( _p.ReadByte() );

                _f.Payload = _data;
                //String _dataAsText = Encoding.ASCII.GetString( _data );
                //System.Diagnostics.Debug.Print( "Received Data As Text: " + _dataAsText );

                if (_f.Crc != _crc_incoming)
                    throw new CrcNotMatchException();

                return _f;
            }
        }
        #endregion

        #region IDisposable Members

        public void Dispose() {
            _canPoll = false;
            _poller.Wait(); //end poller task
        }

        #endregion
    }
}

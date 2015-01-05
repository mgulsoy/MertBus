using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using com.miniteknoloji;

namespace MertBusTestApp {
    public partial class Form1 : Form {

        MertBus mb;
        

        public Form1() {
            InitializeComponent();
        }

        private void button1_Click( object sender, EventArgs e ) {
            //open connection
            serialPort1.Open();

            mb = new MertBus( serialPort1, 12 );
            mb.OnDataReceived += mb_OnDataReceived;
            textBox1.AppendText( "Port opened" + Environment.NewLine );
        }

        void mb_OnDataReceived( byte[] data, byte from_id ) {

            StringBuilder dt = new StringBuilder();
            foreach (byte b in data) {
                dt.AppendFormat( "{0}|{1}|{2}  ", b, Convert.ToString( b, 16 ), Encoding.ASCII.GetString( new byte[] { b } ) );
            }           
            
            this.Invoke( (MethodInvoker)delegate() {
                textBox1.AppendText( "Data received from: " + from_id + Environment.NewLine );
                textBox1.AppendText( dt.ToString() + Environment.NewLine ); 
            } ); 

        }

        private void button2_Click( object sender, EventArgs e ) {
            mb.SendData( 1, new byte[] { (byte)'A', (byte)'X' } );
            textBox1.AppendText( "Sent data: AX" + Environment.NewLine );
            
        }

        private void button3_Click( object sender, EventArgs e ) {
            byte[] bx = new byte[] { 0x40, 0x42, 0x0F, 0x00 };

            uint val = BitConverter.ToUInt32( bx, 0 );

            Debug.Print( val.ToString() );
        }
    }
}

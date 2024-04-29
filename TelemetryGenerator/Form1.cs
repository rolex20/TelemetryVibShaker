using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace TelemetryGenerator
{
    public partial class Form1 : Form
    {
        private Thread? threadTelemetry; // this thread runs the telemetry
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken token;
        private UdpClient? udpSender;
        private byte[] datagram = new byte[7];

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnCancel.Enabled = true;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            ConnectUDP();

            // Start a new thread to act as the UDP Server
            threadTelemetry = new Thread(DoTelemetry);
            threadTelemetry.Start();
        }

        // This will be executed in a new thread
        private void DoTelemetry()
        {
            Stopwatch stopwatch = new Stopwatch();
            long lastSecond = 0;

            int iteration = 1;
            progressBar1.BeginInvoke(new Action(() => { progressBar1.Value = 0; }));
            while (iteration++ <= nudDatagrams.Value && !token.IsCancellationRequested)
            {
                datagram[0] = 1;  // this flag means that this datagram contains telemetry instead of aircraft name
                datagram[1] = (byte)nudAoA.Value;
                datagram[2] = (byte)nudSpeedBrake.Value;
                datagram[3] = (byte)nudFlaps.Value;

                /* Speed Conversion 
                 * First convert knots to meters / second by dividing the speed value by 1.94384449
                 * Then divide by ten to convert to decameters/second to ensure the value fits in 1 byte
                 */
                datagram[4] = (byte)nudSpeed.Value;

                // G-Forces
                datagram[5] = (byte)nudGForces.Value;

                // Altitude
                datagram[6] = (byte)nudAltitude.Value;

                // Send  Telemetry
                udpSender.Send(datagram, datagram.Length);

                // Busy Wait
                stopwatch.Restart();
                while (stopwatch.ElapsedMilliseconds < nudWaittime.Value)
                {
                    if (token.IsCancellationRequested) break;
                    long currentSecond = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                    Debug.Print($"Current Second={currentSecond},  Last-Second={lastSecond}");
                    if (lastSecond != currentSecond)
                    {
                        lastSecond = currentSecond;
                        progressBar1.BeginInvoke(new Action(() => { progressBar1.Value = 100 * iteration / (int)nudDatagrams.Value; }));
                    }

                }
            }
            progressBar1.BeginInvoke(new Action(() => { progressBar1.Value = 100; }));

            DisconnectUDP();
            btnCancel_Click(null, null);

        }

        private void DisconnectUDP()
        {
            if (udpSender != null)
            {
                udpSender.Dispose();
                udpSender = null;
            }

        }


        private void ConnectUDP()
        {
            udpSender = new UdpClient();
            udpSender.Connect(txtIP.Text, Convert.ToInt32(txtPort.Text));
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancel.Enabled = false;
            btnStart.Enabled = true;
            // Signal the CancellationToken to cancel the operation
            if (sender is not null) cancellationTokenSource.Cancel();
        }

        private void btnSendUnitName_Click(object sender, EventArgs e)
        {
            if (udpSender is null) ConnectUDP();

            byte[] sendBytes = Encoding.ASCII.GetBytes(txtAircraftName.Text);
            udpSender.Send(sendBytes, sendBytes.Length);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
        }
    }
}

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;





namespace FalconExporter
{
    public partial class frmMain : Form
    {
        private Reader _sharedMemReader = new Reader();
        private FlightData _lastFlightData;

        private Process currentProcess;
        private UdpClient udpSender;
        private byte[] datagram;
        private Stopwatch stopWatch = new Stopwatch();
        private int maxProcessingTime;
        private ulong timeStamp;
        private float lastTelemetry;


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();


        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();


        public frmMain()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Allocate the udp datagram
            datagram = new byte[7];
            datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

            ConnectUDP();

            // Switch to Monitor
            if (chkChangeToMonitor.Checked) tabControl1.SelectedIndex = 1;

            btnDisconnect.Enabled = true;
            btnStart.Enabled = false;
            timer1.Interval = (int)nudFrequency.Value;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (chkShowStatistics.Checked)
            {
                stopWatch.Restart();                
            }

            if (ReadSharedMem() != null)
            {
                ProcessTelemetry();
            }
            else
            {
                UpdateCaption(tsStatus, "Not Connected.");
            }

            if (chkShowStatistics.Checked)
            {
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                if (maxProcessingTime < elapsed) maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
            }
        }

        private FlightData ReadSharedMem()
        {
            return _lastFlightData = _sharedMemReader.GetCurrentData();
        }

        private float Abs(float value) {
            return value >= 0.0f ? value : -1.0f * value;
        }

        private void ProcessTelemetry()
        {
            float totalFuel = _lastFlightData.internalFuel + _lastFlightData.externalFuel;
            float aia = Abs(_lastFlightData.aauz);


            // Using totalFuel as kind of a flag when we are in a new plane
            // If totalFuel decreases, we are in the same aircraft fying in it or something
            // If totalFuel increases, probably we are in a new plane, new mission, since I don't do refueling
            if (totalFuel < lastTelemetry)
            {
                lastTelemetry = totalFuel; 
                float speedBrakesPct = _lastFlightData.speedBrake * 100.0f;

                // datagram[0] = 1;  // already done this with btnStart_Click()
                datagram[1] = _lastFlightData.alpha >= 0.0f ? (byte)_lastFlightData.alpha : (byte)0; // AoA
                datagram[2] = (byte)speedBrakesPct; // Speed Brakes
                datagram[3] = 0; // Couldn't find flaps in falcon shared memory telemetry

                // Speed Conversion for true air speed
                // vt is in feet per second
                // We need decameters per second:
                // decameters per second = 0.3048 * _lastFlightData.vt / 10;
                datagram[4] = (byte)(0.0304799999536704f * _lastFlightData.vt);

                // G-Forces
                datagram[5] = _lastFlightData.gs >= 0.0f ? (byte)_lastFlightData.gs : (byte)0;

                // Altitud Conversion, same as Speed above, we ned decameters
                // maximum will be 2550 meters, which is enough to see if we are flying
                // (it would if I could find an above the ground reliable, always available telemetry (RALT is not always available)
                float alt = 0.0304799999536704f * aia;
                datagram[6] = alt <= 255.0f ? (byte)alt : (byte)255;

                // Send Telemetry
                timeStamp = GetTickCount64();
                udpSender.Send(datagram, datagram.Length);
            } else if ( totalFuel > lastTelemetry)
            {
                lastTelemetry = totalFuel;
                byte[] sendBytes = Encoding.ASCII.GetBytes(txtAircraftName.Text);
                timeStamp = GetTickCount64();
                udpSender.Send(sendBytes, sendBytes.Length);
                tsAircraftChange.Tag = true;
            }

            // Update UI
            if (tabControl1.SelectedIndex == 1 && chkShowStatistics.Checked)
            {
                this.SuspendLayout();
                UpdateCaption(tsStatus, "Connected.");
                UpdateCaption(lblAoA, _lastFlightData.alpha);
                UpdateCaption(lblSpeed, _lastFlightData.kias);
                UpdateCaption(lblAltitude, aia);
                UpdateCaption(lblSpeedBrakes, _lastFlightData.speedBrake * 100.0f);
                UpdateCaption(lblTrueAirspeed, _lastFlightData.vt);
                UpdateCaption(lblGForces, _lastFlightData.gs);
                UpdateCaption(lblVehicleType, _lastFlightData.vehicleACD);
                UpdateCaption(lblFuel, totalFuel);
                UpdateCaption(lblTimeStamp, timeStamp);
                UpdateCaption(lblMaxProcessingTime, maxProcessingTime);
                if ((bool)tsAircraftChange.Tag)
                {
                    UpdateCaption(tsAircraftChange, timeStamp);
                    tsAircraftChange.Tag = false;
                }
                this.ResumeLayout();
            }
        }



        private void frmMain_Load(object sender, EventArgs e)
        {
            tsAircraftChange.Tag = false; // flag to update the timestamp of the last aircraft change

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);
            ProcessorCheck();
        }

        private void ProcessorCheck()
        {
            // LoadSettings must be called before ProcessorCheck()

            // Get the current process
            currentProcess = Process.GetCurrentProcess();

            // Assign only Efficiency cores if requested and CPU==12700K
            chkUseEfficiencyCoresOnly_CheckedChanged(null, null);


            // Change the priority class to the previous setting selected (NORMAL, BELOW_NORMAL or IDLE)
            //cmbPriorityClass.SelectedIndex = Properties.Settings.Default.PriorityClassSelectedIndex;
            cmbPriorityClass_SelectedIndexChanged(null, null);

        }


        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.XCoordinate = this.Location.X;
            Properties.Settings.Default.YCoordinate = this.Location.Y;

            // Save the settings for all controls in the form
            SaveSettings(this);

            // SaveSettings() is recursive so calling Save() below
            Properties.Settings.Default.Save();
        }

        // This method loads the setting Value for a given control
        private void LoadSetting(Control control)
        {

            // Get the name of the control
            string controlName = control.Name;

            // If the control name is not null or empty
            if (string.IsNullOrEmpty(controlName)) return;


            // Get the control's property Value according to its type
            MyControlInfo controlInfo = new MyControlInfo(control);
            if (controlInfo.Value != null) // Load Value only if I am interested in this control
            {
                // If the next line fails, remember to add that control to the Properties.Settings:
                // Right click on the Project, select Properties, click on General and then click on
                // the link "Create or open application settings"
                object savedSettingValue = Properties.Settings.Default[controlName];
                controlInfo.AssignValue(savedSettingValue);// Get the setting Value from the Properties.Settings.Default
            }




        }


        // This method saves the setting Value for a given control
        private void SaveSetting(Control control)
        {
            // Get the name of the control
            string controlName = control.Name;

            if (string.IsNullOrEmpty(controlName)) return;


            MyControlInfo controlInfo = new MyControlInfo(control);

            // Get the control's property Value according to its type
            //object Value = RestorableSetting(control);

            // Save setting only if I am interested in this control
            if (controlInfo.Value != null)
            {
                // If the next line fails, remember to add that control to the Properties.Settings:
                // Right click on the Project, select Properties, click on General and then click on
                // the link "Create or open application settings"
                Properties.Settings.Default[controlName] = controlInfo.Value;   // Set the setting Value in the Properties.Settings.Default
            }
        }

        // This method loads the settings for all controls in a container
        private void LoadSettings(Control container)
        {
            // Loop through all the controls in the container
            foreach (Control control in container.Controls)
            {
                // Load the setting for the current control
                LoadSetting(control);
                // If the control is a container itself, load the settings for its children recursively
                if (control.HasChildren)
                {
                    LoadSettings(control);
                }
            }
        }

        // This method saves the settings for all controls in a container
        private void SaveSettings(Control container)
        {
            // Loop through all the controls in the container
            foreach (Control control in container.Controls)
            {
                // Save the setting for the current control
                SaveSetting(control);
                // If the control is a container itself, save the settings for its children recursively
                if (control.HasChildren)
                {
                    SaveSettings(control);
                }
            }
        }

        private void chkUseEfficiencyCoresOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (currentProcess == null) return;


            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
            {
                chkUseEfficiencyCoresOnly.Visible = true;
                if (chkUseEfficiencyCoresOnly.Checked)
                {

                    // Define the CPU affinity mask for CPUs 17 to 20
                    // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                    IntPtr affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                    // Set the CPU affinity
                    currentProcess.ProcessorAffinity = affinityMask;
                }
            }

            regKey.Close();
        }

        private void cmbPriorityClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentProcess != null && cmbPriorityClass.Tag != null && (bool)cmbPriorityClass.Tag) // Ignore first change during InitializeComponent()
            {
                switch (cmbPriorityClass.SelectedIndex)
                {
                    case 0: //NORMAL
                        currentProcess.PriorityClass = ProcessPriorityClass.Normal;
                        break;
                    case 1: // BELOW NORMAL
                        currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                        break;
                    case 2: //IDLE
                        currentProcess.PriorityClass = ProcessPriorityClass.Idle;
                        break;
                }
            }

            cmbPriorityClass.Tag = true;
        }

        private void UpdateCaption(Label L, float value)
        {
            float twodecimals = value * 100.0f;
            if (Convert.ToInt32(L.Tag) != twodecimals)
            {
                L.Tag = twodecimals;
                L.Text = (value >= 0.0f) ? $"{value:F1}" : "---"; // if value<0 then it is not valid or applicable yet
            }
        }


        private void UpdateCaption(Label L, int value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateCaption(ToolStripLabel L, ulong value)
        {
            if (Convert.ToUInt64(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }


        private void UpdateCaption(Label L, ulong value)
        {
            if (Convert.ToUInt64(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateCaption(Label L, string value)
        {
            if (!L.Tag.Equals(value))
            {
                L.Tag = value;
                L.Text = value;
            }
        }

        private void UpdateCaption(ToolStripLabel L, string value)
        {
            if (!L.Tag.Equals(value))
            {
                L.Tag = value;
                L.Text = value;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            btnDisconnect.Enabled = false;
            btnStart.Enabled = true;
            tsStatus.Text = "Timer stopped.";
            DisconnectUDP();
        }

        private void chkShowStatistics_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Visible = chkShowStatistics.Checked;
        }

        private void ConnectUDP()
        {
            udpSender = new UdpClient();
            udpSender.Connect(txtDestinationHostname.Text, Convert.ToInt32(txtDestinationPort.Text));
        }


        private void DisconnectUDP()
        {
            if (udpSender != null)
            {
                udpSender.Dispose();
                udpSender = null;
            }
        }

        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }

        private void btnResetMax_Click(object sender, EventArgs e)
        {
            maxProcessingTime = 0;
        }

        private void nudFrequency_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)nudFrequency.Value;
        }
    }
}

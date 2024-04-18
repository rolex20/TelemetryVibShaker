using System.Net.Sockets;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using TelemetryVibShaker;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;


namespace SimConnectExporter
{
    public partial class frmMain : Form
    {
        private Process currentProcess;
        private UdpClient udpSender;
        private byte[] datagram;

        private SimConnect simconnect = null;
        private const int WM_USER_SIMCONNECT = 0x0402;

        private string CurrentAircraftName;
        private Stopwatch stopWatch;
        int maxProcessingTime;

        enum DEFINITIONS
        {
            Struct1,
        }

        enum REQUESTS
        {
            Request1,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title; // Aircraft aircraftName
            public double trueAirspeed; // True airspeed in knots
            public double flaps; // Flaps position
            public double spoilers; // Airbrakes / Spoilers position
            public double angleOfAttack; // Angle of Attack in degrees
        };

        private const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
        private const uint PROCESS_MODE_BACKGROUND_END = 0x00200000;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();


        private void ConnectToSimConnect()
        {

            try
            {
                tsStatusBar1.Text = "Trying to connect...";

                simconnect = new SimConnect("MSFS2020 SimConnectExporter C#", this.Handle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(Simconnect_OnRecvSimobjectData);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                //simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "meter per second", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Trailing Edge Flaps Left Percent", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Spoilers Handle Position", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "INCIDENCE ALPHA", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);
                simconnect.RequestDataOnSimObject(REQUESTS.Request1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                // Automatically change to the motor if required by the user
                if (chkChangeToMonitor.Checked) tabs.SelectedIndex = 1;

            }
            catch (COMException ex)
            {
                tsStatusBar1.Text = ex.Message;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (simconnect != null)
                {
                    simconnect.ReceiveMessage();
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            tsStatusBar1.Text = "Connected to MSFS SimConnect since " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            tsStatusBar1.Text = "SimConnect connection closed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); ;
            DisconnectFromSimConnect();
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            //if (chkShowStatistics.Checked) 
                stopWatch.Restart();

            if (data.dwRequestID == (uint)REQUESTS.Request1)
            {
                Struct1 sd = (Struct1)data.dwData[0];

                /*** Send Datagram ***/
                string aircraftName = sd.title;
                if (CurrentAircraftName.Equals(aircraftName))// send datagram
                {

                    datagram[0] = (byte)Math.Round(57.2957795f * (float)sd.angleOfAttack); // convert radians to degrees
                    datagram[1] = (byte)sd.spoilers;
                    datagram[2] = (byte)sd.flaps;

                    Debug.Print($"AOA={sd.angleOfAttack * 57.2957795f} in bytes={datagram[0]}");
                    /* Speed Conversion 
                     * First convert knots to meters / second by dividing the speed value by 1.94384449
                     * Then divide by ten to convert to decameters/second to ensure the value fits in 1 byte
                     */
                    datagram[3] = (byte)(((float)sd.trueAirspeed / 1.94384449f) / 10.0f);
                    udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
                }
                else // send aircraft name
                {
                    CurrentAircraftName = aircraftName;
                    byte[] sendBytes = Encoding.ASCII.GetBytes(CurrentAircraftName);
                    udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
                }


                /*** Update UI ***/
                if (chkShowStatistics.Checked && tabs.SelectedIndex == 1)
                {
                    UpdateValue(lblLastProcessorUsedUDP, (int)GetCurrentProcessorNumber());
                    UpdateValue(lblTimestamp, Environment.TickCount64);
                    UpdateValue(lblCurrentUnitType, sd.title);
                    UpdateValue(lblSpeed, (int)sd.trueAirspeed);// show in knots
                    UpdateValue(lblLastFlaps, (int)sd.flaps);
                    UpdateValue(lblLastSpeedBrakes, (int)sd.spoilers);
                    UpdateValue(lblLastAoA, (int)datagram[0]);
                    
                }
                UpdateValue(lblProcessingTimeUDP, maxProcessingTime);

            }
            //if (chkShowStatistics.Checked) { 
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                if (maxProcessingTime < elapsed) maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
            //}
        }

        private void UpdateValue(Label L, int value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateValue(Label L, long value)
        {
            if (Convert.ToInt64(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateValue(Label L, string value)
        {
            if (!L.Tag.Equals(value))
            {
                L.Tag = value;
                L.Text = value;
            }
        }




        private void DisconnectFromSimConnect()
        {
            if (simconnect != null)
            {
                simconnect.Dispose();
                simconnect = null;
            }
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }



        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectUDP();
            DisconnectFromSimConnect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectUDP();
            DisconnectFromSimConnect();
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
            udpSender.Connect(txtDestinationHostname.Text, Convert.ToInt32(txtDestinationPort.Text));
        }


        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            CurrentAircraftName = "";
            ConnectUDP();
            ConnectToSimConnect();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Allocate the udp datagram
            datagram = new byte[4];

            // Create the stopwatch
            stopWatch = new Stopwatch();

            // Load the settings for all controls in the form
            LoadSettings(this);


            ProcessorCheck();

            tsStatusBar1.Text = "Idle";
        }

        private void ProcessorCheck()
        {
            // LoadSettings must be called before ProcessorCheck()

            // Get the current process
            currentProcess = Process.GetCurrentProcess();

            // Assign only Efficiency cores if requested and CPU==12700K
            chkUseEfficiencyCoresOnly_CheckedChanged(null, null);


            // Assign background mode if requested
            if (chkUseBackgroundProcessing.Checked)
                SetPriorityClass(currentProcess.Handle, PROCESS_MODE_BACKGROUND_BEGIN);


            // Change the priority class to the previous setting selected (NORMAL, BELOW_NORMAL or IDLE)
            //cmbPriorityClass.SelectedIndex = Properties.Settings.Default.PriorityClassSelectedIndex;
            cmbPriorityClass_SelectedIndexChanged(null, null);

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

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.XCoordinate = this.Location.X;
            Properties.Settings.Default.YCoordinate = this.Location.Y;

            // Save the settings for all controls in the form
            SaveSettings(this);

            // SaveSettings() is recursive so calling Save() below
            Properties.Settings.Default.Save();

        }

        private void chkUseBackgroundProcessing_CheckedChanged(object sender, EventArgs e)
        {
            if (currentProcess == null) return;


            if (chkUseBackgroundProcessing.Checked)
                SetPriorityClass(currentProcess.Handle, PROCESS_MODE_BACKGROUND_BEGIN);
            else
                SetPriorityClass(currentProcess.Handle, PROCESS_MODE_BACKGROUND_END);

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
    }
}

using System.Net.Sockets;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using TelemetryVibShaker;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;


namespace SimConnectExporter
{
    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }

    public partial class frmMain : Form
    {
        private Process? currentProcess;
        private UdpClient? udpSender;
        private byte[]? datagram;

        private SimConnect? simconnect = null;
        private const int WM_USER_SIMCONNECT = 0x0402;

        private string? CurrentAircraftName;
        private float maxGForce; // Max G force recorded for the CurrentAircraftName
        private Stopwatch? stopWatch;
        private int maxProcessingTime;
        private long lastTimeStamp_ms;

        private Mutex SingleInstanceMutex;


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
            public float trueAirspeed; // True airspeed in knots
            public float flaps; // Flaps position in percentage
            public float spoilers; // Airbrakes / Spoilers position in percentage
            public float angleOfAttack; // Angle of Attack in degrees
            public float gear;  // gear animation opsition in percentage
            public float gForce;
            public float altitude; // Altitude above ground level in meters
        };

        private const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
        private const uint PROCESS_MODE_BACKGROUND_END = 0x00200000;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();


        private int GetSimConnectPeriod()
        {
            switch (cmbSimConnectPeriod.Text)
            {
                case "SIMCONNECT_PERIOD_VISUAL_FRAME":
                    return SIMCONNECT_PERIOD.VISUAL_FRAME;
                case "SIMCONNECT_PERIOD_SIM_FRAME":
                    return SIMCONNECT_PERIOD.SIM_FRAME;
                case "SIMCONNECT_PERIOD_SECOND":
                    return SIMCONNECT_PERIOD_SECOND;
                default:
                    return SIMCONNECT_PERIOD.VISUAL_FRAME; // just to remove compiler warning
            }
        }

        private bool Connect()
        {
            tsStatusBar1.Text = "Trying to connect...";
            try
            {
                simconnect = new SimConnect("MSFS2020 SimConnectExporter C#", this.Handle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(Simconnect_OnRecvSimobjectData);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "knots", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);
                //simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "meter per second", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Trailing Edge Flaps Left Percent", "percent", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Spoilers Handle Position", "percent", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "INCIDENCE ALPHA", "Radians", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR CENTER POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED); //GEAR ANIMATION POSITION
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "G FORCE", "Gforce", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALT ABOVE GROUND", "meters", SIMCONNECT_DATATYPE.FLOAT32, 0, SimConnect.SIMCONNECT_UNUSED);


                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                // Prepare udp Sender
                ConnectUDP();

                // Request data from SimConnect
                int period = GetSimConnectPeriod();
                //simconnect.RequestDataOnSimObject(REQUESTS.Request1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                simconnect.RequestDataOnSimObject(REQUESTS.Request1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, period, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);


                btnConnect.Enabled = false;


                return true;

            }
            catch //(COMException ex)
            {
                //tsStatusBar1.Text = ex.Message;
                tsStatusBar1.Tag = Convert.ToInt32(tsStatusBar1.Tag) + 1;
                tsStatusBar1.Text = $"Attempted to SimConnect failed {Convert.ToInt32(tsStatusBar1.Tag)}, retrying... ";

            }

            return false;
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

            // Notify that a Disconnect has been generated by MSFS, not by the user clicking the btnDisconnect button
            btnDisconnect_Click(null, null);
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            // perform division in double to retain maximum precision
            // result is in milliseconds
            long currentTimeStamp_ms = (long)GetTickCount64();

            if (currentTimeStamp_ms - lastTimeStamp_ms < nudFrequency.Value)
                return; // Skip this telemetry

            if (chkShowStatistics.Checked)
                stopWatch.Restart();

            // update the new timestamp
            lastTimeStamp_ms = currentTimeStamp_ms;

            if (data.dwRequestID == (uint)REQUESTS.Request1)
            {
                Struct1 sd = (Struct1)data.dwData[0];

                /*** Send Datagram ***/
                string aircraftName = sd.title;
                if (CurrentAircraftName.Equals(aircraftName)) // same aircraft as before, just send the datagram with telemetry
                {
                    float aoa = 57.2957795f * sd.angleOfAttack; // convert radians to degrees
                    datagram[0] = 1;  // this flag means that this datagram contains telemetry instead of aircraft name
                    datagram[1] = aoa < 0.0f ? (byte)0 : (byte)aoa;
                    datagram[2] = (byte)sd.spoilers;
                    datagram[3] = (byte)sd.flaps;

                    /* Speed Conversion 
                     * First convert knots to meters / second by dividing the speed value by 1.94384449
                     * Then divide by ten to convert to decameters/second to ensure the value fits in 1 byte
                     */
                    datagram[4] = (byte)(((float)sd.trueAirspeed / 1.94384449f) / 10.0f); // No need for more resolution in here

                    // G-Forces
                    datagram[5] = sd.gForce < 0.0f ? (byte)0 : (byte)sd.gForce;

                    // Altitude
                    int dm = (int)sd.altitude / 10; // convert meters to Decameters (fit in 1 byte) 
                    datagram[6] = dm <= 255 ? (byte)dm : (byte)255; // max is 2550m, just good enough to make sure we are in the air

                    // Gear
                    datagram[7] = (byte)sd.gear;

                    // Send  Telemetry
                    udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);


                    /* Check maxG and Track */
                    if (sd.gForce > maxGForce)
                    {
                        maxGForce = sd.gForce;

                        if (chkTrackGForces.Checked)
                        {
                            // Create a new row object based on the row template
                            DataGridViewRow newRow = (DataGridViewRow)dgGForces.Rows[0].Clone();


                            // Set the values for each cell in the row
                            newRow.Cells[0].Value = aircraftName;
                            newRow.Cells[1].Value = $"{maxGForce:F1}";
                            newRow.Cells[2].Value = $"{lastTimeStamp_ms}";

                            // Add the new row to the DataGridView
                            dgGForces.Rows.Add(newRow);
                        }
                    }
                }
                else // send aircraft name
                {
                    CurrentAircraftName = aircraftName;
                    byte[] sendBytes = Encoding.ASCII.GetBytes(CurrentAircraftName);
                    udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
                    maxGForce = 0.0f; // resetting max g force detected for this aircraft
                }


                /*** Update UI ***/
                if (chkShowStatistics.Checked && tabs.SelectedIndex == 1)
                {
                    this.SuspendLayout();
                    UpdateValue(lblLastProcessorUsedUDP, (int)GetCurrentProcessorNumber());
                    UpdateValue(lblTimestamp, lastTimeStamp_ms);
                    UpdateValue(lblCurrentUnitType, sd.title);
                    UpdateValue(lblSpeed, (int)sd.trueAirspeed);// show in knots
                    UpdateValue(lblLastFlaps, (int)sd.flaps);
                    UpdateValue(lblLastSpeedBrakes, (int)sd.spoilers);
                    UpdateValue(lblLastAoA, (int)datagram[1]);
                    UpdateValue(lblGear, (int)sd.gear);
                    UpdateValue(lblAltitude, (int)sd.altitude);
                    UpdateValue(lblGforce, (int)sd.gForce);
                    UpdateValue(lblMaxGForce, maxGForce);
                    UpdateValue(lblProcessingTimeUDP, maxProcessingTime);
                    this.ResumeLayout();
                }
                

            }
            if (chkShowStatistics.Checked)
            {
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                if (maxProcessingTime < elapsed) maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
            }
        }


        private void UpdateValue(Label L, double value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? $"{value:F1}" : "---"; // if value<0 then it is not valid or applicable yet
            }
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


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectFromSimConnect();
            DisconnectUDP();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            tsStatusBar1.Text = "SimConnect disconnection requested by user at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); ;
            DisconnectFromSimConnect();
            DisconnectUDP();

            // Notify that a disconnection was generated by MSFS, not by the user, and attempt to reconnect again when possible
            // This can happen if the user, quit MSFS to make some changes and then they start MSFS
            // Without this code, the user will have to go back to SimConnectExporter and click on Connect again
            if (sender is null)
                btnConnect_Click(null, null);
            else
            {
                if (timer1.Enabled) timer1.Enabled = false;
                btnConnect.Enabled = true;
                cmbSimConnectPeriod.Enabled = true;
            }
        }

        private void DisconnectUDP()
        {
            if (udpSender != null)
            {
                udpSender.Dispose();
                udpSender = null;
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
            // Automatically change to the motor if required by the user
            if (chkChangeToMonitor.Checked) tabs.SelectedIndex = 1;

            tsStatusBar1.Tag = 0;
            //CurrentAircraftName = "";
            if (sender is not null) tsStatusBar1.Text = "Connection attempt requested...";
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            cmbSimConnectPeriod.Enabled = false;
            timer1.Enabled = true;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        private void SingleInstanceChecker()
        {
            SingleInstanceMutex = new Mutex(true, this.Text + "_mutex", out bool createdNew);
            if (!createdNew)
            {
                BringExistingInstanceToFront();
                ShowNotification("Another instance of this application is already running.");
                Application.Exit();
            }
        }

        private void BringExistingInstanceToFront()
        {
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (IsIconic(handle))
                    {
                        ShowWindowAsync(handle, SW_RESTORE);
                    }
                    SetForegroundWindow(handle);
                    break;
                }
            }
        }

        private void ShowNotification(string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = "Application Notification",
                BalloonTipText = message
            };

            notifyIcon.ShowBalloonTip(10000);
        }



        private void frmMain_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);


            CurrentAircraftName = "";

            // Allocate the udp datagram
            datagram = new byte[8];

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

            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
            string processorName = regKey.GetValue("ProcessorNameString").ToString();
            currentProcess = Process.GetCurrentProcess();

            // We need to know the number of processors available to determine if Hyperthreading and Efficient cores are enabled
            int cpuCount = 0;
            regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (regKey != null)
            {
                // The number of subkeys corresponds to the number of CPUs
                cpuCount = regKey.SubKeyCount;
            }
            regKey.Close();


            CpuType cpuType;
            if (processorName.Contains("12700K")) cpuType = CpuType.Intel_12700K;
            else if (processorName.Contains("14700K")) cpuType = CpuType.Intel_14700K;
            else cpuType = CpuType.Other;

            // Calculate affinity for efficient cores only
            IntPtr affinityMask = IntPtr.Zero;
            switch (cpuType)
            {
                case CpuType.Intel_12700K:
                    if (cpuCount == 20)
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);
                        chkUseEfficiencyCoresOnly.Text = "12700K" + chkUseEfficiencyCoresOnly.Text;
                        chkUseEfficiencyCoresOnly.Visible = true;
                    }
                    break;
                case CpuType.Intel_14700K:
                    if (cpuCount == 28)
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | 1 << 23 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27);
                        chkUseEfficiencyCoresOnly.Text = "14700K" + chkUseEfficiencyCoresOnly.Text;
                        chkUseEfficiencyCoresOnly.Visible = true;
                    }
                    break;
                default:
                    //ignore
                    break;
            }

            if (chkUseEfficiencyCoresOnly.Checked && (affinityMask != IntPtr.Zero))
            {
                try
                {
                    // Set the CPU affinity to Efficient Cores only
                    currentProcess.ProcessorAffinity = affinityMask;
                }
                catch { } // Ignore
            }
        }


        private void cmbPriorityClass_SelectedIndexChanged(object? sender, EventArgs? e)
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

        private void btnResetMax_Click(object sender, EventArgs e)
        {
            maxProcessingTime = 0;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Connect())
            {
                timer1.Enabled = false;
            }
        }

        private void dgGForces_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0) // Check if the clicked cell is valid
            {
                var cellValue = dgGForces.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                if (cellValue != null)
                {
                    Clipboard.SetText(cellValue.ToString());
                }
            }
        }
    }
}

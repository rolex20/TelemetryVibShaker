using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdealProcessorEnhanced;
using System.IO.Pipes;
using System.IO;





namespace FalconExporter
{

    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }
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
        ProcessorAssigner processorAssigner = null;  // Must be alive the while the program is running and is assigned only once if it is the right type of processor

        private uint maxProcessorNumber = 0;
        private bool needToCallSetNewIdealProcessor = true;

        private Thread pipeServerThread;  // IPC_PipeServer Thread for pipes interprocess communications
        private CancellationTokenSource pipeCancellationTokenSource;
        private bool topMost = false; // to fix .TopMost bug




        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);


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
            datagram = new byte[8];
            datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

            ConnectUDP();

            // Switch to Monitor
            if (chkChangeToMonitor.Checked) tabControl1.SelectedIndex = 1;
            DisableChildControls(tabSettings);

            btnStop.Enabled = true;
            btnStart.Enabled = false;
            timer1.Interval = (int)nudFrequency.Value;
            timer1.Enabled = true;
            if (chkAutoMinimize.Checked) this.WindowState = FormWindowState.Minimized;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            EnableChildControls(tabSettings);            

            btnStop.Enabled = false;
            btnStart.Enabled = true;
            UpdateCaption(tsStatus, "Timer stopped.");
            DisconnectUDP();
        }


        private void EnableChildControls(Control control, bool enabled = true)
        {
            foreach (var thisControl in control.Controls)
            {
                (thisControl as Control).Enabled = enabled;
            }
        }
        private void DisableChildControls(Control control)
        {
            EnableChildControls(control, false);
        }

        private void SetNewIdealProcessor(uint maxProcNumber)
        {
            if (maxProcNumber <= 0)
            {
                UpdateCaption(tsStatus, $"Invalid MaxProcessorNumber {maxProcNumber}");
                return;
            }


            // My Intel 14700K has 8 performance cores and 12 efficiency cores.
            // CPU numbers 0-15 are performance
            // CPU numbers 16-27 are efficiency            
            uint newIdealProcessor = processorAssigner.GetNextProcessor();

            IntPtr currentThreadHandle = GetCurrentThread();
            int previousProcessor = (int)SetThreadIdealProcessor(currentThreadHandle, newIdealProcessor);


            if (previousProcessor < 0 || (previousProcessor > maxProcNumber))
            {
                UpdateCaption(tsStatus, $"SetNewIdealProcessor({newIdealProcessor})={previousProcessor}");
                return;
            }
            else
                UpdateCaption(tsStatus, $"Success for SetNewIdealProcessor({newIdealProcessor})");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (needToCallSetNewIdealProcessor)
            {
                needToCallSetNewIdealProcessor = false;
                SetNewIdealProcessor(maxProcessorNumber); // This one also displays the new ideal processor
            }


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
            float speedBrakesPct = 0.0f;
            float gearPct = 0.0f;
            if (totalFuel < lastTelemetry)
            {
                lastTelemetry = totalFuel; 
                speedBrakesPct = _lastFlightData.speedBrake * 100.0f;

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

                // Gear
                gearPct = _lastFlightData.gearPos * 100.0f;
                datagram[7] = (byte)(gearPct);

                // Send Telemetry
                timeStamp = GetTickCount64();
                //udpSender.Send(datagram, datagram.Length);
                udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
            } else if ( totalFuel > lastTelemetry) // when suddenly there is more fuel, it should be because we are in a new plane, (since I don't refuel)
            {
                //TODO: find a better way to detect aircraft change

                lastTelemetry = totalFuel;
                byte[] sendBytes = Encoding.ASCII.GetBytes(txtAircraftName.Text);
                timeStamp = GetTickCount64();
                //udpSender.Send(sendBytes, sendBytes.Length);
                udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
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
                UpdateCaption(lblSpeedBrakes, speedBrakesPct);
                UpdateCaption(lblTrueAirspeed, _lastFlightData.vt*0.592484f ); //convert to knots
                UpdateCaption(lblGForces, _lastFlightData.gs);
                UpdateCaption(lblVehicleType, _lastFlightData.vehicleACD);
                UpdateCaption(lblFuel, totalFuel);
                UpdateCaption(lblTimeStamp, timeStamp);
                UpdateCaption(lblMaxProcessingTime, maxProcessingTime);
                UpdateCaption(lblGear, gearPct);
                if ((bool)tsAircraftChange.Tag)
                {
                    UpdateCaption(tsAircraftChange, timeStamp);
                    tsAircraftChange.Tag = false;
                }
                this.ResumeLayout();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        private Mutex SingleInstanceMutex;

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

            tsAircraftChange.Tag = false; // flag to update the timestamp of the last aircraft change

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);

            if (chkAutoMinimize.Checked)
            {
                Task.Run(() => AutoStart());
            }

            ProcessorCheck();

            // Start Interprocess communication server using named pipes using a different thread
            pipeCancellationTokenSource = new CancellationTokenSource();
            pipeServerThread = new Thread(() => IPC_PipeServer(pipeCancellationTokenSource.Token));
            pipeServerThread.IsBackground = true;
            pipeServerThread.Start();

        }

        // When playing in VR I can't conveniently switch to performance monitor to change settings
        // Instead I user RemoteWindowControl to send IPC pipe messages to WarThunderExporter
        // This way I can change settings more conveniently using a web browser in my phone
        private async void IPC_PipeServer(CancellationToken cancellationToken)
        {
            bool need_to_close = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("FalconExporterPipeCommands", PipeDirection.In))
                {
                    // Wait for a client to connect
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string command;
                        string result = "OK";
                        while ((command = await reader.ReadLineAsync()) != null)
                        {
                            this.Invoke(new Action(() =>
                            {

                                switch (command)
                                {
                                    case "INTERVAL_100":
                                        nudFrequency.Value = 100;
                                        break;
                                    case "INTERVAL_50":
                                        nudFrequency.Value = 50;
                                        break;
                                    case "MONITOR":
                                        tabControl1.SelectTab("tabMonitor");
                                        break;
                                    case "DEBUG":
                                        tabControl1.SelectTab("tabDebug");
                                        break;
                                    case "SETTINGS":
                                        tabControl1.SelectTab("tabSettings");
                                        break;
                                    case "STOP":
                                        if (btnStop.Enabled) btnDisconnect_Click(null, null);
                                        break;
                                    case "START":
                                        if (btnStart.Enabled) btnStart_Click(null, null);
                                        break;
                                    case "CYCLE_STATISTICS":
                                        chkShowStatistics.Checked = !chkShowStatistics.Checked;
                                        break;
                                    case "MINIMIZE":
                                        this.WindowState = FormWindowState.Minimized;
                                        break;
                                    case "RESTORE":
                                        this.WindowState = FormWindowState.Normal;
                                        break;
                                    case "FOREGROUND":
                                        this.BringToFront();
                                        this.Focus();
                                        break;
                                    case string s when s.StartsWith("ALIGN_LEFT"):
                                        if (s.Length > 10)
                                        {
                                            string new_position = s.Substring(10);
                                            int x;
                                            if (int.TryParse(new_position, out x))
                                                this.Location = new Point(x, this.Location.Y);
                                        }
                                        else
                                        {
                                            result = "BAD REQUEST FOR ALIGN_LEFT";
                                        }
                                        break;
                                    case "TOPMOST":
                                        // The following doesn't work, had to fix with local copy
                                        // this.TopMost = !(this.TopMost);
                                        topMost = !topMost;
                                        this.TopMost = topMost;
                                        result = "Now .TopMost=" + this.TopMost.ToString();
                                        break;
                                    case "CLOSE":
                                        need_to_close = true;
                                        pipeCancellationTokenSource.Cancel();
                                        break;
                                    default:
                                        result = "Unknown command";
                                        break;
                                }
                                string info = $"Received command):[{command}][{result}]";
                                UpdateCaption(tsStatus, $"[{GetTickCount64()}]Received command:[{command}][{result}]");
                            }));
                        }
                    }
                }
            }
            // We need to execute the following code on a thread that can modify form objects/properties
            if (need_to_close) this.Invoke(new Action(() =>
            {
                this.Close();
            }));

        }


        private void AutoStart()
        {
            Thread.Sleep(10000);

            this.Invoke((MethodInvoker)delegate
            {
                // Code here will run on the UI thread
                // We can safely interact with UI elements

                // Verify that the user hasn't aborted the autostart or clicked manually on btnStartListening
                if (chkAutoMinimize.Checked && btnStart.Enabled) btnStart_Click(null, null);
            });
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
                    if (cpuCount == 20) {
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
                        chkReassignIdealProcessor.Visible = true;
                        chkReassignIdealProcessor.Enabled = true;
                        maxProcessorNumber = 27;
                        if (processorAssigner == null) processorAssigner = new ProcessorAssigner(maxProcessorNumber);
                        needToCallSetNewIdealProcessor = true; // Force flag because chkReassignIdealProcesso() onclick will miss it since the control wasn't enabled yet
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

        private void chkReassignIdealProcessor_CheckedChanged(object sender, EventArgs e)
        {
            // processor cannot be assigned from the current thread
            // let's signal the need for that operation here
            needToCallSetNewIdealProcessor = chkReassignIdealProcessor.Visible && chkReassignIdealProcessor.Checked;
        }
    }
}

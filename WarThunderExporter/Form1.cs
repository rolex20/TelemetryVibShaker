using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO.Pipes;
using IdealProcessorEnhanced;


namespace WarThunderExporter
{
    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }

    public partial class frmWarThunderTelemetry : Form
    {
        const int RetryInterval = 1500; // Milliseconds

        HttpClient httpClient;
        private Process currentProcess;
        private uint maxProcessorNumber = 0;
        private bool needToCallSetNewIdealProcessor = true;
        private ProcessorAssigner processorAssigner = null;  // Must be alive the while the program is running and is assigned only once if it is the right type of processor

        private UdpClient udpSender;
        private byte[] datagram;
        private Stopwatch stopWatch = new Stopwatch();
        private Stopwatch stopWatchWarThunder = new Stopwatch();
        private int maxProcessingTime; // This is per aircraft
        private int maxWarThunderProcessingTime; // Per aircraft too
        private ulong timeStamp;
        private string? lastAircraftName;
        private CancellationTokenSource cancellationTokenSource;
        private SimpleStatsCalculator internalStats = new SimpleStatsCalculator();
        private SimpleStatsCalculator wtStats = new SimpleStatsCalculator();

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

        public frmWarThunderTelemetry()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();

            txtDebug.Tag = 0; // Used to track the number of errors detected
            timer1.Tag = -1; // Make sure we have an int here for ActivateNewTimerInterval
            txtWtUrl.Tag = false; // flag used to control that the base address only can be changed once

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);

            var handler = new SocketsHttpHandler
            {
                UseProxy = false, // Disable proxy
                //PooledConnectionLifetime = TimeSpan.FromMinutes(10) // Set pooled connection lifetime};
            };
            httpClient = new HttpClient(handler);

            // Start Interprocess communication server using named pipes using a different thread
            pipeCancellationTokenSource = new CancellationTokenSource();
            pipeServerThread = new Thread(() => IPC_PipeServer(pipeCancellationTokenSource.Token));
            pipeServerThread.IsBackground = true;
            pipeServerThread.Start();


            if (chkAutoMinimize.Checked)
            {
                Task.Run(() => AutoStart());
            }


            ProcessorCheck();
        }

        // When playing in VR I can't conveniently switch to performance monitor to change settings
        // Instead I user RemoteWindowControl to send IPC pipe messages to WarThunderExporter
        // This way I can change settings more conveniently using a web browser in my phone
        private async void IPC_PipeServer(CancellationToken cancellationToken)
        {
            bool need_to_close = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("WarThunderExporterPipeCommands", PipeDirection.In))
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
                                        if (btnStop.Enabled) btnStop_Click(null, null);
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
                                        } else
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
                                AddToLog(GetTickCount64(), info, "IPC_PipeServer()", true);

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

        private void frmWarThunderTelemetry_FormClosing(object sender, FormClosingEventArgs e)
        {
            pipeCancellationTokenSource.Cancel();


            txtDebug.Text = String.Empty; // We don't want to save this to Properties.Settings


            if (this.WindowState != FormWindowState.Minimized)
            {
                Properties.Settings.Default.XCoordinate = this.Location.X;
                Properties.Settings.Default.YCoordinate = this.Location.Y;
            }

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


        private void nudFrequency_ValueChanged(object sender, EventArgs e)
        {
            int v = (int)nudFrequency.Value;
            nudFrequency.Tag = v;
            //TimerActivateNewInterval(timer1, v); This is done automatically in the timer1
        }

        private void btnResetMax_Click(object sender, EventArgs e)
        {
            maxProcessingTime = 0;
            maxWarThunderProcessingTime = 0;
            lblMaxProcTimeControl.Tag = false; //flag to skip the first delay
            lblMaxProcessingTimeWT.Tag = false; //flag to skip the first delay
        }

        private void chkShowStatistics_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Visible = chkShowStatistics.Checked;
        }

        private void PrepareMonitorLabels()
        {
            lblAoA.Tag = -1;
            lblSpeed.Tag = -1;
            lblAltitude.Tag = -1;
            lblSpeedBrakes.Tag = -1;
            lblFlaps.Tag = -1;
            lblGForces.Tag = -1;
            lblLastTimeStamp.Tag = 1L; // -1 here caused problems with overflow
            lblMaxProcessingTime.Tag = 0; // no need to use -1 here
            lblAircraftType.Tag = String.Empty;
            lblAircraftType.Text = String.Empty;
            lastAircraftName = String.Empty;

            btnResetMax_Click(null, null);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //this.BeginInvoke(new Action(() =>
            //{
            btnStart.Enabled = false;
            btnStop.Enabled = true;


            // Before connecting udpSender, lets make sure our int cached copy is up to date
            nudFrequency.Tag = (int)nudFrequency.Value;

            PrepareMonitorLabels();

            if (!(bool)txtWtUrl.Tag) // make sure we only change this once, this is an HttpClient limitation
            {
                string baseAddress = SanitizeURL(txtWtUrl.Text);
                httpClient.BaseAddress = new Uri(baseAddress);
                txtWtUrl.Tag = true; // no more changes accepted to base address
            }
            UpdateCaption(tsStatus, $"Connecting to [{httpClient.BaseAddress.ToString()}]...");

            // Allocate the udp datagram
            datagram = new byte[8];
            datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

            // Prepare the UDP connection to the War Thunder Telemetry
            ConnectUDP();

            // Switch to Monitor
            if (chkChangeToMonitor.Checked) tabControl1.SelectedIndex = 1;
            DisableChildControls(tabSettings);
            nudFrequency.Enabled = true; // we still want to be able to change this



            cancellationTokenSource = new CancellationTokenSource();

            // Minimize if required
            if (chkAutoMinimize.Checked) this.WindowState = FormWindowState.Minimized;

            TimerActivateNewInterval(timer1, (int)nudFrequency.Value);

            //}));

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

        private void btnStop_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();

            timer1.Enabled = false;
            EnableChildControls(tabSettings);
            if ((bool)txtWtUrl.Tag) txtWtUrl.Enabled = false; // only can be changed once

            btnStop.Enabled = false;
            btnStart.Enabled = true;
            UpdateCaption(tsStatus, "Operation canceled by the user.");
            DisconnectUDP();
        }
        /*
                private void UpdateCaption(Label L, float value)
                {
                    int twodecimals = (int)(value * 100.0f);
                    if (Convert.ToInt32(L.Tag) != twodecimals)
                    {
                        L.Tag = twodecimals;
                        L.Text = $"{value:F1}";
                        //L.Text = (value >= 0.0f) ? $"{value:F1}" : "---"; // if value<0 then it is not valid or applicable yet
                    }
                }

                private void UpdateCaption(ToolStripLabel L, ulong value)
                {
                    if (Convert.ToUInt64(L.Tag) != value)
                    {
                        L.Tag = value;
                        L.Text = value.ToString();
                        //L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
                    }
                }



                private void UpdateCaption(Label L, int value)
                {
                    if (Convert.ToInt32(L.Tag) != value)
                    {
                        L.Tag = value;
                        L.Text = value.ToString();
                        //L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
                    }
                }



                private void UpdateCaption(Label L, ulong value)
                {
                    if (Convert.ToUInt64(L.Tag) != value)
                    {
                        L.Tag = value;
                        L.Text = value.ToString();
                        //L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
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
        */
        private void UpdateCaption<TControl, TValue>(TControl L, TValue value)
            where TControl : Control
        {
            if (!Equals(L.Tag, value))
            {
                L.Tag = value;
                L.Text = value switch
                {
                    float f => $"{f:F1}",
                    string s => s,
                    _ => value.ToString()
                };
            }
        }

        private void UpdateCaption<TValue>(ToolStripLabel L, TValue value)
        {
            if (!Equals(L.Tag, value))
            {
                L.Tag = value;
                L.Text = value switch
                {
                    float f => $"{f:F1}",
                    string s => s,
                    _ => value.ToString()
                };
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            // Some times there is a long wait caused by War Thunder to deliver the response
            timer1.Enabled = false;

            // Async function call, don't wait
            WarThunderTelemetryAsync(); // this function re-enables timer1 when it finishes
        }

        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }

        private int FindJsonValue(JObject telemetry, string keyName, int defaultValue)
        {
            return telemetry.TryGetValue(keyName, out var value) ? (int)value : defaultValue;
        }

        private float FindJsonValue(JObject telemetry, string keyName, float defaultValue)
        {
            return telemetry.TryGetValue(keyName, out var value) ? (float)value : defaultValue;
        }

        private float FindJsonValue(JObject telemetry, string keyName, float defaultValue, float maxValue)
        {
            if (telemetry.TryGetValue(keyName, out var value))
                return (float)value <= maxValue ? (float)value : maxValue;
            else
                return defaultValue;
            //return telemetry.TryGetValue(keyName, out var value) ? (float)value : defaultValue;
        }

        private string FindJsonValue(JObject telemetry, string keyName, string defaultValue)
        {
            return telemetry.TryGetValue(keyName, out var value) ? (string)value : defaultValue;
        }

        private void SetNewIdealProcessor(uint maxProcNumber)
        {
            if (maxProcNumber <= 0)
            {
                AddToLog(GetTickCount64(), "Invalid MaxProcessorNumber", $"SetNewIdealProcessor({maxProcNumber})", true);
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
                AddToLog(GetTickCount64(), "Call Failed. ", $"SetNewIdealProcessor({newIdealProcessor})={previousProcessor}", true);
            }
            else
            {
                AddToLog(GetTickCount64(), "Call Succeeded. ", $"SetNewIdealProcessor({newIdealProcessor})={previousProcessor}", true);
            }
        }

        private void AddToLog(ulong timestamp, string message, string source, bool force = false)
        {
            int currentLinesCount = (int)txtDebug.Tag;
            if (currentLinesCount < 100 || force) // only show first 100 errors
            {
                // Show error information                    
                txtDebug.AppendText($"[{timeStamp}] [{source}] {message} " + Environment.NewLine);

                // The first time we get an error, let's show it to the user
                if (currentLinesCount == 0)
                {
                    tabControl1.SelectedIndex = 2; // Debug tab
                    txtDebug.Tag = currentLinesCount + 1; // Only let's call the attention on the first error
                }
            }

        }

        // This is where the War Thunder Telemetry is read
        // More info here: https://github.com/lucasvmx/WarThunder-localhost-documentation
        // The function is large because timer1 has issues with async calls and because
        // I wanted to avoid inherinting all async markings to all functions.
        // The function is large, but straightforward to understand and almost no indirection-levels of abrstraction required to understand it
        private async Task WarThunderTelemetryAsync()
        {
            if (needToCallSetNewIdealProcessor)
            {
                needToCallSetNewIdealProcessor = false;
                SetNewIdealProcessor(maxProcessorNumber); // This one also displays the new ideal processor
            }



            // Some times there is a long wait caused by War Thunder to deliver the response
            // timer1.Enabled = false; // Now this is done in the caller

            // Avoid repetitive calls to the getter
            bool ShowStatistics_cache = chkShowStatistics.Checked;

            bool normalTimerReactivation = false;

            try
            {

                // Get indicators-telemetry data from War Thunder
                if (ShowStatistics_cache) stopWatchWarThunder.Restart(); // Measure War Thunder Response time
                HttpResponseMessage response2 = await httpClient.GetAsync("indicators", cancellationTokenSource.Token); //txtWtUrl
                response2.EnsureSuccessStatusCode();
                string responseBody2 = await response2.Content.ReadAsStringAsync();

                if (ShowStatistics_cache)
                {
                    stopWatchWarThunder.Stop(); // This timer is only for War Thunder web services measurements
                    stopWatch.Restart(); // This timer is only for our own processing
                }

                // Let's check if the request was canceled
                if (cancellationTokenSource.Token.IsCancellationRequested) return;


                // Parse the JSON data
                JObject telemetryIndicators = JObject.Parse(responseBody2);

                // Extract the required telemetry information
                string aircraftName = FindJsonValue(telemetryIndicators, "type", String.Empty);
                if (aircraftName.Length > 0)
                {
                    float altitudeInFeets = FindJsonValue(telemetryIndicators, "altitude_10k", 0.0f);
                    float gMeter = FindJsonValue(telemetryIndicators, "g_meter", 0.0f);



                    if (ShowStatistics_cache)
                    {
                        stopWatch.Stop();
                        stopWatchWarThunder.Start();
                    }

                    // Get state-telemetry data from War Thunder                    
                    HttpResponseMessage response1 = await httpClient.GetAsync("state", cancellationTokenSource.Token);
                    response1.EnsureSuccessStatusCode();
                    string responseBody1 = await response1.Content.ReadAsStringAsync();

                    if (ShowStatistics_cache)
                    {
                        stopWatchWarThunder.Stop();
                        stopWatch.Start();
                    }
                    timeStamp = GetTickCount64();  // Timestamping here is safer than the previous call which might return with empty data

                    // Let's check if the request was canceled
                    if (cancellationTokenSource.Token.IsCancellationRequested) return;

                    // Parse the JSON data
                    JObject telemetryState = JObject.Parse(responseBody1);

                    // Extract the required telemetry information
                    float speedInKnots = FindJsonValue(telemetryState, "TAS, km/h", 0.0f) * 0.53995680345572f;//telemetryState["TAS, km/h"].Value<float>() * 0.53995680345572f;
                    int flapsStatus = FindJsonValue(telemetryState, "flaps, %", 0); // telemetryState["flaps, %"].Value<int>();
                    int airBrakePct = FindJsonValue(telemetryState, "airbrake, %", 0);// telemetryState["airbrake, %"].Value<int>();
                    float angleOfAttack = FindJsonValue(telemetryState, "AoA, deg", 0.0f, 255.0f);// telemetryState["AoA, deg"].Value<float>();
                    int gearPct = FindJsonValue(telemetryState, "gear, %", 0);

                    if (aircraftName.Equals(lastAircraftName)) // just send the telemetry for the same aircraft
                    {
                        /*
                        * 
                        * Prepare UDP Datagram
                        * 
                        * */

                        // already done
                        // datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

                        datagram[1] = angleOfAttack >= 0.0f ? (byte)angleOfAttack : (byte)0; // AoA, degrees
                        datagram[2] = (byte)airBrakePct; // Speed brakes, %
                        datagram[3] = (byte)flapsStatus; // Flaps, %

                        // True air speed conversion:
                        // We have knots, we need decameters/s to make sure it fits in one byte, resolution lost here, don't care/need that resolution in this app
                        datagram[4] = (byte)(speedInKnots * 0.05144444444f);  // 1 knots equals 0.05144444 decameters per second

                        datagram[5] = (byte)gMeter; // G force

                        // Altitude conversion: From feet to decameters
                        datagram[6] = (byte)(altitudeInFeets * 0.0304799999536704f);

                        // Gear
                        datagram[7] = (byte)gearPct; //Gear

                        // Send Telemetry
                        udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);

                    }
                    else // let's report the new aircraft name/type we are flying
                    {
                        internalStats.Initialize();
                        wtStats.Initialize();
                        btnResetMax_Click(null, null); // this is per aircraft
                        lastAircraftName = (string)aircraftName;
                        byte[] sendBytes = Encoding.ASCII.GetBytes(lastAircraftName);
                        udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
                    }

                    // Update UI
                    if (tabControl1.SelectedIndex == 1 && chkShowStatistics.Checked && this.WindowState != FormWindowState.Minimized)
                    {
                        this.SuspendLayout();
                        UpdateCaption(tsStatus, "Connected.");
                        UpdateCaption(lblAoA, angleOfAttack);
                        UpdateCaption(lblSpeed, speedInKnots);
                        UpdateCaption(lblAltitude, altitudeInFeets);
                        UpdateCaption(lblSpeedBrakes, airBrakePct);
                        UpdateCaption(lblFlaps, flapsStatus);
                        UpdateCaption(lblGForces, gMeter);
                        UpdateCaption(lblAircraftType, (string)aircraftName);
                        UpdateCaption(lblLastTimeStamp, timeStamp);
                        UpdateCaption(lblMaxProcessingTime, internalStats.Max());
                        UpdateCaption(lblAvgProcessingTime, internalStats.Average());
                        UpdateCaption(lblMinProcessingTime, internalStats.Min());
                        UpdateCaption(lblMaxProcessingTimeWT, wtStats.Max());
                        UpdateCaption(lblAvgProcessingTimeWT, wtStats.Average());
                        UpdateCaption(lblMinProcessingTimeWT, wtStats.Min());
                        this.ResumeLayout();
                    }
                    normalTimerReactivation = true;
                } //if-then (aircraftName.Length>0) 
                else // let's try again, but let's wait 1 second
                {
                    //if war-thunder is not sending an aircraft-type, let's force a change for the next time
                    lastAircraftName = String.Empty;
                    TimerActivateNewInterval(timer1, RetryInterval); // nudFrequency.Tag = (int)nudFrequency.Value
                } //if-then-else (aircraftName.Length>0)



            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                RecoverFromTypicalException("Error parsing Json WarThunder Telemetry response.  Retrying soon...", true);
            }

            catch (OperationCanceledException) // Operation canceled by user, this is no error
            {
                RecoverFromTypicalException(null, false); // Status Text will be set in btnStop_Click()
            }
            catch (InvalidOperationException)
            {
                RecoverFromTypicalException("requestUri must be an absolute URI or BaseAddress must be set.", true);
            }
            catch (HttpRequestException)
            {
                RecoverFromTypicalException("Request failed (network connectivity, timeout, etc).  Retrying soon...", true);
            }
            catch (UriFormatException)
            {
                RecoverFromTypicalException("URI is not valid relative or absolute URI.  Retrying soon...", true);
            }

            catch (Exception ex)
            {
                AddToLog(timeStamp, ex.Message, "WarThunderTelemetryAsync-catch()");
                RecoverFromTypicalException("Error Retrieveing Telemtry Data.  Retrying soon...", true);
            }

            if (ShowStatistics_cache)
            {
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                internalStats.AddSample(elapsed);
                if (maxProcessingTime < elapsed)
                {
                    if ((bool)lblMaxProcTimeControl.Tag) // I want to ignore the first one
                        maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
                    //else  lblMaxProcTimeControl.Tag = true; // Now, this is done below.  (Next, time, the maxProcessingTime will be updated)
                }

                elapsed = stopWatchWarThunder.Elapsed.Milliseconds;
                wtStats.AddSample(elapsed);
                if (maxWarThunderProcessingTime < elapsed)
                {
                    if ((bool)lblMaxProcTimeControl.Tag) // I can reuse here
                        maxWarThunderProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
                    else
                        lblMaxProcTimeControl.Tag = true; // Next time, the maxWarThunderProcessingTime will be updated
                }


            }

            if (normalTimerReactivation) TimerActivateNewInterval(timer1, (int)nudFrequency.Tag); // .Tag has a duplicate using int as type
        }

        private void RecoverFromTypicalException(string? msg, bool reenable)
        {
            ulong nowTimeStamp = GetTickCount64(); // don't want to timestamp errors, only valid telemetry
            //UpdateCaption(lblLastTimeStamp, timeStamp);
            if (msg is not null)
            {
                UpdateCaption(tsStatus, $"[{nowTimeStamp}] {msg}");
            }
            if (reenable) TimerActivateNewInterval(timer1, RetryInterval);  // Wait more time before trying again
        }

        private void TimerActivateNewInterval(System.Windows.Forms.Timer timer, int interval)
        {
            if (timer.Interval != (int)timer.Tag) // let's use the Tag-duplicated value to not call this property change if not really needed
            {
                timer.Interval = interval;
                timer.Tag = interval;
            }


            // If we are in running state, then udpSender is not null and we need to enable the timer
            if (udpSender is not null && !(cancellationTokenSource.Token.IsCancellationRequested))
            {
                timer.Enabled = true;
            }
        }


        private string SanitizeURL(string input)
        {
            // Trim spaces at the beginning and end of the string
            string sanitizedString = input.Trim();

            // Add trailing slash if it doesn't exist
            if (!sanitizedString.EndsWith("/"))
            {
                sanitizedString += "/";
            }

            return sanitizedString;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        private Mutex singleInstanceChecker;

        private void SingleInstanceChecker()
        {
            singleInstanceChecker = new Mutex(true, this.Text + "_mutex", out bool createdNew);
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

        private void AllowOnlyDigits(object sender, KeyPressEventArgs e)
        {
            // Only allow numbers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }


        private void txtDestinationPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            AllowOnlyDigits(sender, e);
        }

        private void chkReassignIdealProcessor_CheckedChanged(object sender, EventArgs e)
        {
            // processor cannot be assigned from the current thread
            // let's signal the need for that operation here
            needToCallSetNewIdealProcessor = chkReassignIdealProcessor.Visible && chkReassignIdealProcessor.Checked;
        }
    }
}

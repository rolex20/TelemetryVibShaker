using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Text;


namespace WarThunderExporter
{
    public partial class frmWarThunderTelemetry : Form
    {
        HttpClient httpClient;
        private Process currentProcess;
        private UdpClient udpSender;
        private byte[] datagram;
        private Stopwatch stopWatch = new Stopwatch();
        private Stopwatch stopWatchWarThunder = new Stopwatch();
        private int maxProcessingTime; // This is per aircraft
        private int maxWarThunderProcessingTime; // Per aircraft too
        private ulong timeStamp;
        private string? lastAircraftName;
        private CancellationTokenSource cancellationTokenSource;


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

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
            txtDebug.Tag = 0; // Used to track the number of errors detected
            timer1.Tag = -1; // Make sure we have an int here for ActivateNewTimerInterval
            txtWtUrl.Tag = false; // flag used to control that the base address only can be changed once

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);
            ProcessorCheck();

            var handler = new SocketsHttpHandler
            {
                UseProxy = false, // Disable proxy
                //PooledConnectionLifetime = TimeSpan.FromMinutes(10) // Set pooled connection lifetime};
            };

            httpClient = new HttpClient(handler);
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
            txtDebug.Text = String.Empty; // We don't want to save this to Properties.Settings

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

        private void nudFrequency_ValueChanged(object sender, EventArgs e)
        {
            int v = (int)nudFrequency.Value;
            nudFrequency.Tag = v;
            TimerActivateNewInterval(timer1, v);
        }

        private void btnResetMax_Click(object sender, EventArgs e)
        {
            maxProcessingTime = 0;
            maxWarThunderProcessingTime = 0;
            lblMaxProcTimeControl.Tag = false; //flag to skip the first frame
            lblMaxWarThunderProcessingTime.Tag = false;
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
            datagram = new byte[7];
            datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

            // Prepare the UDP connection to the War Thunder Telemetry
            ConnectUDP();

            // Switch to Monitor
            if (chkChangeToMonitor.Checked) tabControl1.SelectedIndex = 1;
            DisableChildControls(tabSettings);
            nudFrequency.Enabled = true; // we still want to be able to change this


            btnStop.Enabled = true;
            btnStart.Enabled = false;

            cancellationTokenSource = new CancellationTokenSource();

            TimerActivateNewInterval(timer1, (int)nudFrequency.Value);
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


        private void UpdateCaption(Label L, int value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = value.ToString();
                //L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
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


        private void timer1_Tick(object sender, EventArgs e)
        {
            WarThunderTelemetryAsync();
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


        // This is where the War Thunder Telemetry is read
        // More info here: https://github.com/lucasvmx/WarThunder-localhost-documentation
        // The function is large because timer1 has issues with async calls and because
        // I wanted to avoid inherinting all async markings to all functions.
        // The function is large, but straightforward to understand and almost no indirection-levels required to understand it
        private async Task WarThunderTelemetryAsync()
        {
            timer1.Enabled = false; // some times there is a long wait caused by War Thunder to deliver the response

            try
            {

                // Get indicators-telemetry data from War Thunder
                if (chkShowStatistics.Checked) stopWatchWarThunder.Restart(); // Measure War Thunder Response time
                    HttpResponseMessage response2 = await httpClient.GetAsync("indicators", cancellationTokenSource.Token); //txtWtUrl
                    response2.EnsureSuccessStatusCode();
                    string responseBody2 = await response2.Content.ReadAsStringAsync();
                if (chkShowStatistics.Checked) stopWatchWarThunder.Stop();

                // I am not counting the possible long delay-first-response from war thunder
                if (chkShowStatistics.Checked) stopWatch.Restart(); 

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
                    if (chkShowStatistics.Checked) stopWatch.Stop();

                    // Get state-telemetry data from War Thunder
                    if (chkShowStatistics.Checked) stopWatchWarThunder.Start();
                    HttpResponseMessage response1 = await httpClient.GetAsync("state", cancellationTokenSource.Token);
                    response1.EnsureSuccessStatusCode();
                    string responseBody1 = await response1.Content.ReadAsStringAsync();
                    if (chkShowStatistics.Checked) stopWatchWarThunder.Stop();

                    if (chkShowStatistics.Checked) stopWatch.Start();
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

                        // Send Telemetry
                        udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);

                    }
                    else // let's report the new aircraft name/type we are flying
                    {
                        btnResetMax_Click(null, null); // this is per aircraft
                        lastAircraftName = (string)aircraftName;
                        byte[] sendBytes = Encoding.ASCII.GetBytes(lastAircraftName);
                        udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
                    }

                    // Update UI
                    if (tabControl1.SelectedIndex == 1 && chkShowStatistics.Checked)
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
                        UpdateCaption(lblMaxProcessingTime, maxProcessingTime);
                        UpdateCaption(lblMaxWarThunderProcessingTime, maxWarThunderProcessingTime);
                        this.ResumeLayout();
                    }

                    TimerActivateNewInterval(timer1, (int)nudFrequency.Tag); // nudFrequency.Tag = (int)nudFrequency.Value

                } //if-then (aircraftName.Length>0) 
                else // let's try again, but let's wait 1 second
                {
                    //if war-thunder is not sending an aircraft-type, let's force a change for the next time
                    lastAircraftName = String.Empty;
                    TimerActivateNewInterval(timer1, 1000); // nudFrequency.Tag = (int)nudFrequency.Value
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

                if ((int)txtDebug.Tag < 100) // only show first 100 errors
                {
                    // Show error information                    
                    txtDebug.AppendText($"[{timeStamp}] {ex.Message}" + Environment.NewLine);

                    // The first time we get an error, let's show it to the user
                    if ((int)txtDebug.Tag == 0)
                    {
                        tabControl1.SelectedIndex = 2; // Debug tab
                        txtDebug.Tag = (int)txtDebug.Tag + 1; // Only let's call the attention on the first error
                    }
                }
                RecoverFromTypicalException("Error Retrieveing Telemtry Data.  Retrying soon...", true);
            }

            if (chkShowStatistics.Checked)
            {
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                if (maxProcessingTime < elapsed)
                {
                    if ((bool)lblMaxProcTimeControl.Tag) // I want to ignore the first one
                        maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
                    lblMaxProcTimeControl.Tag = true; // Next, time, the maxProcessingTime will be updated
                }

                elapsed = stopWatchWarThunder.Elapsed.Milliseconds;
                if (maxWarThunderProcessingTime < elapsed)
                {
                    if ((bool)lblMaxWarThunderProcessingTime.Tag) // I want to ignore the first one
                        maxWarThunderProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
                    lblMaxWarThunderProcessingTime.Tag = true; // Next, time, the maxWarThunderProcessingTime will be updated
                }


            }

        }

        private void RecoverFromTypicalException(string? msg, bool reenable)
        {
            ulong nowTimeStamp = GetTickCount64(); // don't want to timestamp errors, only valid telemetry
            //UpdateCaption(lblLastTimeStamp, timeStamp);
            if (msg is not null)
            {
                UpdateCaption(tsStatus, $"[{nowTimeStamp}] {msg}");
            }
            if (reenable) TimerActivateNewInterval(timer1, 1000);  // Wait more time before trying again
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


    }
}

using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Text;


namespace WarThunderExporter
{
    public partial class frmWarThunderTelemetry : Form
    {
        HttpClient httpClient = new HttpClient();
        private Process currentProcess;
        private UdpClient udpSender;
        private byte[] datagram;
        private Stopwatch stopWatch = new Stopwatch();
        private int maxProcessingTime;
        private ulong timeStamp;
        private string? lastAircraftName, indicators_url, state_url;
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
        }

        private void chkShowStatistics_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Visible = chkShowStatistics.Checked;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Allocate the udp datagram
            datagram = new byte[7];
            datagram[0] = 1; // flag to indicate this is a telemetry datagram for my TelemetryVibShaker program

            ConnectUDP();

            // Switch to Monitor
            if (chkChangeToMonitor.Checked) tabControl1.SelectedIndex = 1;
            DisableChildControls(tabSettings);


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

            btnStop.Enabled = false;
            btnStart.Enabled = true;
            UpdateCaption(tsStatus, "Operation canceled by the user.");
            DisconnectUDP();
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


        private void timer1_Tick(object sender, EventArgs e)
        {
            WarThunderTelemetryAsync();
        }

        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }


        // This is how to read Telemetry from War Thunder
        // More info here: https://github.com/lucasvmx/WarThunder-localhost-documentation
        // The function is large because timer1 has issues with async calls and because
        // I wanted to avoid inherinting all async markings to all functions.
        // The function is large, but straightforward to understand and almost no indirection-levels required to understand it
        private async Task WarThunderTelemetryAsync()
        {
            timer1.Enabled = false;

            if (chkShowStatistics.Checked)
            {
                stopWatch.Restart();
            }

            try
            {

                // Get indicators-telemetry data from War Thunder
                HttpResponseMessage response2 = await httpClient.GetAsync(indicators_url, cancellationTokenSource.Token); //txtWtUrl
                response2.EnsureSuccessStatusCode();
                string responseBody2 = await response2.Content.ReadAsStringAsync();

                // Parse the JSON data
                JObject telemetryIndicators = JObject.Parse(responseBody2);

                // Extract the required telemetry information
                string aircraftName = telemetryIndicators["type"].Value<string>();
                float altitudeInFeets = telemetryIndicators["altitude_10k"].Value<float>();
                float gMeter = telemetryIndicators["g_meter"].Value<float>();

                if (aircraftName is not null) // if we don't have an aircraft name, then we are not flying
                {
                    // Get state-telemetry data from War Thunder
                    HttpResponseMessage response1 = await httpClient.GetAsync(state_url, cancellationTokenSource.Token);
                    response1.EnsureSuccessStatusCode();
                    string responseBody1 = await response1.Content.ReadAsStringAsync();
                    timeStamp = GetTickCount64();

                    // Parse the JSON data
                    JObject telemetryState = JObject.Parse(responseBody1);

                    // Extract the required telemetry information
                    float speedInKnots = telemetryState["TAS, km/h"].Value<float>() * 0.53995680345572f;
                    int flapsStatus = telemetryState["flaps, %"].Value<int>();
                    int airBrakePct = telemetryState["airbrake, %"].Value<int>();
                    float angleOfAttack = telemetryState["AoA, deg"].Value<float>();

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
                        lastAircraftName = aircraftName;
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
                        UpdateCaption(lblGForces, gMeter);
                        UpdateCaption(lblAircraftType, aircraftName);
                        UpdateCaption(lblLastTimeStamp, timeStamp);
                        UpdateCaption(lblMaxProcessingTime, maxProcessingTime);
                        this.ResumeLayout();
                    }

                    TimerActivateNewInterval(timer1, (int)nudFrequency.Tag); // nudFrequency.Tag = (int)nudFrequency.Value

                } //if-then (aircraftName is not null) 
                else // let's try again, but let's wait 1 second
                {
                    TimerActivateNewInterval(timer1, 1000); // nudFrequency.Tag = (int)nudFrequency.Value
                } //if-then-else (aircraftName) is not null



            }
            catch (OperationCanceledException) // Operation canceled by user, this is no error
            {
                timeStamp = GetTickCount64();
                UpdateCaption(lblLastTimeStamp, timeStamp);
                // UpdateCaption(tsStatus, "Operation canceled by the user."); // already done in btnStop_Click()
            }
            catch (Exception ex)
            {
                timeStamp = GetTickCount64();
                UpdateCaption(lblLastTimeStamp, timeStamp);
                if ((int)txtDebug.Tag < 100) // only show first 100 errors
                {
                    // Show error information                    
                    txtDebug.AppendText($"[{timeStamp}] {ex.Message}" + Environment.NewLine);
                    UpdateCaption(tsStatus, "Error Retrievieng Telemetry Data. Retrying...");                    

                    // The first time we get an error, let's show it to the user
                    if ((int)txtDebug.Tag == 0)
                    {
                        tabControl1.SelectedIndex = 2; // Debug tab
                        txtDebug.Tag = (int)txtDebug.Tag + 1; // Only let's call the attention on the first error
                    }
                }

                TimerActivateNewInterval(timer1, 1000);  // Wait more time before trying again
            }

            if (chkShowStatistics.Checked)
            {
                stopWatch.Stop();
                int elapsed = stopWatch.Elapsed.Milliseconds;
                if (maxProcessingTime < elapsed) maxProcessingTime = elapsed;  // this is going to be delayed by one cycle, but it's okay
            }

        }

        private void TimerActivateNewInterval(System.Windows.Forms.Timer timer, int interval)
        {
            if (timer.Interval != (int)timer.Tag) // let's use the Tag-duplicated value to not call this property change if not really needed
            {
                timer.Interval = interval;
                timer.Tag = interval;
            }


            // If we are in running state, then udpSender is not null and we want to need the timer
            if (udpSender is not null)
            {
                timer.Enabled = true;
            }
        }

        private void txtWtUrl_TextChanged(object sender, EventArgs e)
        {
            txtWtUrl.Text = SanitizeURL(txtWtUrl.Text);

            indicators_url = txtWtUrl.Text + "/indicators";
            state_url = txtWtUrl.Text + "/state";
        }

        private string SanitizeURL(string input)
        {
            // Trim spaces at the beginning and end of the string
            string trimmedString = input.Trim();

            // Remove trailing slash if it exists
            if (trimmedString.EndsWith("/"))
            {
                trimmedString = trimmedString.Remove(trimmedString.Length - 1);
            }

            return trimmedString;
        }
    }
}

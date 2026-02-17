using NAudio.Wave;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System.IO.Pipes;
using System.Windows.Forms;
using IdealProcessorEnhanced;


namespace TelemetryVibShaker
{

    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }

    public partial class frmMain : Form
    {
        private const int ARDUINO = 0;
        private const int TWATCH = 1;

        private MotorController[]? motorControllers; // currently the interface is fixed to 2 (Arduino and TWatch) and each with 2 effects
        private EffectDefinition[]? arduinoEffects;  // currently only two vibration motors connected
        private EffectDefinition[]? TWatchEffects; // curently only 1 motor and 1 display connected
        private AoA_SoundManager? soundManager;  // manages sound effects according to the current AoA
        private TelemetryServer? telemetry;      // controls all telemetry logic
        private Thread? threadTelemetry; // this thread runs the telemetry        
        private SimpleStatsCalculator Stats = new SimpleStatsCalculator();  // milliseconds elapsed in processing the monitor-update-cycle in Timer1 (UI)
        private Stopwatch? stopWatchUI;  // used to measure processing time in Timer1 (updating the UI)
        private Process? currentProcess;
        private ProcessorAssigner processorAssigner = null;  // Must be alive the while the program is running and is assigned only once if it is the right type of processor

        private Thread pipeServerThread;  // IPC_PipeServer Thread for pipes interprocess communications
        private CancellationTokenSource pipeCancellationTokenSource;

        private bool topMost = false; // to fix .TopMost bug



        private const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
        private const uint PROCESS_MODE_BACKGROUND_END = 0x00200000;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();

        //[DllImport("kernel32.dll")]
        //private static extern uint GetTickCount();

        private uint maxProcessorNumber = 0;
        private bool needToCallSetNewIdealProcessor = true;

        public frmMain()
        {
            InitializeComponent();
        }


        private bool CheckFileExists(TextBox tb, string file)
        {
            bool exist = File.Exists(tb.Text);
            if (!exist)
            {
                MessageBox.Show("The file does not exist.", file, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tb.SelectAll();
                tb.Focus();
            }
            return exist;
        }

        private void btnSoundEffect1_Click(object sender, EventArgs e)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            updateSelectedFile(txtSoundEffect1, "Select an NWAVE compatible audio file", btnSoundEffect1.Tag as string);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private void btnSoundEffect2_Click(object sender, EventArgs e)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            updateSelectedFile(txtSoundEffect2, "Select an NWAVE compatible audio file", btnSoundEffect2.Tag as string);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private DialogResult updateSelectedFile(TextBox tb, string title, string filter)
        {
            DialogResult result;
            openFileDialog1.Filter = filter;
            openFileDialog1.FileName = tb.Text;
            openFileDialog1.Title = title;
            result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
                tb.Text = openFileDialog1.FileName;

            return result;
        }


        private void updateEffectsTimeout()
        {
            lblEffectTimeout.Text = trkEffectTimeout.Value.ToString() + " second(s)";
            lblEffectTimeout.Tag = trkEffectTimeout.Value;
        }

        private void btnJSONFile_Click(object sender, EventArgs e)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            DialogResult jsonSelection = updateSelectedFile(txtJSON, "Select an JSON file defining AoA for each aircraft", btnJSONFile.Tag as string);
#pragma warning restore CS8604 // Possible null reference argument.
            if (jsonSelection == DialogResult.OK)
            {
                string error = TelemetryServer.TestJSONFile(txtJSON.Text);
                if (!string.IsNullOrEmpty(error))
                    MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Visible = chkShowStatistics.Checked;
        }

        private void updateVolumeMultiplier(Label label, TrackBar trackBar)
        {
            label.Text = trackBar.Value.ToString() + "%";
            label.Tag = trackBar.Value;
        }

        private void trkVolumeMultiplier1_Scroll(object sender, EventArgs e)
        {
            updateVolumeMultiplier(lblVolumeMultiplier1, trkVolumeMultiplier1);
            if (soundManager != null)
                soundManager.VolumeAmplifier1 = (float)trkVolumeMultiplier1.Value / 100.0f;
        }

        private void trkVolumeMultiplier2_Scroll(object sender, EventArgs e)
        {
            updateVolumeMultiplier(lblVolumeMultiplier2, trkVolumeMultiplier2);
            if (soundManager != null)
                soundManager.VolumeAmplifier2 = (float)trkVolumeMultiplier2.Value / 100.0f;

        }

        private void FillAudioDevices()
        {


            //This version has a problem is likely due to a limitation within the NAudio library. Specifically, the WaveOut.GetCapabilities method returns a ProductName that is truncated to a maximum of 31 characters
            var enumerator = new MMDeviceEnumerator();
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var capabilities = WaveOut.GetCapabilities(n);
                cmbAudioDevice1.Items.Add(capabilities.ProductName);

                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[n];
                Debug.Print(device.FriendlyName);
            }
            /*
                        // Create enumerator
                        var enumerator = new MMDeviceEnumerator();
                        // Skip the -1 Microsoft Audio Mapper
                        for (int n = 0; n < WaveOut.DeviceCount; n++)
                        {
                            var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[n];
                            cmbAudioDevice1.Items.Add($"{device.ID} - {device.InstanceId} - {device.FriendlyName}");
                        }
            */
        }



        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                pipeCancellationTokenSource.Cancel();

                // StopEffects the telemetry if it is still running: kill the upd server, etc
                btnStop_Click(null, null);


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
            catch { }
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

        // Only update status if, UI is not up to date.
        private void UpdateSoundEffectStatus(SoundEffectStatus newStatus)
        {

            SoundEffectStatus currentStatus = (SoundEffectStatus)lblSoundStatus.Tag;

            if (currentStatus != newStatus)
            {
                lblSoundStatus.Tag = newStatus;
                switch (newStatus)
                {
                    case SoundEffectStatus.NotPlaying:
                        lblSoundStatus.Text = "Not playing sounds.";
                        break;
                    case SoundEffectStatus.Ready:
                        lblSoundStatus.Text = "Sound effects ready.";
                        break;
                    case SoundEffectStatus.Playing1:
                        lblSoundStatus.Text = "Playing in-AoA effect...";
                        break;
                    case SoundEffectStatus.Playing2:
                        lblSoundStatus.Text = "Playing above-AoA effect...";
                        break;
                    case SoundEffectStatus.Canceled:
                        lblSoundStatus.Text = "Alarm canceled.";
                        break;
                }
            }

        }



        private void frmMain_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();

            // Initialize max timers
            btnResetMax_Click(null, null);

            // Flag to skip the initial max UI processing time
            lblMaxProcessingTimeTitle.Tag = false;

            stopWatchUI = new Stopwatch();

            // The TextChanged event is not exposed in the Designer:
#pragma warning disable CS8622 // Dereference of a possibly null reference
            numMinIntensitySpeedBrakes.TextChanged += numMinIntensitySpeedBrakes_ValueChanged;
            numMaxIntensitySpeedBrakes.TextChanged += numMaxIntensitySpeedBrakes_ValueChanged;
            numMinIntensityFlaps.TextChanged += numMinIntensityFlaps_ValueChanged;
            numMaxIntensityFlaps.TextChanged += numMaxIntensityFlaps_ValueChanged;
#pragma warning restore CS8622 // Dereference of a possibly null reference

            // This tags are used to make sure no double processing is made between _TextChanged and _ValueChanged events
            numMinIntensitySpeedBrakes.Tag = (long)0;
            numMaxIntensitySpeedBrakes.Tag = (long)0;
            numMinIntensityFlaps.Tag = (long)0;
            numMaxIntensityFlaps.Tag = (long)0;

            soundManager = null;
            telemetry = null;
            motorControllers = null;
            TWatchEffects = null;
            arduinoEffects = null;


            FillAudioDevices();

            // Load the settings for all controls in the form
            LoadSettings(this);


            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);

            updateVolumeMultiplier(lblVolumeMultiplier1, trkVolumeMultiplier1);
            updateVolumeMultiplier(lblVolumeMultiplier2, trkVolumeMultiplier2);
            updateEffectsTimeout();

            lblCurrentUnitType.Tag = string.Empty;
            lblLastAoA.Tag = (float)0.0;

            lblDatagramsPerSecond.Text = string.Empty;
            lblTestErrMsg.Text = string.Empty;


            btnStop.Tag = false;
            lblSoundStatus.Tag = SoundEffectStatus.Invalid;
            UpdateSoundEffectStatus(SoundEffectStatus.NotPlaying);

            chkPlayAlarm.Enabled = true;

            // Start Interprocess communication server using named pipes using a different thread
            pipeCancellationTokenSource = new CancellationTokenSource();
            pipeServerThread = new Thread(() => IPC_PipeServer(pipeCancellationTokenSource.Token));
            pipeServerThread.IsBackground = true;
            pipeServerThread.Start();



            if (chkAutoStart.Checked & btnStartListening.Enabled)
            {
                Task.Run(() => AutoStartTelemetryVibShaker());
            }

            // Adjust affinity, priority class based on processor and previous saved settings - loaded in LoadSettings()
            ProcessorCheck();

        }


        // When playing in VR I can't conveniently switch to performance monitor to change settings
        // Instead I user RemoteWindowControl to send IPC pipe messages to TelemetryVibShaker
        // This way I can change settings more conveniently using a web browser in my phone
        private async void IPC_PipeServer(CancellationToken cancellationToken)
        {
            bool close_requested = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("TelemetryVibShakerPipeCommands", PipeDirection.In))
                {
                    Debug.WriteLine("Named pipe server started. Waiting for connection...");

                    // Wait for a client to connect
                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                    Debug.WriteLine("Client connected.");

                    int lines = 0;
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
                                    case "MONITOR":
                                        tabs.SelectTab("tabMonitor");
                                        break;
                                    case "NOT_FOUNDS":
                                        tabs.SelectTab("tabNotFounds");
                                        break;
                                    case "SETTINGS":
                                        tabs.SelectTab("tabSettings");
                                        break;
                                    case "STOP":
                                        if (btnStop.Enabled) btnStop_Click(null, null);
                                        break;
                                    case "START":
                                        if (btnStartListening.Enabled) btnStartListening_Click(null, null);
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
                                        close_requested = true;
                                        pipeCancellationTokenSource.Cancel();
                                        break;


                                    default:
                                        result = "Unknown command";
                                        break;
                                }
                                string info = $"Received command #${++lines}):[{command}][{result}]";
                                Debug.WriteLine(info);
                                //LogError(info, "PipeServer", true);

                            }));
                        }
                    }
                }
            }

            // We need to execute the following code on a thread that can modify form objects/properties
            if (close_requested) this.Invoke(new Action(() =>
            {
                this.Close();
            }));

        }


        private void AutoStartTelemetryVibShaker()
        {
            for (int i = 10; i >= 0; i--)
            {
                lblCountDownTimer.BeginInvoke(new Action(() => { lblCountDownTimer.Text = i.ToString(); }));
                if (!chkAutoStart.Checked || !btnStartListening.Enabled) break;
                System.Threading.Thread.Sleep(1000);
            }
            lblCountDownTimer.BeginInvoke(new Action(() => { lblCountDownTimer.Text = String.Empty; }));

            this.Invoke((MethodInvoker)delegate
            {
                // Code here will run on the UI thread
                // We can safely interact with UI elements

                // Verify that the user hasn't aborted the autostart or clicked manually on btnStartListening
                if (chkAutoStart.Checked && btnStartListening.Enabled)
                    btnStartListening_Click(null, null);
            });
        }


        private void trkEffectTimeout_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }

        private void btnStop_Click(object? sender, EventArgs? e)
        {
            if (telemetry == null) return;

            timer1.Enabled = false;

            // Signal stop request
            telemetry.Abort();

            // Abort the players
            if (soundManager != null)
            {
                soundManager.StopEffects();
                soundManager = null;
                UpdateSoundEffectStatus(SoundEffectStatus.NotPlaying);
            }
            telemetry = null;
            motorControllers = null;
            arduinoEffects = null;



            // Reenable some controls
            ChangeStatus(txtSoundEffect1, true);
            ChangeStatus(txtSoundEffect2, true);
            ChangeStatus(txtJSON, true);
            ChangeStatus(btnSoundEffect1, true);
            ChangeStatus(btnSoundEffect2, true);
            ChangeStatus(btnJSONFile, true);
            ChangeStatus(txtListeningPort, true);
            TestRoutines(true);


            // Adjust valid operations
            ChangeStatus(btnStop, false);
            ChangeStatus(btnStartListening, true);


            // Update status
            toolStripStatusLabel1.Text = "Idle.";

        }

        private void ChangeStatus(Control control, bool newStatus)
        {
            control.Enabled = newStatus;
        }



        // Create a new thread and run the telemetry
        private void DoTelemetry()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            telemetry.Run();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            toolStripStatusLabel1.Text = "aborted";
        }

        private void PrepareControllers()
        {
            //        private MotorController[] motorControllers; // currently the interface is fixed to 2 (Arduino and TWatch) and each with 2 effects
            //        private EffectDefinition[] arduinoEffects;  // currently only two vibration motors connected
            //        private EffectDefinition TWatchEffects; // curently only 1 motor and 1 display connected

            //        See included the "Points for Effects - Diagram.png"  

            motorControllers = new MotorController[2]; // one for Arduino and the other for the TWatch

            // Define Arduino Effects First
            arduinoEffects = new EffectDefinition[2];

            // Speed brake data points
            MotorStrengthPoints points = new MotorStrengthPoints(1, (float)numMinIntensitySpeedBrakes.Value, 5, (float)numMinIntensitySpeedBrakes.Value, 6, (float)numMinIntensitySpeedBrakes.Value, 100, (float)numMaxIntensitySpeedBrakes.Value);
            arduinoEffects[0] = new EffectDefinition(VibrationEffectType.SpeedBrakes, points, chkVibrateMotorForSpeedBrake.Checked);

            // Flaps data points
            points = new MotorStrengthPoints(1, (float)numMinIntensitySpeedBrakes.Value, 5, (float)numMinIntensitySpeedBrakes.Value, 6, (float)numMinIntensitySpeedBrakes.Value, 100, (float)numMaxIntensitySpeedBrakes.Value);
            arduinoEffects[1] = new EffectDefinition(VibrationEffectType.Flaps, points, chkVibrateMotorForFlaps.Checked);

            // Now the first MotorController can be defined (Arduino)
            motorControllers[ARDUINO] = new MotorController("Arduino", txtArduinoIP.Text, Int32.Parse(txtArduinoPort.Text), arduinoEffects, (chkVibrateMotorForSpeedBrake.Checked || chkVibrateMotorForFlaps.Checked));

            // Now Define TWatch Effects
            TWatchEffects = new EffectDefinition[2];

            // Gear data points
            // The 15 and 16 doesnt matter too much, they will be replaced by the actual aircraft AoA ranges
            // For the TWatch the vibration strength doesn't matter too much because
            // currently I have only found how to turn them on or off, not how to control the strength
            // however this interface simplifies the creation of effect points
            //points = new MotorStrengthPoints(11, 100, 15, 100, 16, 100, 255, 255);
            points = new MotorStrengthPoints(1, 0, 98, 0, 99, 100, 100, 100);
            TWatchEffects[0] = new EffectDefinition(VibrationEffectType.Gear, points, chkTWatchVibrate.Checked);

            // AoA Background Color Display
            // Will show Yellow,Green or Red depending on AoA
            // The 15 and 16 doesnt matter too much, they will be replaced by the actual aircraft AoA ranges
            // For the AoA Background Color Display the vibration strength doesn't matter too much because
            // currently I have only found how to turn them on or off, not how to control the strength
            // however this interface simplifies the creation of effect points
            points = new MotorStrengthPoints(11, 100, 15, 100, 16, 100, 255, 255);
            TWatchEffects[1] = new EffectDefinition(VibrationEffectType.BackgroundAoA, points, chkTWatchDisplayBackground.Checked);

            // Now the second MotorController can be defined (TWatch)
            motorControllers[TWATCH] = new MotorController("TWatch-2020V3", txtTWatchIP.Text, Int32.Parse(txtTWatchPort.Text), TWatchEffects, (chkTWatchVibrate.Checked || chkTWatchDisplayBackground.Checked));

            // Start playing sound effects with volume 0, this minimizes any delay when the effectType is actually needed
            soundManager = new AoA_SoundManager(txtSoundEffect1.Text, txtSoundEffect2.Text, (float)trkVolumeMultiplier1.Value / 100.0f, (float)trkVolumeMultiplier2.Value / 100.0f, cmbAudioDevice1.SelectedIndex, chkPlayAlarm.Checked);
            soundManager.EnableEffect1 = chkEnableAoASoundEffects1.Checked;
            soundManager.EnableEffect2 = chkEnableAoASoundEffects2.Checked;
            UpdateSoundEffectStatus(soundManager.Status);
            soundManager.PlayAlarm = chkPlayAlarm.Checked;

            telemetry = new TelemetryServer(soundManager, motorControllers, chkShowStatistics.Checked, Int32.Parse(txtListeningPort.Text));
            telemetry.SetJSON(txtJSON.Text);
            telemetry.MinSpeed = (int)nudMinSpeed.Value;
            telemetry.MinAltitude = (int)nudMinAltitude.Value;
        }


        private void btnStartListening_Click(object sender, EventArgs e)
        {

            // Check if files exist
            if (!CheckFileExists(txtSoundEffect1, "Sound Effect 1")) return;
            if (!CheckFileExists(txtSoundEffect2, "Sound Effect 2")) return;
            if (!CheckFileExists(txtJSON, "JSON File")) return;


            // Check if the JSON file is valid
            string error = TelemetryServer.TestJSONFile(txtJSON.Text);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtJSON.Focus();
                return;
            }


            // Check if the user has selected a valid audio device
            if (cmbAudioDevice1.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a valid audio device first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbAudioDevice1.Focus();
                return;
            }

            // Sanitize port range using the ephemeral range
            if (Int32.TryParse(txtListeningPort.Text, out int value))
            {
                if (value < 49152) value = 49152;
                if (value > 65535) value = 65535;
                txtListeningPort.Text = value.ToString();
            }

            // Adjust valid operations
            ChangeStatus(btnStartListening, false);
            ChangeStatus(btnStop, true);
            toolStripStatusLabel1.Text = "Listening...";


            // Prepare SoundManager, TelemetryServer, MotorControllers
            PrepareControllers();



            // Reset max timers
            btnResetMax_Click(null, null);


            // Start a new thread to act as the UDP Server
            threadTelemetry = new Thread(DoTelemetry);
            threadTelemetry.Start();
            lblUDPServerThread.Text = "---";
            lblUIThreadID.Text = lblUDPServerThread.Text;
            lblTimestamp.Text = lblUDPServerThread.Text;
            lblUDPServerThread.Tag = -1;
            lblUIThreadID.Tag = -1;


            // Disable some controls
            ChangeStatus(txtSoundEffect1, false);
            ChangeStatus(txtSoundEffect2, false);
            ChangeStatus(txtJSON, false);
            ChangeStatus(btnSoundEffect1, false);
            ChangeStatus(btnSoundEffect2, false);
            ChangeStatus(btnJSONFile, false);
            ChangeStatus(txtListeningPort, false);
            TestRoutines(false);




            // Display stats if required
            if (chkChangeToMonitor.Checked) tabs.SelectTab(5);


            // Minimize if required
            if (chkAutoStart.Checked) this.WindowState = FormWindowState.Minimized;

            // Start monitoring in UI
            timer1.Enabled = true;
        }

        private bool UpdateValue<TControl, TValue>(TControl L, TValue value)
            where TControl : Control
        {
            if (!Equals(L.Tag, value))
            {
                L.Tag = value;
                L.Text = value switch
                {
                    float f => $"{f:F1}",
                    string s => s,
                    int s => s == int.MaxValue ? "N/A" : s.ToString(),
                    _ => value.ToString()
                };
                return true;
            }
            else
                return false;
        }

        private void SetNewIdealProcessor(uint maxProcNumber)
        {
            if (maxProcNumber <= 0)
            {
                toolStripStatusLabel1.Text = $"Invalid MaxProcessorNumber: {maxProcNumber}";
                return;
            }


            // My Intel 14700K has 8 performance cores and 12 efficiency cores.
            // CPU numbers 0-15 are performance
            // CPU numbers 16-27 are efficiency            
            uint newIdealProcessor = processorAssigner.GetNextProcessor();

            IntPtr currentThreadHandle = GetCurrentThread();
            int previousProcessor = (int)SetThreadIdealProcessor(currentThreadHandle, newIdealProcessor); // returns -1 if failed


            toolStripStatusLabel1.Text = $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] SetNewIdealProcessor({newIdealProcessor})={previousProcessor}";

        }


        private void UpdateMaxUIProcessingTime()
        {
            UpdateValue(lblProcessingTimeUIMax, Stats.Max()); // this might be delayed by one cycle, but it's ok
            UpdateValue(lblProcessingTimeUIMin, Stats.Min());
            UpdateValue(lblProcessingTimeUIAvg, Stats.Average());
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (needToCallSetNewIdealProcessor)
            {
                needToCallSetNewIdealProcessor = false;
                SetNewIdealProcessor(maxProcessorNumber); // This one also displays the new ideal processor
            }


            int processorUsedForUI = 0;
            bool showStatistics = chkShowStatistics.Checked;

            if (showStatistics)
            {
                stopWatchUI.Restart();
                processorUsedForUI = (int)GetCurrentProcessorNumber();
            }


            // check if we haven't received more telemetry so we should mute all effects            
            if ((soundManager.EffectsAreActive()) && ((Stopwatch.GetTimestamp() / Stopwatch.Frequency) - telemetry.LastSecond > trkEffectTimeout.Value))
            {
                soundManager.MuteEffects();
                UpdateSoundEffectStatus(SoundEffectStatus.Canceled);
                //Note: My arduino program and my TWatch programs, both include their own
                //logic to stop the vibration motor if they don't receive more telemetry
            }

            // Track Not Founds
            if (telemetry.UnitHaschanged && soundManager.AoA1 == 360)
                lstNotFounds.Items.Add(telemetry.CurrentUnitType);

            // Statistics are updated once per second
            if (showStatistics && telemetry.IsRunning() && (tabs.SelectedIndex == 5) && (this.WindowState != FormWindowState.Minimized))
            {
                this.SuspendLayout();

                // Report last datagram timestamp
                UpdateValue(lblTimestamp, telemetry.TimeStamp);

                // Report sound effectType
                UpdateSoundEffectStatus(soundManager.Status);

                // Report Current Unit Type
                UpdateValue(lblCurrentUnitType, telemetry.CurrentUnitType);

                // Report the last AoA received
                UpdateValue(lblLastAoA, telemetry.LastData.AoA);

                // Report datagrams per second
                UpdateValue(lblDatagramsPerSecond, telemetry.DPS);

                // Report speed brakes
                UpdateValue(lblLastSpeedBrakes, telemetry.LastData.SpeedBrakes);

                // Report flaps
                UpdateValue(lblLastFlaps, telemetry.LastData.Flaps);

                // Report speed
                UpdateValue(lblSpeed, telemetry.LastData.Speed);

                // Report G-Forces
                UpdateValue(lblGForces, telemetry.LastData.GForces);

                // Report Altitude Above Ground
                UpdateValue(lblAltitude, telemetry.LastData.Altitude);

                // Report gear
                UpdateValue(lblLastGear, telemetry.LastData.Gear);


                // Report last processor used for UDP processing
                UpdateValue(lblLastProcessorUsedUDP, telemetry.LastProcessorUsed);

                // Report last procesor used for UI (monitor) this function
                UpdateValue(lblLastProcessorUsedUI, processorUsedForUI);

                // Report UI ThreadID
                UpdateValue(lblUIThreadID, (int)GetCurrentThreadId());

                // Report UDP ThreadID
                UpdateValue(lblUDPServerThread, telemetry.ThreadId);

                // Always calculate/Report max UDP processing time
                UpdateValue(lblProcessingTimeUDPMax, telemetry.Stats.Max());
                UpdateValue(lblProcessingTimeUDPMin, telemetry.Stats.Min());
                UpdateValue(lblProcessingTimeUDPAvg, telemetry.Stats.Average());

                // Always calculate/Report max UI processing time (monitor).  This one needs to be the last                
                UpdateMaxUIProcessingTime();
                this.ResumeLayout();

                stopWatchUI.Stop();
                Stats.AddSample(stopWatchUI.Elapsed.Milliseconds);
            }

            timer1.Enabled = true;
        }

        private void AllowOnlyDigits(object sender, KeyPressEventArgs e)
        {
            // Only allow numbers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void txtListeningPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            AllowOnlyDigits(sender, e);
        }

        private void txtArduinoPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            AllowOnlyDigits(sender, e);
        }

        private void txtTWatchPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            AllowOnlyDigits(sender, e);
        }

        private void chkEnableAoASoundEffects1_CheckedChanged(object sender, EventArgs e)
        {
            if (soundManager != null) soundManager.EnableEffect1 = chkEnableAoASoundEffects1.Checked;
        }

        private void chkEnableAoASoundEffects2_CheckedChanged(object sender, EventArgs e)
        {
            if (soundManager != null) soundManager.EnableEffect2 = chkEnableAoASoundEffects2.Checked;
        }

        private void chkVibrateMotorForSpeedBrake_CheckedChanged(object sender, EventArgs e)
        {
            if (arduinoEffects != null)
                arduinoEffects[0].Enabled = chkVibrateMotorForSpeedBrake.Checked;

            if (motorControllers != null)
                motorControllers[ARDUINO].Enabled = (chkVibrateMotorForSpeedBrake.Checked || chkVibrateMotorForFlaps.Checked);
        }

        private void chkVibrateMotorForFlaps_CheckedChanged(object sender, EventArgs e)
        {
            if (arduinoEffects != null)
                arduinoEffects[1].Enabled = chkVibrateMotorForFlaps.Checked;

            if (motorControllers != null)
                motorControllers[ARDUINO].Enabled = (chkVibrateMotorForSpeedBrake.Checked || chkVibrateMotorForFlaps.Checked);
        }
        private void chkTWatchVibrate_CheckedChanged(object sender, EventArgs e)
        {
            if (TWatchEffects != null)
                TWatchEffects[0].Enabled = chkTWatchVibrate.Checked;
        }

        private void chkTWatchDisplayBackground_CheckedChanged(object sender, EventArgs e)
        {
            if (TWatchEffects != null)
                TWatchEffects[1].Enabled = chkTWatchDisplayBackground.Checked;
        }

        private void UpdateIntensity(NumericUpDown control, EffectDefinition[] effects, int index, float min, float max)
        {
            long now = Environment.TickCount / 100; // if you type too fast, you might need to decrease this value
            if ((long)control.Tag == now) return;  // don't do this twice (one for _ValueChanged and another one for _TextChanged)
            control.Tag = now; // update the timestamp of the last time we updated the value

            if (effects != null)
            {
                // Speed brake data points
                MotorStrengthPoints points = new MotorStrengthPoints(1, min, 5, min, 6, min, 100, max);
                effects[index].UpdatePoints(points);
            }
            Debug.Print(control.Value.ToString());
        }

        private void numMinIntensitySpeedBrakes_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 0, (float)nud.Value, (float)numMaxIntensitySpeedBrakes.Value);
            return;

        }

        private void numMaxIntensitySpeedBrakes_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 0, (float)numMinIntensitySpeedBrakes.Value, (float)nud.Value);
            return;

        }

        private void numMinIntensityFlaps_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 1, (float)nud.Value, (float)numMaxIntensityFlaps.Value);
            return;


        }

        private void numMaxIntensityFlaps_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 1, (float)numMinIntensityFlaps.Value, (float)nud.Value);
            return;


        }

        // Reset max timers, UI and UDP
        private void btnResetMax_Click(object? sender, EventArgs? e)
        {

            if (telemetry != null)
            {
                telemetry.Stats.Initialize();
                UpdateValue(lblProcessingTimeUDPMax, telemetry.Stats.Max());
            }

            Stats.Initialize();
            UpdateValue(lblProcessingTimeUIMax, Stats.Max());
        }

        private void nudMinSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (telemetry != null) telemetry.MinSpeed = (int)nudMinSpeed.Value;
        }

        public void SendUdpDatagram(string ipAddress, int destinationPort, byte[] data)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    udpClient.Connect(IPAddress.Parse(ipAddress), destinationPort);
                    udpClient.Send(data, data.Length);
                }
                catch (Exception ex)
                {
                    lblTestErrMsg.Text = ex.Message;
                }
            }
        }

        // Uses WaveOut Directly
        public void TestSound1(string filePath, int audioDeviceId)
        {
            lblTestErrMsg.Text = String.Empty;

            try
            {
                using (var audioFile = new AudioFileReader(filePath))
                using (var outputDevice = new WaveOutEvent() { DeviceNumber = audioDeviceId })
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                     Thread.Sleep(500); // busy waiting but only for this
                    }
                }
            }
            catch (Exception ex)
            {
                lblTestErrMsg.Text = ex.Message;
            }
        }

        private void TestSoundEffect1_Click(object sender, EventArgs e)
        {
            TestSound1(txtSoundEffect1.Text, cmbAudioDevice1.SelectedIndex);
        }

        private void TestSoundEffect2_Click(object sender, EventArgs e)
        {
            TestSound1(txtSoundEffect2.Text, cmbAudioDevice1.SelectedIndex);
        }

        private void btnTestArduinoMotors_Click(object sender, EventArgs e)
        {

            lblTestErrMsg.Text = String.Empty;

            byte[] strengthvibration = { 200, 0 };
            SendUdpDatagram(txtArduinoIP.Text, Convert.ToInt32(txtArduinoPort.Text), strengthvibration);
            Thread.Sleep(800);

            strengthvibration[0] = 0;
            strengthvibration[1] = 200;
            SendUdpDatagram(txtArduinoIP.Text, Convert.ToInt32(txtArduinoPort.Text), strengthvibration);
            Thread.Sleep(800);

            // Turn motors off quickly
            strengthvibration[0] = 0;
            strengthvibration[1] = 0;
            SendUdpDatagram(txtArduinoIP.Text, Convert.ToInt32(txtArduinoPort.Text), strengthvibration);

        }

        private void TestTWatchMotor_Click(object sender, EventArgs e)
        {
            lblTestErrMsg.Text = String.Empty;

            byte[] parameters = { 100, 0 }; // [0]-Motor Vibration, [1]-Screen Color
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters); // Vibrate and Turn Screen 1 => TFT_YELLOW
            Thread.Sleep(800);

            parameters[0] = 0; // turn off motor vibration
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters); // Vibrate and Turn Screen 1 => TFT_YELLOW
        }




        private void TestRoutines(bool Enabled)
        {
            btnTestTWatchDisplay.Enabled = Enabled;
            btnTestTWatchMotor.Enabled = Enabled;

            btnTestArduinoMotors.Enabled = Enabled;

            btnTestSoundEffect1.Enabled = Enabled;
            btnTestSoundEffect2.Enabled = Enabled;
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

        private void ProcessorCheck()
        {
            // LoadSettings must be called before ProcessorCheck()

            // Get the current process
            currentProcess = Process.GetCurrentProcess();

            // Assign only Efficiency cores if requested and CPU==12700K,14700K
            chkUseEfficiencyCoresOnly_CheckedChanged(null, null);


            // Assign background mode if requested
            if (chkUseBackgroundProcessing.Checked)
                SetPriorityClass(currentProcess.Handle, PROCESS_MODE_BACKGROUND_BEGIN);


            // Change the priority class to the previous setting selected (NORMAL, BELOW_NORMAL or IDLE)
            //cmbPriorityClass.SelectedIndex = Properties.Settings.Default.PriorityClassSelectedIndex;
            cmbPriorityClass_SelectedIndexChanged(null, null);

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


        private void nudMinAltitude_ValueChanged(object sender, EventArgs e)
        {
            if (telemetry != null) telemetry.MinAltitude = (int)nudMinAltitude.Value;
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

        private void chkPlayAlarm_CheckedChanged(object sender, EventArgs e)
        {
            if (soundManager != null) { soundManager.PlayAlarm = chkPlayAlarm.Checked; }

            if (!chkPlayAlarm.Enabled) return;

            if (chkPlayAlarm.Checked)
            {
                SoundTester2(Properties.Settings.Default.HalfAnHourAlarmSoundEffect, trkVolumeMultiplier1.Value / 100.0f);
            }

        }

        private void lstNotFounds_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstNotFounds.SelectedItem is null) return;

            Clipboard.SetText(lstNotFounds.SelectedItem.ToString());
        }

        private void chkReassignIdealProcessor_CheckedChanged(object sender, EventArgs e)
        {
            // processor cannot be assigned from the current thread
            // let's signal the need for that operation here
            needToCallSetNewIdealProcessor = chkReassignIdealProcessor.Visible && chkReassignIdealProcessor.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (processorAssigner == null) processorAssigner = new ProcessorAssigner(7);
            SetNewIdealProcessor(2);
        }

        //Uses Internal MediaPlayer
        private void SoundTester2(string fileName, float volume)
        {
            MediaPlayer soundEffect = new MediaPlayer(cmbAudioDevice1.SelectedIndex);
            soundEffect.Open(fileName);
            soundEffect.Volume = volume;
            soundEffect.Play();
        }

        private void btnTestSoundEffect3_Click(object sender, EventArgs e)
        {
            SoundTester2(Properties.Settings.Default.AircraftFoundSoundEffect, trkVolumeMultiplier1.Value / 100.0f);
        }

        private void btnTestSoundEffect4_Click(object sender, EventArgs e)
        {
            SoundTester2(Properties.Settings.Default.AircraftNotFoundSoundEffect, trkVolumeMultiplier1.Value / 100.0f);
        }

        private void btnTestSoundEffect5_Click(object sender, EventArgs e)
        {
            SoundTester2(Properties.Settings.Default.HalfAnHourAlarmSoundEffect, trkVolumeMultiplier1.Value / 100.0f);
        }

        private void btnTestTWatchDisplay_Click(object sender, EventArgs e)
        {
            lblTestErrMsg.Text = String.Empty;

            byte[] parameters = { 0, 1 }; // [0]-Motor Vibration, [1]-Screen Color
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters); // Vibrate and Turn Screen 1 => TFT_YELLOW
            Thread.Sleep(800);

            parameters[1] = 2; // Dark Green
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters);
            Thread.Sleep(800);

            parameters[1] = 3; // TFT_GREEN
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters);
            Thread.Sleep(800);

            parameters[1] = 4; // TFT_RED
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters);
            Thread.Sleep(800);

            parameters[1] = 0; // TFT_BLACK
            SendUdpDatagram(txtTWatchIP.Text, Convert.ToInt32(txtTWatchPort.Text), parameters);

        }
    }
}
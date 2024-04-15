using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace TelemetryVibShaker
{
    public partial class frmMain : Form
    {
        private const int ARDUINO = 0;
        private const int TWATCH = 1;

        private MotorController[] motorControllers; // currently the interface is fixed to 2 (Arduino and TWatch) and each with 2 effects
        private EffectDefinition[] arduinoEffects;  // currently only two vibration motors connected
        private EffectDefinition[] TWatchEffects; // curently only 1 motor and 1 display connected
        private AoA_SoundManager soundManager;  // manages sound effects according to the current AoA
        private TelemetryServer telemetry;      // controls all telemetry logic
        private Thread threadTelemetry; // this thread runs the telemetry
        private long lastSecond; // second of the last udp datagram processed
        private int maxUIProcessingTime;  // milliseconds elapsed in processing the monitor-update-cycle in Timer1
        private Stopwatch stopWatchUI;  // used to measure processing time in Timer1 (updating the UI)

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();


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
            updateSelectedFile(txtSoundEffect1, "Select an NWAVE compatible audio file", (string)btnSoundEffect1.Tag);
        }

        private void btnSoundEffect2_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtSoundEffect2, "Select an NWAVE compatible audio file", (string)btnSoundEffect2.Tag);
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
            DialogResult jsonSelection = updateSelectedFile(txtJSON, "Select an JSON file defining AoA for each aircraft", (string)btnJSONFile.Tag);
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

        private void fillAudioDevices()
        {
            // create a device enumerator
            var enumerator = new MMDeviceEnumerator();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];

                cmbAudioDevice1.Items.Add(device.FriendlyName);
            }

            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var capabilities = WaveOut.GetCapabilities(n);
                comboBox1.Items.Add($"{n}: {capabilities.ProductName}");
            }
        }



        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the telemetry if it is still running: kill the upd server, etc
            btnStop_Click(null, null);

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
            // Initialize max timers
            btnResetMax_Click(null, null);

            // Flag to skip the initial max UI processing time
            lblMaxProcessingTimeTitle.Tag = false;

            stopWatchUI = new Stopwatch();

            // The TextChanged event is not exposed in the Designer:
            numMinIntensitySpeedBrakes.TextChanged += numMinIntensitySpeedBrakes_ValueChanged;
            numMaxIntensitySpeedBrakes.TextChanged += numMaxIntensitySpeedBrakes_ValueChanged;
            numMinIntensityFlaps.TextChanged += numMinIntensityFlaps_ValueChanged;
            numMaxIntensityFlaps.TextChanged += numMaxIntensityFlaps_ValueChanged;

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


            fillAudioDevices();

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


            btnStop.Tag = false;
            lblSoundStatus.Tag = SoundEffectStatus.Invalid;
            UpdateSoundEffectStatus(SoundEffectStatus.NotPlaying);




        }

        private void trkEffectTimeout_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (telemetry == null) return;

            timer1.Enabled = false;

            // Signal stop request
            telemetry.Abort();

            // Abort the players
            if (soundManager != null)
            {
                soundManager.Stop();
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
            telemetry.Run();
            toolStripStatusLabel1.Text = "aborted";
        }

        private void PrepareControllers()
        {
            //        private MotorController[] motorControllers; // currently the interface is fixed to 2 (Arduino and TWatch) and each with 2 effects
            //        private EffectDefinition[] arduinoEffects;  // currently only two vibration motors connected
            //        private EffectDefinition TWatchEffects; // curently only 1 motor and 1 display connected

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

            // AoA data points
            // The 15 and 16 doesnt matter too much, they will be replaced by the actual aircraft AoA ranges
            // For the TWatch the vibration strength doesn't matter too much because
            // currently I have only found how to turn them on or off, not how to control the strength
            // however this interface simplifies the creation of effect points
            //points = new MotorStrengthPoints(11, 100, 15, 100, 16, 100, 255, 255);
            points = new MotorStrengthPoints(11, 0, 15, 0, 16, 100, 255, 100);
            TWatchEffects[0] = new EffectDefinition(VibrationEffectType.AoA, points, chkTWatchVibrate.Checked);

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
            soundManager = new AoA_SoundManager(txtSoundEffect1.Text, txtSoundEffect2.Text, (float)trkVolumeMultiplier1.Value / 100.0f, (float)trkVolumeMultiplier2.Value / 100.0f, cmbAudioDevice1.SelectedIndex);
            soundManager.EnableEffect1 = chkEnableAoASoundEffects1.Checked;
            soundManager.EnableEffect2 = chkEnableAoASoundEffects2.Checked;
            UpdateSoundEffectStatus(soundManager.Status);

            telemetry = new TelemetryServer(soundManager, motorControllers, chkShowStatistics.Checked, Int32.Parse(txtListeningPort.Text));
            telemetry.SetJSON(txtJSON.Text);
            telemetry.MinSpeed = (int)nudMinSpeed.Value;
        }


        private void btnStartListening_Click(object sender, EventArgs e)
        {
            // Display stats if required
            if (chkChangeToMonitor.Checked) tabs.SelectTab(4);

            lastSecond = 0; // reset tracker

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
            if (cmbAudioDevice1.SelectedIndex == -1)
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

            // Prepare SoundManager, TelemetryServer, MotorControllers
            PrepareControllers();



            // Reset max timers
            btnResetMax_Click(null, null);


            // Start a new thread to act as the UDP Server
            threadTelemetry = new Thread(DoTelemetry);
            threadTelemetry.Start();
            lblUDPServerThread.Text = "---";
            lblUIThreadID.Text = lblUDPServerThread.Text;
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


            // Adjust valid operations
            ChangeStatus(btnStartListening, false);
            ChangeStatus(btnStop, true);
            toolStripStatusLabel1.Text = "Listening...";

            timer1.Enabled = true;

        }

        private void UpdateValue(Label L, int value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateMaxUIProcessingTime()
        {
            UpdateValue(lblProcessingTimeUI, maxUIProcessingTime); // this might be delayed by one cycle, but it's ok

            stopWatchUI.Stop();
            int elapsed = stopWatchUI.Elapsed.Milliseconds;

            // discard the first UI processing
            if ((bool)lblMaxProcessingTimeTitle.Tag)
            {
                if (maxUIProcessingTime < elapsed)
                    maxUIProcessingTime = elapsed;
            }
            else
                lblMaxProcessingTimeTitle.Tag = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            stopWatchUI.Restart();
            int processorUsedForUI = (int)GetCurrentProcessorNumber();


            if (telemetry != null) lastSecond = telemetry.LastSecond;

            // check if we haven't received more telemetry so we should mute all effects
            if ((soundManager.SoundIsActive()) && (Environment.TickCount64 / 1000 - lastSecond > trkEffectTimeout.Value))
            {
                soundManager.MuteEffects();
                UpdateSoundEffectStatus(SoundEffectStatus.Canceled);
                //Note: My arduino program and my TWatch programs, both include their own
                //logic to stop the vibration motor if they don't receive more telemetry
            }

            // Statistics are updated once per second
            if (chkShowStatistics.Checked && telemetry.IsRunning() && tabs.SelectedIndex == 4)
            {
                // Report sound effectType
                UpdateSoundEffectStatus(soundManager.Status);

                if (telemetry.LastData.AoA < 0) // new aircraft type?
                {
                    // Report unit type
                    //if (!telemetry.CurrentUnitType.Equals(lblCurrentUnitType.Tag))
                    //{
                        lblCurrentUnitType.Tag = telemetry.CurrentUnitType;
                        lblCurrentUnitType.Text = telemetry.CurrentUnitType + " (" + soundManager.AoA1.ToString() + ", " + soundManager.AoA2.ToString() + ")";
                    //}

                    // Report max UDP processing time, it should have been reseted to zero
                    //UpdateValue(lblProcessingTimeUDP, telemetry.MaxProcessingTime);

                    // UI Reporting takes so little time when there are no updates, let's continue and report
                    //return; // if no value has been received yet, don't update nothing else
                }

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

                // Report max UDP processing time
                UpdateValue(lblProcessingTimeUDP, telemetry.MaxProcessingTime);

                // Report last processor used for UDP processing
                UpdateValue(lblLastProcessorUsedUDP, telemetry.LastProcessorUsed);

                // Report last procesor used for UI (monitor) this function
                UpdateValue(lblLastProcessorUsedUI, processorUsedForUI);

                // Report UI ThreadID
                UpdateValue(lblUIThreadID, (int)GetCurrentThreadId());

                // Report UDP ThreadID
                UpdateValue(lblUDPServerThread, telemetry.ThreadId);

                // Report max UI processing time (monitor)
                UpdateMaxUIProcessingTime(); // the stopwatch is stopped here

            }

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
            long now = Environment.TickCount64 / 100; // if you type too fast, you might need to decrease this value
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

            long now = Environment.TickCount64 / 100;
            if ((long)numMinIntensitySpeedBrakes.Tag == now) return;  // don't do this twice (one for _ValueChanged and another one for _TextChanged)
            numMinIntensitySpeedBrakes.Tag = now;

            if (arduinoEffects != null)
            {
                // Speed brake data points
                MotorStrengthPoints points = new MotorStrengthPoints(1, (float)numMinIntensitySpeedBrakes.Value, 5, (float)numMinIntensitySpeedBrakes.Value, 6, (float)numMinIntensitySpeedBrakes.Value, 100, (float)numMaxIntensitySpeedBrakes.Value);
                arduinoEffects[0].UpdatePoints(points);
            }
            Debug.Print(numMinIntensitySpeedBrakes.Value.ToString());
        }

        private void numMaxIntensitySpeedBrakes_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 0, (float)numMinIntensitySpeedBrakes.Value, (float)nud.Value);
            return;

            long now = Environment.TickCount64 / 500;
            if ((long)numMaxIntensitySpeedBrakes.Tag == now) return;  // don't do this twice (one for _ValueChanged and another one for _TextChanged)
            numMaxIntensitySpeedBrakes.Tag = now;

            if (arduinoEffects != null)
            {
                // Speed brake data points
                MotorStrengthPoints points = new MotorStrengthPoints(1, (float)numMinIntensitySpeedBrakes.Value, 5, (float)numMinIntensitySpeedBrakes.Value, 6, (float)numMinIntensitySpeedBrakes.Value, 100, (float)numMaxIntensitySpeedBrakes.Value);
                arduinoEffects[0].UpdatePoints(points);
            }
        }

        private void numMinIntensityFlaps_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 1, (float)nud.Value, (float)numMaxIntensityFlaps.Value);
            return;


            long now = Environment.TickCount64 / 500;
            if ((long)numMinIntensityFlaps.Tag == now) return;  // don't do this twice (one for _ValueChanged and another one for _TextChanged)
            numMinIntensityFlaps.Tag = now;

            if (arduinoEffects != null)
            {
                // Flaps data points
                MotorStrengthPoints points = new MotorStrengthPoints(1, (float)numMinIntensityFlaps.Value, 5, (float)numMinIntensityFlaps.Value, 6, (float)numMinIntensityFlaps.Value, 100, (float)numMaxIntensityFlaps.Value);
                arduinoEffects[1].UpdatePoints(points);
            }
        }

        private void numMaxIntensityFlaps_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            UpdateIntensity((NumericUpDown)nud, arduinoEffects, 1, (float)numMinIntensityFlaps.Value, (float)nud.Value);
            return;

            long now = Environment.TickCount64 / 500;
            if ((long)numMaxIntensityFlaps.Tag == now) return;  // don't do this twice (one for _ValueChanged and another one for _TextChanged)
            numMaxIntensityFlaps.Tag = now;

            if (arduinoEffects != null)
            {
                // Flaps data points
                MotorStrengthPoints points = new MotorStrengthPoints(1, (float)numMinIntensityFlaps.Value, 5, (float)numMinIntensityFlaps.Value, 6, (float)numMinIntensityFlaps.Value, 100, (float)numMaxIntensityFlaps.Value);
                arduinoEffects[1].UpdatePoints(points);
            }
        }

        // Reset max timers, UI and UDP
        private void btnResetMax_Click(object sender, EventArgs e)
        {
            lblProcessingTimeUDP.Tag = null;
            lblProcessingTimeUI.Tag = null;

            if (telemetry != null)
            {
                telemetry.MaxProcessingTime = -1;
                UpdateValue(lblProcessingTimeUDP, telemetry.MaxProcessingTime);
            }

            maxUIProcessingTime = -1;
            UpdateValue(lblProcessingTimeUI, maxUIProcessingTime);
        }

        private void nudMinSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (telemetry != null) telemetry.MinSpeed = (int)nudMinSpeed.Value;
        }
    }
}
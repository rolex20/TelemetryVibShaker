using NAudio.CoreAudioApi;
using NAudio.Wave;
//using System.Net.Sockets;
//using System.Diagnostics;
using System.Windows.Forms;
using System.Net;
using System.Text;


namespace TelemetryVibShaker
{
    public partial class frmMain : Form
    {
        private AoA_SoundManager soundManager;  // manages sound effects according to the current AoA
        private TelemetryServer telemetry;      // controls all telemetry logic
        private Thread threadTelemetry; // this thread runs the telemetry
        private long lastSecond; // second of the last udp datagram processed



        public frmMain()
        {
            InitializeComponent();
        }


        private bool CheckFileExists(TextBox tb)
        {
            bool exist = File.Exists(tb.Text);
            if (!exist)
            {
                MessageBox.Show("The file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (string.IsNullOrEmpty(error))
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

        // Only update status if, UI is not up to date.
        private void UpdateSoundEffectStatus(SoundEffectStatus newStatus)
        {

            SoundEffectStatus currentStatus = (SoundEffectStatus)lblSoundStatus.Tag;

            if (currentStatus != newStatus)
            {
                lblSoundStatus.Tag = currentStatus;
                switch (newStatus)
                {
                    case SoundEffectStatus.NotPlaying:
                        lblSoundStatus.Text = "Not playing sounds.";
                        break;
                    case SoundEffectStatus.Ready:
                        lblSoundStatus.Text = "Sound effects ready.";
                        break;
                    case SoundEffectStatus.Playing:
                        lblSoundStatus.Text = "Playing effect...";
                        break;
                    case SoundEffectStatus.Canceled:
                        lblSoundStatus.Text = "Alarm canceled.";
                        break;
                }
            }

        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            fillAudioDevices();

            // Load the settings for all controls in the form
            LoadSettings(this);

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);

            updateVolumeMultiplier(lblVolumeMultiplier1, trkVolumeMultiplier1);
            updateVolumeMultiplier(lblVolumeMultiplier2, trkVolumeMultiplier2);
            updateEffectsTimeout();

            lblCurrentUnitType.Tag = string.Empty;

            lblLastAoA.Text = string.Empty;
            lblLastAoA.Tag = (float)0.0;

            lblDatagramsPerSecond.Text = string.Empty;
            lblProcessingTime.Text = string.Empty;
            lblServerThread.Text = string.Empty;


            btnStop.Tag = false;
            lblSoundStatus.Tag = SoundEffectStatus.Invalid;
            UpdateSoundEffectStatus(SoundEffectStatus.NotPlaying);

            // Make it easier for the additional UDP listenerUdp thread to update the UI
            //Label.CheckForIllegalCrossThreadCalls = false;
            //TextBox.CheckForIllegalCrossThreadCalls = false;

            soundManager = null;
            telemetry = null;

        }

        private void trkEffectTimeout_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (telemetry == null) return;


            // Signal stop request
            telemetry.Stop();

            // Stop the player
            if (soundManager != null)
            {
                soundManager.Stop();
                soundManager = null;
                UpdateSoundEffectStatus(SoundEffectStatus.NotPlaying);
            }


            // Stop the UDP listenerUdp
            telemetry.Stop();


            // Reenable some controls
            ChangeStatus(txtSoundEffect1, true);
            ChangeStatus(txtSoundEffect2, true);
            ChangeStatus(txtJSON, true);
            ChangeStatus(btnSoundEffect1, true);
            ChangeStatus(btnSoundEffect2, true);
            ChangeStatus(btnJSONFile, true);
            ChangeStatus(txtListeningPort, true);


            // Adjust valid operations
            timer1.Enabled = false;
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
        private void UDPServer()
        {

        }


        private void btnStartListening_Click(object sender, EventArgs e)
        {
            lastSecond = 0; // reset tracker

            // Check if files exist
            if (!CheckFileExists(txtSoundEffect1)) return;
            if (!CheckFileExists(txtSoundEffect2)) return;
            if (!CheckFileExists(txtJSON)) return;



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

            // Start playing sound effects with volume 0, this minimizes any delay when the effect is actually needed
            soundManager = new AoA_SoundManager(txtSoundEffect1.Text, txtSoundEffect2.Text, (float)trkVolumeMultiplier1.Value / 100.0f, (float)trkVolumeMultiplier2.Value / 100.0f, cmbAudioDevice1.SelectedIndex);
            UpdateSoundEffectStatus(SoundEffectStatus.Ready);


            // Sanitize port range using the ephemeral range
            if (Int32.TryParse(txtListeningPort.Text, out int value))
            {
                if (value < 49152) value = 49152;
                if (value > 65535) value = 65535;
                txtListeningPort.Text = value.ToString();
            }



            lblProcessingTime.Tag = 0;  // Reset max processing time tracker
            // Start a new thread to act as the UDP Server
            threadTelemetry = new Thread(UDPServer);
            threadTelemetry.Start();
            lblServerThread.Text = threadTelemetry.ManagedThreadId.ToString();

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

            // Display stats if required
            if (chkChangeToMonitor.Checked) tabs.SelectTab(4);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (telemetry != null) lastSecond = telemetry.lastSecond;
            if ((soundManager.SoundIsActive()) && (Environment.TickCount64 / 1000 - lastSecond > trkEffectTimeout.Value))
            {
                soundManager.MuteEffects();
                UpdateSoundEffectStatus(SoundEffectStatus.Canceled);
            }

            // Statistics are updated once per second
            if (chkShowStatistics.Checked && telemetry.IsRunning())
            {
                // Report the last AoA received
                if ((int)lblLastAoA.Tag != telemetry.lastAoA)
                {
                    lblLastAoA.Tag = telemetry.lastAoA;
                    lblLastAoA.Text = telemetry.lastAoA.ToString() + "°";
                }

                // Report datagrams per second
                if ((int)lblDatagramsPerSecond.Tag != telemetry.dps)
                {
                    lblDatagramsPerSecond.Tag = telemetry.dps;
                    lblDatagramsPerSecond.Text = telemetry.dps.ToString();
                }

                // Report sound effect
                UpdateSoundEffectStatus(telemetry.SoundEffectStatus);

                // Report unit type
                if (!telemetry.CurrentUnitType.Equals(lblCurrentUnitType))
                {
                    lblCurrentUnitType.Text = telemetry.CurrentUnitType;
                }
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
    }
}



using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Net.Sockets;
using TelemetryVibShaker.Properties;
using System.Text.Json;
using System.Diagnostics;

namespace TelemetryVibShaker
{
    public partial class frmMain : Form
    {
        private UdpClient listener;
        private long lastSecond; // second of the last received datagram
        private float lastAoA;  // last AoA correctly parsed
        private int dps; // datagrams received per second

        private Root JSONroot;  // json root object
        private String currentUnitType;  // current type of aircraft used by the player

        private AoA_SoundManager soundManager; // manages sound effects according to the current AoA

        enum EffectStatus
        {
            Invalid,
            NotPlayingEffect,
            SoundEffectsReady,
            PlayingEffect, // 1 or 2
            EffectCanceled, // 1 and 2
        }

        // To measure processing time per datagram
        Stopwatch stopwatch;



        public frmMain()
        {
            InitializeComponent();
        }


        private void btnStartListening_Click(object sender, EventArgs e)
        {
            // Check if files exist
            if (!CheckFileExists(txtSoundEffect1)) return;
            if (!CheckFileExists(txtSoundEffect2)) return;
            if (!CheckFileExists(txtJSON)) return;

            // Display stats if required
            if (chkChangeToMonitor.Checked) tabs.SelectTab(3);
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
            updateSelectedFile(txtSoundEffect1, "Select an NWAVE compatible audio file", (String)btnSoundEffect1.Tag);
        }

        private void btnSoundEffect2_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtSoundEffect2, "Select an NWAVE compatible audio file", (String)btnSoundEffect2.Tag);
        }

        private void updateSelectedFile(TextBox tb, String title, String filter)
        {
            openFileDialog1.Filter = filter;
            openFileDialog1.FileName = tb.Text;
            openFileDialog1.Title = title;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                tb.Text = openFileDialog1.FileName;
        }


        private void updateEffectsTimeout()
        {
            lblEffectTimeout.Text = trkEffectTimeout.Value.ToString() + " second(s)";
            lblEffectTimeout.Tag = trkEffectTimeout.Value;
        }

        private void btnJSONFile_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtJSON, "Select an JSON file defining AoA for each aircraft", (String)btnJSONFile.Tag);
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
        private void UpdateSoundEffectStatus(EffectStatus newStatus)
        {
            EffectStatus currentStatus = (EffectStatus)lblSoundStatus.Tag;


            if (currentStatus != newStatus)
            {
                lblSoundStatus.Tag = currentStatus;
                switch (newStatus)
                {
                    case EffectStatus.NotPlayingEffect:
                        lblSoundStatus.Text = "Not playing sounds.";
                        break;
                    case EffectStatus.SoundEffectsReady:
                        lblSoundStatus.Text = "Sound effects ready.";
                        break;
                    case EffectStatus.PlayingEffect:
                        lblSoundStatus.Text = "Playing effect...";
                        break;
                    case EffectStatus.EffectCanceled:
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

            lblCurrentUnitType.Tag = "";

            lblLastAoA.Text = "";
            lblLastAoA.Tag = (float)0.0;

            lblDatagramsPerSecond.Text = "";
            lblProcessingTime.Text = "";

            stopwatch = new Stopwatch();

            lblSoundStatus.Tag = EffectStatus.Invalid;
            UpdateSoundEffectStatus(EffectStatus.NotPlayingEffect);

            // Make it easier for the additional UDP listener thread to update the UI
            Label.CheckForIllegalCrossThreadCalls = false;
            TextBox.CheckForIllegalCrossThreadCalls = false;

        }

        private void trkEffectTimeout_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }
    }
}
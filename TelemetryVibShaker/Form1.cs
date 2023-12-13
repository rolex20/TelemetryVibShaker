


using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TelemetryVibShaker
{
    public partial class frmMain : Form
    {



        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            fillAudioDevices();

            // Load the settings for all controls in the form
            LoadSettings(this);

            // Restore previous settings
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
/*
            cmbAudioDevice1.SelectedIndex = Properties.Settings.Default.cmbAudioDevice1;
            chkEnableAoASoundEffects1.Checked = Properties.Settings.Default.chkEnableAoASoundEffects1;
            chkEnableAoASoundEffects2.Checked = Properties.Settings.Default.chkEnableAoASoundEffects2;
            txtSoundEffect1.Text = Properties.Settings.Default.txtSoundEffect1;
            txtSoundEffect2.Text = Properties.Settings.Default.txtSoundEffect2;
            trkVolumeMultiplier1.Value = Properties.Settings.Default.trkVolumeMultiplier1;
            trkVolumeMultiplier2.Value= Properties.Settings.Default.trkVolumeMultiplier2;
*/


            updateVolumeMultiplier(lblVolumeMultiplier1, trkVolumeMultiplier1);
            updateVolumeMultiplier(lblVolumeMultiplier2, trkVolumeMultiplier2);
            updateEffectsTimeout();
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

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }

        private void updateEffectsTimeout()
        {
            lblEffectTimeout.Text = trkEffectTimeout.Value.ToString() + " second(s)";
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
        }

        private void trkVolumeMultiplier1_Scroll(object sender, EventArgs e)
        {
            updateVolumeMultiplier(lblVolumeMultiplier1, trkVolumeMultiplier1);
        }

        private void trkVolumeMultiplier2_Scroll(object sender, EventArgs e)
        {
            updateVolumeMultiplier(lblVolumeMultiplier2, trkVolumeMultiplier2);
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
/*
            // Save current settings
            Properties.Settings.Default.XCoordinate = this.Location.X;
            Properties.Settings.Default.YCoordinate = this.Location.Y;
            Properties.Settings.Default.cmbAudioDevice1 = cmbAudioDevice1.SelectedIndex;
            Properties.Settings.Default.cmbAudioDevice1 = cmbAudioDevice1.SelectedIndex;
            Properties.Settings.Default.chkEnableAoASoundEffects1 = chkEnableAoASoundEffects1.Checked;
            Properties.Settings.Default.chkEnableAoASoundEffects2 = chkEnableAoASoundEffects2.Checked;
            Properties.Settings.Default.txtSoundEffect1 = txtSoundEffect1.Text;
            Properties.Settings.Default.txtSoundEffect2 = txtSoundEffect2.Text;
            Properties.Settings.Default.trkVolumeMultiplier1 = trkVolumeMultiplier1.Value;
            Properties.Settings.Default.trkVolumeMultiplier2 = trkVolumeMultiplier2.Value;
*/
            // Save the settings for all controls in the form
            SaveSettings(this);

            Properties.Settings.Default.Save();
        }


        private object GetPropertySetting(string propertyName)
        {
            object result = null;
            try
            {
                result = Properties.Settings.Default[propertyName];
            }
            catch (Exception ex)
            {
                // continue
            }

            return result;
        }

        // This method checks if a property exists, and if not, creates it
        private void CheckAndCreateProperty(string propertyName, Type propertyType, object defaultValue, string propertyTemplate)
        {
            // Get the existing properties collection
            var properties = Properties.Settings.Default.Properties;

            // If the property does not exist
            object settingValue = GetPropertySetting(propertyName);
            if (settingValue == null)
            {
                object existingProperty = Properties.Settings.Default[propertyTemplate];
                var newProperty = new System.Configuration.SettingsProperty((System.Configuration.SettingsProperty)existingProperty);
                newProperty.Name = propertyName;
                newProperty.DefaultValue = defaultValue;
/*
                // Get an existing property as a template
                var existingProperty = properties[propertyTemplate];

                // Create a new property with the same provider and attributes
                var newProperty = new System.Configuration.SettingsProperty(
                    propertyName, // name of the new property
                    propertyType, // type of the new property
                    existingProperty.Provider, // provider of the new property
                    false, // is the new property read-only?
                    defaultValue, // default value of the new property
                    System.Configuration.SettingsSerializeAs.String, // serialization mode of the new property
                    existingProperty.Attributes, // attributes of the new property
                    false, // is the new property inherited from the application settings?
                    false // is the new property thrown away when the user upgrades the application?
                );
*/
                // Add the new property to the properties collection
                properties.Add(newProperty);
                Properties.Settings.Default.Save();
            }
        }


        // This method loads the setting value for a given control
        private void LoadSetting(Control control)
        {
            // Get the name of the control
            string controlName = control.Name;

            // If the control name is not null or empty
            if (!string.IsNullOrEmpty(controlName))
            {
                // Get the control's property value according to its type
                MyControlInfo controlInfo = new MyControlInfo(control);


                if (controlInfo.ControlValue == null) return;  // Return if I am not interested in loading settings for this control

                // Check and create the property if it does not exist
                CheckAndCreateProperty(controlName, controlInfo.ControlType, controlInfo.ControlValue, controlInfo.PropertyTemplate);


                // Get the setting value from the Properties.Settings.Default
                object settingValue = GetPropertySetting(controlName);//  Properties.Settings.Default[controlName];
                // If the setting value is not null
                if (settingValue != null)
                {
                    // Set the control's property according to its type
                    if (control is TextBox)
                    {
                        (control as TextBox).Text = settingValue.ToString();
                    }
                    else if (control is CheckBox)
                    {
                        (control as CheckBox).Checked = (bool)settingValue;
                    }
                    else if (control is ComboBox)
                    {
                        (control as ComboBox).SelectedIndex = (int)settingValue;
                    }
                    // You can add more cases for other types of controls
                }
            }
        }


        // This method saves the setting value for a given control
        private void SaveSetting(Control control)
        {
            // Get the name of the control
            string controlName = control.Name;
            // If the control name is not null or empty
            if (!string.IsNullOrEmpty(controlName))
            {

                MyControlInfo controlInfo = new MyControlInfo(control);

                // Get the control's property value according to its type
                //object controlValue = RestorableSetting(control);

                // If the control value is not null
                if (controlInfo.ControlValue != null)
                {
                    // Check and create the property if it does not exist
                    CheckAndCreateProperty(controlName, controlInfo.ControlType, controlInfo.ControlValue, controlInfo.PropertyTemplate);

                    // Set the setting value in the Properties.Settings.Default
                    Properties.Settings.Default[controlName] = controlInfo.ControlValue;
                }
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

    }
}
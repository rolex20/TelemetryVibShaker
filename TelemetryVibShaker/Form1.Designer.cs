namespace TelemetryVibShaker
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            tabs = new TabControl();
            tabNormalSoundEffects = new TabPage();
            cmbAudioDevice1 = new ComboBox();
            label33 = new Label();
            chkEnableAoASoundEffects2 = new CheckBox();
            lblVolumeMultiplier2 = new Label();
            trkVolumeMultiplier2 = new TrackBar();
            label34 = new Label();
            label4 = new Label();
            chkEnableAoASoundEffects1 = new CheckBox();
            btnSoundEffect2 = new Button();
            btnSoundEffect1 = new Button();
            lblVolumeMultiplier1 = new Label();
            trkVolumeMultiplier1 = new TrackBar();
            label3 = new Label();
            txtSoundEffect2 = new TextBox();
            label2 = new Label();
            txtSoundEffect1 = new TextBox();
            label1 = new Label();
            tabArduino = new TabPage();
            txtArduinoPort = new TextBox();
            label11 = new Label();
            txtArduinoIP = new TextBox();
            label10 = new Label();
            numMaxIntensityFlaps = new NumericUpDown();
            numMinIntensityFlaps = new NumericUpDown();
            label8 = new Label();
            label9 = new Label();
            chkVibrateMotorForFlaps = new CheckBox();
            numMaxIntensitySpeedBrakes = new NumericUpDown();
            numMinIntensitySpeedBrakes = new NumericUpDown();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            chkVibrateMotorForSpeedBrake = new CheckBox();
            tabTTGO20V3 = new TabPage();
            chkTWatchDisplayBackground = new CheckBox();
            chkTWatchVibrate = new CheckBox();
            txtTWatchPort = new TextBox();
            label13 = new Label();
            txtTWatchIP = new TextBox();
            label14 = new Label();
            label12 = new Label();
            tabSettings = new TabPage();
            btnJSONFile = new Button();
            txtJSON = new TextBox();
            label17 = new Label();
            lblEffectTimeout = new Label();
            trkEffectTimeout = new TrackBar();
            label16 = new Label();
            txtListeningPort = new TextBox();
            label15 = new Label();
            tabMonitor = new TabPage();
            panel1 = new Panel();
            label31 = new Label();
            label32 = new Label();
            lblDatagramsPerSecond = new Label();
            label29 = new Label();
            lblProcessingTime = new Label();
            label27 = new Label();
            label26 = new Label();
            lblCurrentUnitType = new Label();
            lblSoundStatus = new Label();
            label19 = new Label();
            lblLastAoA = new Label();
            label24 = new Label();
            label21 = new Label();
            label25 = new Label();
            label23 = new Label();
            chkShowStatistics = new CheckBox();
            chkChangeToMonitor = new CheckBox();
            btnStartListening = new Button();
            btnStop = new Button();
            openFileDialog1 = new OpenFileDialog();
            toolTip1 = new ToolTip(components);
            timer1 = new System.Windows.Forms.Timer(components);
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            tabs.SuspendLayout();
            tabNormalSoundEffects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier1).BeginInit();
            tabArduino.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxIntensityFlaps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinIntensityFlaps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxIntensitySpeedBrakes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinIntensitySpeedBrakes).BeginInit();
            tabTTGO20V3.SuspendLayout();
            tabSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkEffectTimeout).BeginInit();
            tabMonitor.SuspendLayout();
            panel1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(tabNormalSoundEffects);
            tabs.Controls.Add(tabArduino);
            tabs.Controls.Add(tabTTGO20V3);
            tabs.Controls.Add(tabSettings);
            tabs.Controls.Add(tabMonitor);
            tabs.Location = new Point(24, 22);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(517, 429);
            tabs.TabIndex = 0;
            // 
            // tabNormalSoundEffects
            // 
            tabNormalSoundEffects.Controls.Add(cmbAudioDevice1);
            tabNormalSoundEffects.Controls.Add(label33);
            tabNormalSoundEffects.Controls.Add(chkEnableAoASoundEffects2);
            tabNormalSoundEffects.Controls.Add(lblVolumeMultiplier2);
            tabNormalSoundEffects.Controls.Add(trkVolumeMultiplier2);
            tabNormalSoundEffects.Controls.Add(label34);
            tabNormalSoundEffects.Controls.Add(label4);
            tabNormalSoundEffects.Controls.Add(chkEnableAoASoundEffects1);
            tabNormalSoundEffects.Controls.Add(btnSoundEffect2);
            tabNormalSoundEffects.Controls.Add(btnSoundEffect1);
            tabNormalSoundEffects.Controls.Add(lblVolumeMultiplier1);
            tabNormalSoundEffects.Controls.Add(trkVolumeMultiplier1);
            tabNormalSoundEffects.Controls.Add(label3);
            tabNormalSoundEffects.Controls.Add(txtSoundEffect2);
            tabNormalSoundEffects.Controls.Add(label2);
            tabNormalSoundEffects.Controls.Add(txtSoundEffect1);
            tabNormalSoundEffects.Controls.Add(label1);
            tabNormalSoundEffects.Location = new Point(4, 24);
            tabNormalSoundEffects.Name = "tabNormalSoundEffects";
            tabNormalSoundEffects.Padding = new Padding(3);
            tabNormalSoundEffects.Size = new Size(509, 401);
            tabNormalSoundEffects.TabIndex = 0;
            tabNormalSoundEffects.Text = "Normal AoA Sound Effects";
            tabNormalSoundEffects.UseVisualStyleBackColor = true;
            // 
            // cmbAudioDevice1
            // 
            cmbAudioDevice1.FormattingEnabled = true;
            cmbAudioDevice1.Location = new Point(103, 77);
            cmbAudioDevice1.Name = "cmbAudioDevice1";
            cmbAudioDevice1.Size = new Size(343, 23);
            cmbAudioDevice1.TabIndex = 16;
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Location = new Point(17, 80);
            label33.Name = "label33";
            label33.Size = new Size(80, 15);
            label33.TabIndex = 15;
            label33.Text = "Audio Device:";
            // 
            // chkEnableAoASoundEffects2
            // 
            chkEnableAoASoundEffects2.AutoSize = true;
            chkEnableAoASoundEffects2.Checked = true;
            chkEnableAoASoundEffects2.CheckState = CheckState.Checked;
            chkEnableAoASoundEffects2.Location = new Point(19, 255);
            chkEnableAoASoundEffects2.Name = "chkEnableAoASoundEffects2";
            chkEnableAoASoundEffects2.Size = new Size(268, 19);
            chkEnableAoASoundEffects2.TabIndex = 14;
            chkEnableAoASoundEffects2.Text = "Enable Sound Effect when beyond AoA range:";
            chkEnableAoASoundEffects2.UseVisualStyleBackColor = true;
            // 
            // lblVolumeMultiplier2
            // 
            lblVolumeMultiplier2.AutoSize = true;
            lblVolumeMultiplier2.Location = new Point(454, 343);
            lblVolumeMultiplier2.Name = "lblVolumeMultiplier2";
            lblVolumeMultiplier2.Size = new Size(111, 15);
            lblVolumeMultiplier2.TabIndex = 13;
            lblVolumeMultiplier2.Text = "lblVolumeMultiplier";
            // 
            // trkVolumeMultiplier2
            // 
            trkVolumeMultiplier2.LargeChange = 10;
            trkVolumeMultiplier2.Location = new Point(126, 334);
            trkVolumeMultiplier2.Maximum = 100;
            trkVolumeMultiplier2.Minimum = 1;
            trkVolumeMultiplier2.Name = "trkVolumeMultiplier2";
            trkVolumeMultiplier2.Size = new Size(322, 45);
            trkVolumeMultiplier2.SmallChange = 5;
            trkVolumeMultiplier2.TabIndex = 12;
            trkVolumeMultiplier2.TickFrequency = 10;
            toolTip1.SetToolTip(trkVolumeMultiplier2, "General modifier of volume for the sound effects.  100% is the original sound effect volume.");
            trkVolumeMultiplier2.Value = 60;
            trkVolumeMultiplier2.Scroll += trkVolumeMultiplier2_Scroll;
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.Location = new Point(19, 343);
            label34.Name = "label34";
            label34.Size = new Size(101, 15);
            label34.TabIndex = 11;
            label34.Text = "Volume Multiplier";
            // 
            // label4
            // 
            label4.Location = new Point(17, 15);
            label4.Name = "label4";
            label4.Size = new Size(476, 52);
            label4.TabIndex = 10;
            label4.Text = resources.GetString("label4.Text");
            // 
            // chkEnableAoASoundEffects1
            // 
            chkEnableAoASoundEffects1.AutoSize = true;
            chkEnableAoASoundEffects1.Checked = true;
            chkEnableAoASoundEffects1.CheckState = CheckState.Checked;
            chkEnableAoASoundEffects1.Location = new Point(17, 115);
            chkEnableAoASoundEffects1.Name = "chkEnableAoASoundEffects1";
            chkEnableAoASoundEffects1.Size = new Size(282, 19);
            chkEnableAoASoundEffects1.TabIndex = 9;
            chkEnableAoASoundEffects1.Text = "Enable Sound Effect when in optimal AoA range:";
            chkEnableAoASoundEffects1.UseVisualStyleBackColor = true;
            // 
            // btnSoundEffect2
            // 
            btnSoundEffect2.Location = new Point(456, 303);
            btnSoundEffect2.Name = "btnSoundEffect2";
            btnSoundEffect2.Size = new Size(37, 23);
            btnSoundEffect2.TabIndex = 8;
            btnSoundEffect2.Tag = "WAV Files|*.wav|MP3 files|*.mp3|All files|*.*";
            btnSoundEffect2.Text = "...";
            btnSoundEffect2.UseVisualStyleBackColor = true;
            btnSoundEffect2.Click += btnSoundEffect2_Click;
            // 
            // btnSoundEffect1
            // 
            btnSoundEffect1.Location = new Point(456, 167);
            btnSoundEffect1.Name = "btnSoundEffect1";
            btnSoundEffect1.Size = new Size(37, 23);
            btnSoundEffect1.TabIndex = 7;
            btnSoundEffect1.Tag = "WAV Files|*.wav|MP3 files|*.mp3|All files|*.*";
            btnSoundEffect1.Text = "...";
            btnSoundEffect1.UseVisualStyleBackColor = true;
            btnSoundEffect1.Click += btnSoundEffect1_Click;
            // 
            // lblVolumeMultiplier1
            // 
            lblVolumeMultiplier1.AutoSize = true;
            lblVolumeMultiplier1.Location = new Point(456, 204);
            lblVolumeMultiplier1.Name = "lblVolumeMultiplier1";
            lblVolumeMultiplier1.Size = new Size(111, 15);
            lblVolumeMultiplier1.TabIndex = 6;
            lblVolumeMultiplier1.Text = "lblVolumeMultiplier";
            // 
            // trkVolumeMultiplier1
            // 
            trkVolumeMultiplier1.LargeChange = 10;
            trkVolumeMultiplier1.Location = new Point(124, 195);
            trkVolumeMultiplier1.Maximum = 100;
            trkVolumeMultiplier1.Minimum = 1;
            trkVolumeMultiplier1.Name = "trkVolumeMultiplier1";
            trkVolumeMultiplier1.Size = new Size(322, 45);
            trkVolumeMultiplier1.SmallChange = 5;
            trkVolumeMultiplier1.TabIndex = 5;
            trkVolumeMultiplier1.TickFrequency = 10;
            toolTip1.SetToolTip(trkVolumeMultiplier1, "General modifier of volume for the sound effects.  100% is the original sound effect volume.");
            trkVolumeMultiplier1.Value = 60;
            trkVolumeMultiplier1.Scroll += trkVolumeMultiplier1_Scroll;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(17, 204);
            label3.Name = "label3";
            label3.Size = new Size(101, 15);
            label3.TabIndex = 4;
            label3.Text = "Volume Multiplier";
            // 
            // txtSoundEffect2
            // 
            txtSoundEffect2.Location = new Point(17, 304);
            txtSoundEffect2.Name = "txtSoundEffect2";
            txtSoundEffect2.Size = new Size(429, 23);
            txtSoundEffect2.TabIndex = 3;
            txtSoundEffect2.Text = "C:\\Program Files\\Microsoft Office\\root\\Office16\\MEDIA\\APPLAUSE.WAV";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 286);
            label2.Name = "label2";
            label2.Size = new Size(255, 15);
            label2.TabIndex = 2;
            label2.Text = "Sound Effect when beyond optimal AoA range:";
            // 
            // txtSoundEffect1
            // 
            txtSoundEffect1.Location = new Point(17, 167);
            txtSoundEffect1.Name = "txtSoundEffect1";
            txtSoundEffect1.Size = new Size(429, 23);
            txtSoundEffect1.TabIndex = 1;
            txtSoundEffect1.Text = "C:\\Program Files\\Microsoft Office\\root\\Office16\\MEDIA\\APPLAUSE.WAV";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 149);
            label1.Name = "label1";
            label1.Size = new Size(225, 15);
            label1.TabIndex = 0;
            label1.Text = "Sound Effect when in optimal AoA range:";
            // 
            // tabArduino
            // 
            tabArduino.Controls.Add(txtArduinoPort);
            tabArduino.Controls.Add(label11);
            tabArduino.Controls.Add(txtArduinoIP);
            tabArduino.Controls.Add(label10);
            tabArduino.Controls.Add(numMaxIntensityFlaps);
            tabArduino.Controls.Add(numMinIntensityFlaps);
            tabArduino.Controls.Add(label8);
            tabArduino.Controls.Add(label9);
            tabArduino.Controls.Add(chkVibrateMotorForFlaps);
            tabArduino.Controls.Add(numMaxIntensitySpeedBrakes);
            tabArduino.Controls.Add(numMinIntensitySpeedBrakes);
            tabArduino.Controls.Add(label7);
            tabArduino.Controls.Add(label6);
            tabArduino.Controls.Add(label5);
            tabArduino.Controls.Add(chkVibrateMotorForSpeedBrake);
            tabArduino.Location = new Point(4, 24);
            tabArduino.Name = "tabArduino";
            tabArduino.Padding = new Padding(3);
            tabArduino.Size = new Size(509, 401);
            tabArduino.TabIndex = 1;
            tabArduino.Text = "2xVibMotors";
            tabArduino.UseVisualStyleBackColor = true;
            // 
            // txtArduinoPort
            // 
            txtArduinoPort.Location = new Point(389, 251);
            txtArduinoPort.Name = "txtArduinoPort";
            txtArduinoPort.Size = new Size(106, 23);
            txtArduinoPort.TabIndex = 25;
            txtArduinoPort.Text = "54671";
            txtArduinoPort.KeyPress += txtArduinoPort_KeyPress;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(325, 254);
            label11.Name = "label11";
            label11.Size = new Size(58, 15);
            label11.TabIndex = 24;
            label11.Text = "UDP Port:";
            // 
            // txtArduinoIP
            // 
            txtArduinoIP.Location = new Point(134, 251);
            txtArduinoIP.Name = "txtArduinoIP";
            txtArduinoIP.Size = new Size(106, 23);
            txtArduinoIP.TabIndex = 23;
            txtArduinoIP.Text = "192.168.1.249";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(17, 254);
            label10.Name = "label10";
            label10.Size = new Size(111, 15);
            label10.TabIndex = 22;
            label10.Text = "Arduino IP Address:";
            // 
            // numMaxIntensityFlaps
            // 
            numMaxIntensityFlaps.Location = new Point(389, 209);
            numMaxIntensityFlaps.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMaxIntensityFlaps.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxIntensityFlaps.Name = "numMaxIntensityFlaps";
            numMaxIntensityFlaps.Size = new Size(106, 23);
            numMaxIntensityFlaps.TabIndex = 21;
            toolTip1.SetToolTip(numMaxIntensityFlaps, "Intensity of the vibration when the speed brakes are deployed at its maximum range.");
            numMaxIntensityFlaps.Value = new decimal(new int[] { 255, 0, 0, 0 });
            // 
            // numMinIntensityFlaps
            // 
            numMinIntensityFlaps.Location = new Point(134, 209);
            numMinIntensityFlaps.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMinIntensityFlaps.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numMinIntensityFlaps.Name = "numMinIntensityFlaps";
            numMinIntensityFlaps.Size = new Size(106, 23);
            numMinIntensityFlaps.TabIndex = 20;
            toolTip1.SetToolTip(numMinIntensityFlaps, "Intensity of the vibration when the speed brakes are deployed at its initial stage.");
            numMinIntensityFlaps.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(270, 211);
            label8.Name = "label8";
            label8.Size = new Size(113, 15);
            label8.TabIndex = 19;
            label8.Text = "Maximum Intensity:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(17, 211);
            label9.Name = "label9";
            label9.Size = new Size(111, 15);
            label9.TabIndex = 18;
            label9.Text = "Minimum Intensity:";
            // 
            // chkVibrateMotorForFlaps
            // 
            chkVibrateMotorForFlaps.Location = new Point(11, 165);
            chkVibrateMotorForFlaps.Name = "chkVibrateMotorForFlaps";
            chkVibrateMotorForFlaps.Size = new Size(486, 41);
            chkVibrateMotorForFlaps.TabIndex = 17;
            chkVibrateMotorForFlaps.Text = "Vibrate Motor 2 with Flaps Telemetry:  Attach vibration motor where you want to feel this haptic effect.";
            chkVibrateMotorForFlaps.UseVisualStyleBackColor = true;
            // 
            // numMaxIntensitySpeedBrakes
            // 
            numMaxIntensitySpeedBrakes.Location = new Point(389, 122);
            numMaxIntensitySpeedBrakes.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMaxIntensitySpeedBrakes.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxIntensitySpeedBrakes.Name = "numMaxIntensitySpeedBrakes";
            numMaxIntensitySpeedBrakes.Size = new Size(106, 23);
            numMaxIntensitySpeedBrakes.TabIndex = 16;
            toolTip1.SetToolTip(numMaxIntensitySpeedBrakes, "Intensity of the vibration when the speed brakes are deployed at its maximum range.");
            numMaxIntensitySpeedBrakes.Value = new decimal(new int[] { 255, 0, 0, 0 });
            // 
            // numMinIntensitySpeedBrakes
            // 
            numMinIntensitySpeedBrakes.Location = new Point(134, 122);
            numMinIntensitySpeedBrakes.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMinIntensitySpeedBrakes.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numMinIntensitySpeedBrakes.Name = "numMinIntensitySpeedBrakes";
            numMinIntensitySpeedBrakes.Size = new Size(106, 23);
            numMinIntensitySpeedBrakes.TabIndex = 15;
            toolTip1.SetToolTip(numMinIntensitySpeedBrakes, "Intensity of the vibration when the speed brakes are deployed at its initial stage.");
            numMinIntensitySpeedBrakes.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(270, 124);
            label7.Name = "label7";
            label7.Size = new Size(113, 15);
            label7.TabIndex = 14;
            label7.Text = "Maximum Intensity:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(17, 124);
            label6.Name = "label6";
            label6.Size = new Size(111, 15);
            label6.TabIndex = 13;
            label6.Text = "Minimum Intensity:";
            // 
            // label5
            // 
            label5.Location = new Point(17, 15);
            label5.Name = "label5";
            label5.Size = new Size(476, 52);
            label5.TabIndex = 11;
            label5.Text = "Send UDP datagrams with telemetry information to two small vibration motors controlled by a networked Arduino microcontroller.  The vibration motors are supposed to be attached to your hotas or stick.";
            // 
            // chkVibrateMotorForSpeedBrake
            // 
            chkVibrateMotorForSpeedBrake.Checked = true;
            chkVibrateMotorForSpeedBrake.CheckState = CheckState.Checked;
            chkVibrateMotorForSpeedBrake.Location = new Point(17, 78);
            chkVibrateMotorForSpeedBrake.Name = "chkVibrateMotorForSpeedBrake";
            chkVibrateMotorForSpeedBrake.Size = new Size(486, 41);
            chkVibrateMotorForSpeedBrake.TabIndex = 0;
            chkVibrateMotorForSpeedBrake.Text = "Vibrate Motor 1 with Speed Brake Telemetry:  Attach vibration motor near your throttle.";
            chkVibrateMotorForSpeedBrake.UseVisualStyleBackColor = true;
            // 
            // tabTTGO20V3
            // 
            tabTTGO20V3.Controls.Add(chkTWatchDisplayBackground);
            tabTTGO20V3.Controls.Add(chkTWatchVibrate);
            tabTTGO20V3.Controls.Add(txtTWatchPort);
            tabTTGO20V3.Controls.Add(label13);
            tabTTGO20V3.Controls.Add(txtTWatchIP);
            tabTTGO20V3.Controls.Add(label14);
            tabTTGO20V3.Controls.Add(label12);
            tabTTGO20V3.Location = new Point(4, 24);
            tabTTGO20V3.Name = "tabTTGO20V3";
            tabTTGO20V3.Padding = new Padding(3);
            tabTTGO20V3.Size = new Size(509, 401);
            tabTTGO20V3.TabIndex = 2;
            tabTTGO20V3.Text = "T-Watch 2020 V3";
            tabTTGO20V3.UseVisualStyleBackColor = true;
            // 
            // chkTWatchDisplayBackground
            // 
            chkTWatchDisplayBackground.Checked = true;
            chkTWatchDisplayBackground.CheckState = CheckState.Checked;
            chkTWatchDisplayBackground.Location = new Point(17, 133);
            chkTWatchDisplayBackground.Name = "chkTWatchDisplayBackground";
            chkTWatchDisplayBackground.Size = new Size(468, 41);
            chkTWatchDisplayBackground.TabIndex = 31;
            chkTWatchDisplayBackground.Text = "Change display background color based on AoA:  Yellow when below optimal AoA, Green when in optimal AoA and Red when above optimal AoA.";
            chkTWatchDisplayBackground.UseVisualStyleBackColor = true;
            // 
            // chkTWatchVibrate
            // 
            chkTWatchVibrate.Checked = true;
            chkTWatchVibrate.CheckState = CheckState.Checked;
            chkTWatchVibrate.Location = new Point(17, 86);
            chkTWatchVibrate.Name = "chkTWatchVibrate";
            chkTWatchVibrate.Size = new Size(442, 41);
            chkTWatchVibrate.TabIndex = 30;
            chkTWatchVibrate.Text = "Vibrate Motors with AoA Telemetry:  Only when beyond optimal AoA range.";
            chkTWatchVibrate.UseVisualStyleBackColor = true;
            // 
            // txtTWatchPort
            // 
            txtTWatchPort.Location = new Point(338, 203);
            txtTWatchPort.Name = "txtTWatchPort";
            txtTWatchPort.Size = new Size(106, 23);
            txtTWatchPort.TabIndex = 29;
            txtTWatchPort.Text = "54671";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(274, 206);
            label13.Name = "label13";
            label13.Size = new Size(58, 15);
            label13.TabIndex = 28;
            label13.Text = "UDP Port:";
            // 
            // txtTWatchIP
            // 
            txtTWatchIP.Location = new Point(134, 203);
            txtTWatchIP.Name = "txtTWatchIP";
            txtTWatchIP.Size = new Size(106, 23);
            txtTWatchIP.TabIndex = 27;
            txtTWatchIP.Text = "192.168.1.248";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(15, 206);
            label14.Name = "label14";
            label14.Size = new Size(113, 15);
            label14.TabIndex = 26;
            label14.Text = "T-Watch IP Address:";
            // 
            // label12
            // 
            label12.Location = new Point(17, 15);
            label12.Name = "label12";
            label12.Size = new Size(476, 52);
            label12.TabIndex = 12;
            label12.Text = resources.GetString("label12.Text");
            // 
            // tabSettings
            // 
            tabSettings.Controls.Add(btnJSONFile);
            tabSettings.Controls.Add(txtJSON);
            tabSettings.Controls.Add(label17);
            tabSettings.Controls.Add(lblEffectTimeout);
            tabSettings.Controls.Add(trkEffectTimeout);
            tabSettings.Controls.Add(label16);
            tabSettings.Controls.Add(txtListeningPort);
            tabSettings.Controls.Add(label15);
            tabSettings.Location = new Point(4, 24);
            tabSettings.Name = "tabSettings";
            tabSettings.Size = new Size(509, 401);
            tabSettings.TabIndex = 4;
            tabSettings.Text = "General Settings";
            tabSettings.UseVisualStyleBackColor = true;
            // 
            // btnJSONFile
            // 
            btnJSONFile.Location = new Point(455, 151);
            btnJSONFile.Name = "btnJSONFile";
            btnJSONFile.Size = new Size(37, 23);
            btnJSONFile.TabIndex = 10;
            btnJSONFile.Tag = "JSON Files|*.json|All files|*.*";
            btnJSONFile.Text = "...";
            btnJSONFile.UseVisualStyleBackColor = true;
            btnJSONFile.Click += btnJSONFile_Click;
            // 
            // txtJSON
            // 
            txtJSON.Location = new Point(16, 151);
            txtJSON.Name = "txtJSON";
            txtJSON.Size = new Size(429, 23);
            txtJSON.TabIndex = 9;
            txtJSON.Text = "C:\\Users\\ralch\\source\\repos\\UDPEchoServer1\\AoA_UDP_Server\\units.json";
            toolTip1.SetToolTip(txtJSON, "This file defines AoA optimal ranges for each aircraft.");
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(16, 133);
            label17.Name = "label17";
            label17.Size = new Size(59, 15);
            label17.TabIndex = 8;
            label17.Text = "JSON File:";
            // 
            // lblEffectTimeout
            // 
            lblEffectTimeout.AutoSize = true;
            lblEffectTimeout.Location = new Point(234, 66);
            lblEffectTimeout.Name = "lblEffectTimeout";
            lblEffectTimeout.Size = new Size(94, 15);
            lblEffectTimeout.TabIndex = 4;
            lblEffectTimeout.Text = "lblEffectTimeout";
            // 
            // trkEffectTimeout
            // 
            trkEffectTimeout.Location = new Point(106, 57);
            trkEffectTimeout.Minimum = 1;
            trkEffectTimeout.Name = "trkEffectTimeout";
            trkEffectTimeout.Size = new Size(116, 45);
            trkEffectTimeout.TabIndex = 3;
            toolTip1.SetToolTip(trkEffectTimeout, "After the specified number of seconds, all effects are stopped if no more UDP telemetry is received from export.lua");
            trkEffectTimeout.Value = 1;
            trkEffectTimeout.Scroll += trkEffectTimeout_Scroll;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(13, 66);
            label16.Name = "label16";
            label16.Size = new Size(87, 15);
            label16.TabIndex = 2;
            label16.Text = "Effect Timeout:";
            // 
            // txtListeningPort
            // 
            txtListeningPort.Location = new Point(106, 12);
            txtListeningPort.Name = "txtListeningPort";
            txtListeningPort.Size = new Size(116, 23);
            txtListeningPort.TabIndex = 1;
            txtListeningPort.Text = "54671";
            toolTip1.SetToolTip(txtListeningPort, "Make sure your export.lua script sends UDP packets to this port");
            txtListeningPort.KeyPress += txtListeningPort_KeyPress;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(17, 15);
            label15.Name = "label15";
            label15.Size = new Size(83, 15);
            label15.TabIndex = 0;
            label15.Text = "Listening Port:";
            // 
            // tabMonitor
            // 
            tabMonitor.Controls.Add(panel1);
            tabMonitor.Controls.Add(chkShowStatistics);
            tabMonitor.Controls.Add(chkChangeToMonitor);
            tabMonitor.Location = new Point(4, 24);
            tabMonitor.Name = "tabMonitor";
            tabMonitor.Size = new Size(509, 401);
            tabMonitor.TabIndex = 3;
            tabMonitor.Text = "Monitor";
            tabMonitor.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(label31);
            panel1.Controls.Add(label32);
            panel1.Controls.Add(lblDatagramsPerSecond);
            panel1.Controls.Add(label29);
            panel1.Controls.Add(lblProcessingTime);
            panel1.Controls.Add(label27);
            panel1.Controls.Add(label26);
            panel1.Controls.Add(lblCurrentUnitType);
            panel1.Controls.Add(lblSoundStatus);
            panel1.Controls.Add(label19);
            panel1.Controls.Add(lblLastAoA);
            panel1.Controls.Add(label24);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(label25);
            panel1.Controls.Add(label23);
            panel1.Location = new Point(3, 97);
            panel1.Name = "panel1";
            panel1.Size = new Size(492, 197);
            panel1.TabIndex = 10;
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Location = new Point(287, 104);
            label31.Name = "label31";
            label31.Size = new Size(34, 15);
            label31.TabIndex = 16;
            label31.Text = "none";
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Location = new Point(202, 104);
            label32.Name = "label32";
            label32.Size = new Size(37, 15);
            label32.TabIndex = 15;
            label32.Text = "Flaps:";
            // 
            // lblDatagramsPerSecond
            // 
            lblDatagramsPerSecond.AutoSize = true;
            lblDatagramsPerSecond.Location = new Point(388, 162);
            lblDatagramsPerSecond.Name = "lblDatagramsPerSecond";
            lblDatagramsPerSecond.Size = new Size(49, 15);
            lblDatagramsPerSecond.TabIndex = 14;
            lblDatagramsPerSecond.Text = "number";
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.Location = new Point(207, 162);
            label29.Name = "label29";
            label29.Size = new Size(175, 15);
            label29.TabIndex = 13;
            label29.Text = "Datagrams received per second:";
            // 
            // lblProcessingTime
            // 
            lblProcessingTime.AutoSize = true;
            lblProcessingTime.Location = new Point(141, 161);
            lblProcessingTime.Name = "lblProcessingTime";
            lblProcessingTime.Size = new Size(31, 15);
            lblProcessingTime.TabIndex = 12;
            lblProcessingTime.Text = "time";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Location = new Point(12, 161);
            label27.Name = "label27";
            label27.Size = new Size(123, 15);
            label27.TabIndex = 11;
            label27.Text = "Processing Time (ms):";
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new Point(287, 78);
            label26.Name = "label26";
            label26.Size = new Size(34, 15);
            label26.TabIndex = 10;
            label26.Text = "none";
            // 
            // lblCurrentUnitType
            // 
            lblCurrentUnitType.AutoSize = true;
            lblCurrentUnitType.Location = new Point(101, 78);
            lblCurrentUnitType.Name = "lblCurrentUnitType";
            lblCurrentUnitType.Size = new Size(34, 15);
            lblCurrentUnitType.TabIndex = 9;
            lblCurrentUnitType.Text = "none";
            // 
            // lblSoundStatus
            // 
            lblSoundStatus.AutoSize = true;
            lblSoundStatus.Location = new Point(61, 12);
            lblSoundStatus.Name = "lblSoundStatus";
            lblSoundStatus.Size = new Size(113, 15);
            lblSoundStatus.TabIndex = 3;
            lblSoundStatus.Text = "Not playing sounds.";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(11, 12);
            label19.Name = "label19";
            label19.Size = new Size(44, 15);
            label19.TabIndex = 2;
            label19.Text = "Sound:";
            // 
            // lblLastAoA
            // 
            lblLastAoA.AutoSize = true;
            lblLastAoA.Location = new Point(101, 104);
            lblLastAoA.Name = "lblLastAoA";
            lblLastAoA.Size = new Size(34, 15);
            lblLastAoA.TabIndex = 5;
            lblLastAoA.Text = "none";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(202, 78);
            label24.Name = "label24";
            label24.Size = new Size(79, 15);
            label24.TabIndex = 7;
            label24.Text = "Speed Brakes:";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(12, 78);
            label21.Name = "label21";
            label21.Size = new Size(83, 15);
            label21.TabIndex = 4;
            label21.Text = "Last Unit Type:";
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label25.Location = new Point(12, 49);
            label25.Name = "label25";
            label25.Size = new Size(220, 15);
            label25.TabIndex = 8;
            label25.Text = "Last Telemetry received by export.lua:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(61, 104);
            label23.Name = "label23";
            label23.Size = new Size(33, 15);
            label23.TabIndex = 6;
            label23.Text = "AoA:";
            // 
            // chkShowStatistics
            // 
            chkShowStatistics.AutoSize = true;
            chkShowStatistics.Checked = true;
            chkShowStatistics.CheckState = CheckState.Checked;
            chkShowStatistics.Location = new Point(15, 55);
            chkShowStatistics.Name = "chkShowStatistics";
            chkShowStatistics.Size = new Size(299, 19);
            chkShowStatistics.TabIndex = 9;
            chkShowStatistics.Text = "Show statistics and additional info once per second.";
            chkShowStatistics.UseVisualStyleBackColor = true;
            chkShowStatistics.CheckedChanged += checkBox5_CheckedChanged;
            // 
            // chkChangeToMonitor
            // 
            chkChangeToMonitor.AutoSize = true;
            chkChangeToMonitor.Checked = true;
            chkChangeToMonitor.CheckState = CheckState.Checked;
            chkChangeToMonitor.Location = new Point(14, 20);
            chkChangeToMonitor.Name = "chkChangeToMonitor";
            chkChangeToMonitor.Size = new Size(298, 19);
            chkChangeToMonitor.TabIndex = 0;
            chkChangeToMonitor.Text = "Automatically change to monitor tab at server start.";
            chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // btnStartListening
            // 
            btnStartListening.Location = new Point(346, 457);
            btnStartListening.Name = "btnStartListening";
            btnStartListening.Size = new Size(100, 23);
            btnStartListening.TabIndex = 0;
            btnStartListening.Text = "Start &Listening";
            btnStartListening.UseVisualStyleBackColor = true;
            btnStartListening.Click += btnStartListening_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(462, 457);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 2;
            btnStop.Text = "&Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "WAV Files|*.wav|MP3 files|*.mp3|All files|*.*";
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 496);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(566, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(29, 17);
            toolStripStatusLabel1.Text = "Idle.";
            // 
            // frmMain
            // 
            AcceptButton = btnStartListening;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnStop;
            ClientSize = new Size(566, 518);
            Controls.Add(statusStrip1);
            Controls.Add(btnStop);
            Controls.Add(btnStartListening);
            Controls.Add(tabs);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "frmMain";
            Text = "Telemetry Vib-Sound-Shaker [UDP Server]";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            tabs.ResumeLayout(false);
            tabNormalSoundEffects.ResumeLayout(false);
            tabNormalSoundEffects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier2).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier1).EndInit();
            tabArduino.ResumeLayout(false);
            tabArduino.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxIntensityFlaps).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinIntensityFlaps).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxIntensitySpeedBrakes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinIntensitySpeedBrakes).EndInit();
            tabTTGO20V3.ResumeLayout(false);
            tabTTGO20V3.PerformLayout();
            tabSettings.ResumeLayout(false);
            tabSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkEffectTimeout).EndInit();
            tabMonitor.ResumeLayout(false);
            tabMonitor.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabs;
        private TabPage tabNormalSoundEffects;
        private TabPage tabArduino;
        private TabPage tabTTGO20V3;
        private TabPage tabMonitor;
        private Button btnStartListening;
        private Button btnStop;
        private Label label1;
        private TextBox txtSoundEffect2;
        private Label label2;
        private TextBox txtSoundEffect1;
        private TrackBar trkVolumeMultiplier1;
        private Label label3;
        private Label lblVolumeMultiplier1;
        private Button btnSoundEffect2;
        private Button btnSoundEffect1;
        private CheckBox chkChangeToMonitor;
        private OpenFileDialog openFileDialog1;
        private TabPage tabSettings;
        private CheckBox chkEnableAoASoundEffects1;
        private Label label4;
        private CheckBox chkVibrateMotorForSpeedBrake;
        private Label label5;
        private Label label7;
        private Label label6;
        private NumericUpDown numMinIntensitySpeedBrakes;
        private NumericUpDown numMaxIntensitySpeedBrakes;
        private ToolTip toolTip1;
        private CheckBox chkVibrateMotorForFlaps;
        private NumericUpDown numMaxIntensityFlaps;
        private NumericUpDown numMinIntensityFlaps;
        private Label label8;
        private Label label9;
        private Label label10;
        private TextBox txtArduinoIP;
        private TextBox txtArduinoPort;
        private Label label11;
        private Label label12;
        private TextBox txtTWatchPort;
        private Label label13;
        private TextBox txtTWatchIP;
        private Label label14;
        private CheckBox chkTWatchVibrate;
        private CheckBox chkTWatchDisplayBackground;
        private Label label15;
        private TextBox txtListeningPort;
        private TrackBar trkEffectTimeout;
        private Label label16;
        private Label lblEffectTimeout;
        private Button btnJSONFile;
        private TextBox txtJSON;
        private Label label17;
        private Label lblSoundStatus;
        private Label label19;
        private Label label24;
        private Label label23;
        private Label lblLastAoA;
        private Label label21;
        private Label label25;
        private CheckBox chkShowStatistics;
        private Panel panel1;
        private Label lblCurrentUnitType;
        private Label label26;
        private Label label29;
        private Label lblProcessingTime;
        private Label label27;
        private Label lblDatagramsPerSecond;
        private Label label31;
        private Label label32;
        private Label lblVolumeMultiplier2;
        private TrackBar trkVolumeMultiplier2;
        private Label label34;
        private CheckBox chkEnableAoASoundEffects2;
        private ComboBox cmbAudioDevice1;
        private Label label33;
        private System.Windows.Forms.Timer timer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
    }
}
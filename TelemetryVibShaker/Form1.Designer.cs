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
            nudMinAltitude = new NumericUpDown();
            label38 = new Label();
            label39 = new Label();
            chkUseEfficiencyCoresOnly = new CheckBox();
            chkUseBackgroundProcessing = new CheckBox();
            cmbPriorityClass = new ComboBox();
            label30 = new Label();
            nudMinSpeed = new NumericUpDown();
            label26 = new Label();
            label22 = new Label();
            btnJSONFile = new Button();
            txtJSON = new TextBox();
            label17 = new Label();
            lblEffectTimeout = new Label();
            trkEffectTimeout = new TrackBar();
            label16 = new Label();
            txtListeningPort = new TextBox();
            label15 = new Label();
            Test = new TabPage();
            lblTestErrMsg = new Label();
            btnTestTWatchDisplay = new Button();
            btnTestTWatchMotor = new Button();
            btnTestSoundEffect2 = new Button();
            btnTestSoundEffect1 = new Button();
            btnTestArduinoMotors = new Button();
            tabMonitor = new TabPage();
            panel1 = new Panel();
            lblProcessingTimeUIAvg = new Label();
            lblProcessingTimeUDPAvg = new Label();
            label49 = new Label();
            lblProcessingTimeUIMin = new Label();
            lblProcessingTimeUDPMin = new Label();
            label46 = new Label();
            label44 = new Label();
            label42 = new Label();
            label41 = new Label();
            label40 = new Label();
            lblAltitude = new Label();
            label43 = new Label();
            lblGForces = new Label();
            label45 = new Label();
            label37 = new Label();
            label36 = new Label();
            label35 = new Label();
            lblTimestamp = new Label();
            label31 = new Label();
            lblUIThreadID = new Label();
            label27 = new Label();
            lblLastProcessorUsedUI = new Label();
            lblProcessingTimeUIMax = new Label();
            lblLastProcessorUsedUDP = new Label();
            label28 = new Label();
            lblSpeed = new Label();
            label20 = new Label();
            lblAoAUnits = new Label();
            lblUDPServerThread = new Label();
            label18 = new Label();
            lblLastFlaps = new Label();
            label32 = new Label();
            lblDatagramsPerSecond = new Label();
            label29 = new Label();
            lblProcessingTimeUDPMax = new Label();
            lblMaxProcessingTimeTitle = new Label();
            lblLastSpeedBrakes = new Label();
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
            btnResetMax = new Button();
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
            ((System.ComponentModel.ISupportInitialize)nudMinAltitude).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudMinSpeed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkEffectTimeout).BeginInit();
            Test.SuspendLayout();
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
            tabs.Controls.Add(Test);
            tabs.Controls.Add(tabMonitor);
            tabs.Location = new Point(24, 14);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(517, 412);
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
            tabNormalSoundEffects.Size = new Size(509, 384);
            tabNormalSoundEffects.TabIndex = 0;
            tabNormalSoundEffects.Text = "Sound Effects";
            tabNormalSoundEffects.UseVisualStyleBackColor = true;
            // 
            // cmbAudioDevice1
            // 
            cmbAudioDevice1.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAudioDevice1.FormattingEnabled = true;
            cmbAudioDevice1.Location = new Point(103, 76);
            cmbAudioDevice1.Name = "cmbAudioDevice1";
            cmbAudioDevice1.Size = new Size(343, 23);
            cmbAudioDevice1.TabIndex = 16;
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Location = new Point(17, 79);
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
            chkEnableAoASoundEffects2.Location = new Point(19, 254);
            chkEnableAoASoundEffects2.Name = "chkEnableAoASoundEffects2";
            chkEnableAoASoundEffects2.Size = new Size(268, 19);
            chkEnableAoASoundEffects2.TabIndex = 14;
            chkEnableAoASoundEffects2.Text = "Enable Sound Effect when beyond AoA range:";
            chkEnableAoASoundEffects2.UseVisualStyleBackColor = true;
            chkEnableAoASoundEffects2.CheckedChanged += chkEnableAoASoundEffects2_CheckedChanged;
            // 
            // lblVolumeMultiplier2
            // 
            lblVolumeMultiplier2.AutoSize = true;
            lblVolumeMultiplier2.Location = new Point(454, 342);
            lblVolumeMultiplier2.Name = "lblVolumeMultiplier2";
            lblVolumeMultiplier2.Size = new Size(111, 15);
            lblVolumeMultiplier2.TabIndex = 13;
            lblVolumeMultiplier2.Text = "lblVolumeMultiplier";
            // 
            // trkVolumeMultiplier2
            // 
            trkVolumeMultiplier2.LargeChange = 10;
            trkVolumeMultiplier2.Location = new Point(126, 333);
            trkVolumeMultiplier2.Maximum = 100;
            trkVolumeMultiplier2.Minimum = 1;
            trkVolumeMultiplier2.Name = "trkVolumeMultiplier2";
            trkVolumeMultiplier2.Size = new Size(322, 45);
            trkVolumeMultiplier2.SmallChange = 5;
            trkVolumeMultiplier2.TabIndex = 12;
            trkVolumeMultiplier2.TickFrequency = 10;
            toolTip1.SetToolTip(trkVolumeMultiplier2, "General modifier of volume for the sound effects.  100% is the original sound effectType volume.");
            trkVolumeMultiplier2.Value = 60;
            trkVolumeMultiplier2.Scroll += trkVolumeMultiplier2_Scroll;
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.Location = new Point(19, 342);
            label34.Name = "label34";
            label34.Size = new Size(101, 15);
            label34.TabIndex = 11;
            label34.Text = "Volume Multiplier";
            // 
            // label4
            // 
            label4.Location = new Point(17, 14);
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
            chkEnableAoASoundEffects1.Location = new Point(17, 114);
            chkEnableAoASoundEffects1.Name = "chkEnableAoASoundEffects1";
            chkEnableAoASoundEffects1.Size = new Size(282, 19);
            chkEnableAoASoundEffects1.TabIndex = 9;
            chkEnableAoASoundEffects1.Text = "Enable Sound Effect when in optimal AoA range:";
            chkEnableAoASoundEffects1.UseVisualStyleBackColor = true;
            chkEnableAoASoundEffects1.CheckedChanged += chkEnableAoASoundEffects1_CheckedChanged;
            // 
            // btnSoundEffect2
            // 
            btnSoundEffect2.Location = new Point(456, 302);
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
            btnSoundEffect1.Location = new Point(456, 166);
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
            lblVolumeMultiplier1.Location = new Point(456, 203);
            lblVolumeMultiplier1.Name = "lblVolumeMultiplier1";
            lblVolumeMultiplier1.Size = new Size(111, 15);
            lblVolumeMultiplier1.TabIndex = 6;
            lblVolumeMultiplier1.Text = "lblVolumeMultiplier";
            // 
            // trkVolumeMultiplier1
            // 
            trkVolumeMultiplier1.LargeChange = 10;
            trkVolumeMultiplier1.Location = new Point(124, 194);
            trkVolumeMultiplier1.Maximum = 100;
            trkVolumeMultiplier1.Minimum = 1;
            trkVolumeMultiplier1.Name = "trkVolumeMultiplier1";
            trkVolumeMultiplier1.Size = new Size(322, 45);
            trkVolumeMultiplier1.SmallChange = 5;
            trkVolumeMultiplier1.TabIndex = 5;
            trkVolumeMultiplier1.TickFrequency = 10;
            toolTip1.SetToolTip(trkVolumeMultiplier1, "General modifier of volume for the sound effects.  100% is the original sound effectType volume.");
            trkVolumeMultiplier1.Value = 60;
            trkVolumeMultiplier1.Scroll += trkVolumeMultiplier1_Scroll;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(17, 203);
            label3.Name = "label3";
            label3.Size = new Size(101, 15);
            label3.TabIndex = 4;
            label3.Text = "Volume Multiplier";
            // 
            // txtSoundEffect2
            // 
            txtSoundEffect2.Location = new Point(17, 303);
            txtSoundEffect2.Name = "txtSoundEffect2";
            txtSoundEffect2.Size = new Size(429, 23);
            txtSoundEffect2.TabIndex = 3;
            txtSoundEffect2.Text = "\"C:\\Users\\ralch\\Music\\AoA\\VMS_stall.wav\"";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 285);
            label2.Name = "label2";
            label2.Size = new Size(255, 15);
            label2.TabIndex = 2;
            label2.Text = "Sound Effect when beyond optimal AoA range:";
            // 
            // txtSoundEffect1
            // 
            txtSoundEffect1.Location = new Point(17, 166);
            txtSoundEffect1.Name = "txtSoundEffect1";
            txtSoundEffect1.Size = new Size(429, 23);
            txtSoundEffect1.TabIndex = 1;
            txtSoundEffect1.Text = "C:\\Users\\ralch\\Music\\AoA\\Full Throttle Electric.wav";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 148);
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
            tabArduino.Size = new Size(509, 384);
            tabArduino.TabIndex = 1;
            tabArduino.Text = "Arduino";
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
            numMaxIntensityFlaps.Tag = "";
            toolTip1.SetToolTip(numMaxIntensityFlaps, "Intensity of the vibration when the speed brakes are deployed at its maximum range.");
            numMaxIntensityFlaps.Value = new decimal(new int[] { 255, 0, 0, 0 });
            numMaxIntensityFlaps.ValueChanged += numMaxIntensityFlaps_ValueChanged;
            // 
            // numMinIntensityFlaps
            // 
            numMinIntensityFlaps.Location = new Point(134, 209);
            numMinIntensityFlaps.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMinIntensityFlaps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMinIntensityFlaps.Name = "numMinIntensityFlaps";
            numMinIntensityFlaps.Size = new Size(106, 23);
            numMinIntensityFlaps.TabIndex = 20;
            numMinIntensityFlaps.Tag = "";
            toolTip1.SetToolTip(numMinIntensityFlaps, "Intensity of the vibration when the speed brakes are deployed at its initial stage.");
            numMinIntensityFlaps.Value = new decimal(new int[] { 100, 0, 0, 0 });
            numMinIntensityFlaps.ValueChanged += numMinIntensityFlaps_ValueChanged;
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
            chkVibrateMotorForFlaps.Checked = true;
            chkVibrateMotorForFlaps.CheckState = CheckState.Checked;
            chkVibrateMotorForFlaps.Location = new Point(11, 165);
            chkVibrateMotorForFlaps.Name = "chkVibrateMotorForFlaps";
            chkVibrateMotorForFlaps.Size = new Size(486, 41);
            chkVibrateMotorForFlaps.TabIndex = 17;
            chkVibrateMotorForFlaps.Text = "Vibrate Motor 2 with Flaps Telemetry:  Attach vibration motor where you want to feel this haptic effectType.";
            chkVibrateMotorForFlaps.UseVisualStyleBackColor = true;
            chkVibrateMotorForFlaps.CheckedChanged += chkVibrateMotorForFlaps_CheckedChanged;
            // 
            // numMaxIntensitySpeedBrakes
            // 
            numMaxIntensitySpeedBrakes.Location = new Point(389, 122);
            numMaxIntensitySpeedBrakes.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMaxIntensitySpeedBrakes.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxIntensitySpeedBrakes.Name = "numMaxIntensitySpeedBrakes";
            numMaxIntensitySpeedBrakes.Size = new Size(106, 23);
            numMaxIntensitySpeedBrakes.TabIndex = 16;
            numMaxIntensitySpeedBrakes.Tag = "";
            toolTip1.SetToolTip(numMaxIntensitySpeedBrakes, "Intensity of the vibration when the speed brakes are deployed at its maximum range.");
            numMaxIntensitySpeedBrakes.Value = new decimal(new int[] { 255, 0, 0, 0 });
            numMaxIntensitySpeedBrakes.ValueChanged += numMaxIntensitySpeedBrakes_ValueChanged;
            // 
            // numMinIntensitySpeedBrakes
            // 
            numMinIntensitySpeedBrakes.Location = new Point(134, 122);
            numMinIntensitySpeedBrakes.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numMinIntensitySpeedBrakes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMinIntensitySpeedBrakes.Name = "numMinIntensitySpeedBrakes";
            numMinIntensitySpeedBrakes.Size = new Size(106, 23);
            numMinIntensitySpeedBrakes.TabIndex = 15;
            numMinIntensitySpeedBrakes.Tag = "";
            toolTip1.SetToolTip(numMinIntensitySpeedBrakes, "Intensity of the vibration when the speed brakes are deployed at its initial stage.");
            numMinIntensitySpeedBrakes.Value = new decimal(new int[] { 100, 0, 0, 0 });
            numMinIntensitySpeedBrakes.ValueChanged += numMinIntensitySpeedBrakes_ValueChanged;
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
            label5.Text = resources.GetString("label5.Text");
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
            chkVibrateMotorForSpeedBrake.CheckedChanged += chkVibrateMotorForSpeedBrake_CheckedChanged;
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
            tabTTGO20V3.Size = new Size(509, 384);
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
            chkTWatchDisplayBackground.CheckedChanged += chkTWatchDisplayBackground_CheckedChanged;
            // 
            // chkTWatchVibrate
            // 
            chkTWatchVibrate.Checked = true;
            chkTWatchVibrate.CheckState = CheckState.Checked;
            chkTWatchVibrate.Location = new Point(17, 86);
            chkTWatchVibrate.Name = "chkTWatchVibrate";
            chkTWatchVibrate.Size = new Size(442, 41);
            chkTWatchVibrate.TabIndex = 30;
            chkTWatchVibrate.Text = "Vibrate Motors with Gear Telemetry:  Vibration is only on or off in the TWatch.";
            chkTWatchVibrate.UseVisualStyleBackColor = true;
            chkTWatchVibrate.CheckedChanged += chkTWatchVibrate_CheckedChanged;
            // 
            // txtTWatchPort
            // 
            txtTWatchPort.Location = new Point(338, 203);
            txtTWatchPort.Name = "txtTWatchPort";
            txtTWatchPort.Size = new Size(106, 23);
            txtTWatchPort.TabIndex = 29;
            txtTWatchPort.Text = "54671";
            txtTWatchPort.KeyPress += txtTWatchPort_KeyPress;
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
            tabSettings.Controls.Add(nudMinAltitude);
            tabSettings.Controls.Add(label38);
            tabSettings.Controls.Add(label39);
            tabSettings.Controls.Add(chkUseEfficiencyCoresOnly);
            tabSettings.Controls.Add(chkUseBackgroundProcessing);
            tabSettings.Controls.Add(cmbPriorityClass);
            tabSettings.Controls.Add(label30);
            tabSettings.Controls.Add(nudMinSpeed);
            tabSettings.Controls.Add(label26);
            tabSettings.Controls.Add(label22);
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
            tabSettings.Size = new Size(509, 384);
            tabSettings.TabIndex = 4;
            tabSettings.Text = "General Settings";
            tabSettings.UseVisualStyleBackColor = true;
            // 
            // nudMinAltitude
            // 
            nudMinAltitude.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            nudMinAltitude.Location = new Point(122, 172);
            nudMinAltitude.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            nudMinAltitude.Name = "nudMinAltitude";
            nudMinAltitude.Size = new Size(116, 23);
            nudMinAltitude.TabIndex = 21;
            toolTip1.SetToolTip(nudMinAltitude, "Effects won't be active until your aircraft is above this altitude above the ground");
            nudMinAltitude.ValueChanged += nudMinAltitude_ValueChanged;
            // 
            // label38
            // 
            label38.AutoSize = true;
            label38.Location = new Point(244, 174);
            label38.Name = "label38";
            label38.Size = new Size(131, 15);
            label38.TabIndex = 20;
            label38.Text = "meters (Above Ground)";
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.Location = new Point(3, 175);
            label39.Name = "label39";
            label39.Size = new Size(108, 15);
            label39.TabIndex = 19;
            label39.Text = "Minimum Altitude:";
            label39.TextAlign = ContentAlignment.TopRight;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(17, 341);
            chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            chkUseEfficiencyCoresOnly.Size = new Size(290, 19);
            chkUseEfficiencyCoresOnly.TabIndex = 18;
            chkUseEfficiencyCoresOnly.Text = "Use Efficiency cores only on Intel 12700K";
            chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            chkUseEfficiencyCoresOnly.Visible = false;
            chkUseEfficiencyCoresOnly.CheckedChanged += chkUseEfficiencyCoresOnly_CheckedChanged;
            // 
            // chkUseBackgroundProcessing
            // 
            chkUseBackgroundProcessing.AutoSize = true;
            chkUseBackgroundProcessing.Location = new Point(17, 314);
            chkUseBackgroundProcessing.Name = "chkUseBackgroundProcessing";
            chkUseBackgroundProcessing.Size = new Size(206, 19);
            chkUseBackgroundProcessing.TabIndex = 17;
            chkUseBackgroundProcessing.Text = "Use background processing mode";
            chkUseBackgroundProcessing.UseVisualStyleBackColor = true;
            chkUseBackgroundProcessing.CheckedChanged += chkUseBackgroundProcessing_CheckedChanged;
            // 
            // cmbPriorityClass
            // 
            cmbPriorityClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriorityClass.FormattingEnabled = true;
            cmbPriorityClass.Items.AddRange(new object[] { "NORMAL", "BELOW NORMAL", "IDLE" });
            cmbPriorityClass.Location = new Point(144, 277);
            cmbPriorityClass.Name = "cmbPriorityClass";
            cmbPriorityClass.Size = new Size(121, 23);
            cmbPriorityClass.TabIndex = 15;
            cmbPriorityClass.SelectedIndexChanged += cmbPriorityClass_SelectedIndexChanged;
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new Point(17, 280);
            label30.Name = "label30";
            label30.Size = new Size(121, 15);
            label30.TabIndex = 14;
            label30.Text = "Process Priority Class:";
            // 
            // nudMinSpeed
            // 
            nudMinSpeed.Increment = new decimal(new int[] { 20, 0, 0, 0 });
            nudMinSpeed.Location = new Point(122, 128);
            nudMinSpeed.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            nudMinSpeed.Name = "nudMinSpeed";
            nudMinSpeed.Size = new Size(116, 23);
            nudMinSpeed.TabIndex = 13;
            toolTip1.SetToolTip(nudMinSpeed, "Effects won't be active until your aircraft is above this speed");
            nudMinSpeed.ValueChanged += nudMinSpeed_ValueChanged;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new Point(244, 130);
            label26.Name = "label26";
            label26.Size = new Size(36, 15);
            label26.TabIndex = 12;
            label26.Text = "km/h";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(13, 131);
            label22.Name = "label22";
            label22.Size = new Size(98, 15);
            label22.TabIndex = 11;
            label22.Text = "Minimum Speed:";
            // 
            // btnJSONFile
            // 
            btnJSONFile.Location = new Point(455, 233);
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
            txtJSON.Location = new Point(16, 233);
            txtJSON.Name = "txtJSON";
            txtJSON.Size = new Size(429, 23);
            txtJSON.TabIndex = 9;
            txtJSON.Text = "C:\\Users\\ralch\\source\\repos\\UDPEchoServer1\\AoA_UDP_Server\\units.json";
            toolTip1.SetToolTip(txtJSON, "This file defines AoA optimal ranges for each aircraft.");
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(16, 215);
            label17.Name = "label17";
            label17.Size = new Size(59, 15);
            label17.TabIndex = 8;
            label17.Text = "JSON File:";
            // 
            // lblEffectTimeout
            // 
            lblEffectTimeout.AutoSize = true;
            lblEffectTimeout.Location = new Point(250, 71);
            lblEffectTimeout.Name = "lblEffectTimeout";
            lblEffectTimeout.Size = new Size(94, 15);
            lblEffectTimeout.TabIndex = 4;
            lblEffectTimeout.Text = "lblEffectTimeout";
            // 
            // trkEffectTimeout
            // 
            trkEffectTimeout.Location = new Point(122, 62);
            trkEffectTimeout.Minimum = 1;
            trkEffectTimeout.Name = "trkEffectTimeout";
            trkEffectTimeout.Size = new Size(116, 45);
            trkEffectTimeout.TabIndex = 3;
            toolTip1.SetToolTip(trkEffectTimeout, "After the specified number of seconds, sound effects are stopped if no more UDP telemetry is received from export.lua");
            trkEffectTimeout.Value = 1;
            trkEffectTimeout.Scroll += trkEffectTimeout_Scroll;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(24, 72);
            label16.Name = "label16";
            label16.Size = new Size(87, 15);
            label16.TabIndex = 2;
            label16.Text = "Effect Timeout:";
            // 
            // txtListeningPort
            // 
            txtListeningPort.Location = new Point(122, 17);
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
            label15.Location = new Point(28, 21);
            label15.Name = "label15";
            label15.Size = new Size(83, 15);
            label15.TabIndex = 0;
            label15.Text = "Listening Port:";
            // 
            // Test
            // 
            Test.Controls.Add(lblTestErrMsg);
            Test.Controls.Add(btnTestTWatchDisplay);
            Test.Controls.Add(btnTestTWatchMotor);
            Test.Controls.Add(btnTestSoundEffect2);
            Test.Controls.Add(btnTestSoundEffect1);
            Test.Controls.Add(btnTestArduinoMotors);
            Test.Location = new Point(4, 24);
            Test.Name = "Test";
            Test.Size = new Size(509, 384);
            Test.TabIndex = 5;
            Test.Text = "Test";
            Test.UseVisualStyleBackColor = true;
            // 
            // lblTestErrMsg
            // 
            lblTestErrMsg.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            lblTestErrMsg.ForeColor = Color.FromArgb(192, 0, 0);
            lblTestErrMsg.Location = new Point(29, 213);
            lblTestErrMsg.Name = "lblTestErrMsg";
            lblTestErrMsg.Size = new Size(431, 135);
            lblTestErrMsg.TabIndex = 5;
            lblTestErrMsg.Text = "error msg";
            lblTestErrMsg.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnTestTWatchDisplay
            // 
            btnTestTWatchDisplay.Location = new Point(282, 82);
            btnTestTWatchDisplay.Name = "btnTestTWatchDisplay";
            btnTestTWatchDisplay.Size = new Size(178, 23);
            btnTestTWatchDisplay.TabIndex = 4;
            btnTestTWatchDisplay.Text = "Test T-Watch Display";
            btnTestTWatchDisplay.UseVisualStyleBackColor = true;
            btnTestTWatchDisplay.Click += TestTWatchDisplay_Click;
            // 
            // btnTestTWatchMotor
            // 
            btnTestTWatchMotor.Location = new Point(282, 30);
            btnTestTWatchMotor.Name = "btnTestTWatchMotor";
            btnTestTWatchMotor.Size = new Size(178, 23);
            btnTestTWatchMotor.TabIndex = 3;
            btnTestTWatchMotor.Text = "Test T-Watch Motor";
            btnTestTWatchMotor.UseVisualStyleBackColor = true;
            btnTestTWatchMotor.Click += TestTWatchMotor_Click;
            // 
            // btnTestSoundEffect2
            // 
            btnTestSoundEffect2.Location = new Point(29, 139);
            btnTestSoundEffect2.Name = "btnTestSoundEffect2";
            btnTestSoundEffect2.Size = new Size(178, 23);
            btnTestSoundEffect2.TabIndex = 2;
            btnTestSoundEffect2.Text = "Test Sound Effect 2";
            btnTestSoundEffect2.UseVisualStyleBackColor = true;
            btnTestSoundEffect2.Click += TestSoundEffect2_Click;
            // 
            // btnTestSoundEffect1
            // 
            btnTestSoundEffect1.Location = new Point(29, 82);
            btnTestSoundEffect1.Name = "btnTestSoundEffect1";
            btnTestSoundEffect1.Size = new Size(178, 23);
            btnTestSoundEffect1.TabIndex = 1;
            btnTestSoundEffect1.Text = "Test Sound Effect 1";
            btnTestSoundEffect1.UseVisualStyleBackColor = true;
            btnTestSoundEffect1.Click += TestSoundEffect1_Click;
            // 
            // btnTestArduinoMotors
            // 
            btnTestArduinoMotors.Location = new Point(29, 30);
            btnTestArduinoMotors.Name = "btnTestArduinoMotors";
            btnTestArduinoMotors.Size = new Size(178, 23);
            btnTestArduinoMotors.TabIndex = 0;
            btnTestArduinoMotors.Text = "Test Arduino Motor 1 && 2";
            btnTestArduinoMotors.UseVisualStyleBackColor = true;
            btnTestArduinoMotors.Click += btnTestArduinoMotors_Click;
            // 
            // tabMonitor
            // 
            tabMonitor.Controls.Add(panel1);
            tabMonitor.Controls.Add(chkShowStatistics);
            tabMonitor.Controls.Add(chkChangeToMonitor);
            tabMonitor.Location = new Point(4, 24);
            tabMonitor.Name = "tabMonitor";
            tabMonitor.Size = new Size(509, 384);
            tabMonitor.TabIndex = 3;
            tabMonitor.Text = "Monitor";
            tabMonitor.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(lblProcessingTimeUIAvg);
            panel1.Controls.Add(lblProcessingTimeUDPAvg);
            panel1.Controls.Add(label49);
            panel1.Controls.Add(lblProcessingTimeUIMin);
            panel1.Controls.Add(lblProcessingTimeUDPMin);
            panel1.Controls.Add(label46);
            panel1.Controls.Add(label44);
            panel1.Controls.Add(label42);
            panel1.Controls.Add(label41);
            panel1.Controls.Add(label40);
            panel1.Controls.Add(lblAltitude);
            panel1.Controls.Add(label43);
            panel1.Controls.Add(lblGForces);
            panel1.Controls.Add(label45);
            panel1.Controls.Add(label37);
            panel1.Controls.Add(label36);
            panel1.Controls.Add(label35);
            panel1.Controls.Add(lblTimestamp);
            panel1.Controls.Add(label31);
            panel1.Controls.Add(lblUIThreadID);
            panel1.Controls.Add(label27);
            panel1.Controls.Add(lblLastProcessorUsedUI);
            panel1.Controls.Add(lblProcessingTimeUIMax);
            panel1.Controls.Add(lblLastProcessorUsedUDP);
            panel1.Controls.Add(label28);
            panel1.Controls.Add(lblSpeed);
            panel1.Controls.Add(label20);
            panel1.Controls.Add(lblAoAUnits);
            panel1.Controls.Add(lblUDPServerThread);
            panel1.Controls.Add(label18);
            panel1.Controls.Add(lblLastFlaps);
            panel1.Controls.Add(label32);
            panel1.Controls.Add(lblDatagramsPerSecond);
            panel1.Controls.Add(label29);
            panel1.Controls.Add(lblProcessingTimeUDPMax);
            panel1.Controls.Add(lblMaxProcessingTimeTitle);
            panel1.Controls.Add(lblLastSpeedBrakes);
            panel1.Controls.Add(lblCurrentUnitType);
            panel1.Controls.Add(lblSoundStatus);
            panel1.Controls.Add(label19);
            panel1.Controls.Add(lblLastAoA);
            panel1.Controls.Add(label24);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(label25);
            panel1.Controls.Add(label23);
            panel1.Location = new Point(3, 68);
            panel1.Name = "panel1";
            panel1.Size = new Size(503, 299);
            panel1.TabIndex = 10;
            // 
            // lblProcessingTimeUIAvg
            // 
            lblProcessingTimeUIAvg.AutoSize = true;
            lblProcessingTimeUIAvg.Location = new Point(410, 271);
            lblProcessingTimeUIAvg.Name = "lblProcessingTimeUIAvg";
            lblProcessingTimeUIAvg.Size = new Size(45, 15);
            lblProcessingTimeUIAvg.TabIndex = 47;
            lblProcessingTimeUIAvg.Tag = "-1";
            lblProcessingTimeUIAvg.Text = "UI time";
            toolTip1.SetToolTip(lblProcessingTimeUIAvg, "Max UI processing time (monitor).  Ignores the first one.");
            // 
            // lblProcessingTimeUDPAvg
            // 
            lblProcessingTimeUDPAvg.AutoSize = true;
            lblProcessingTimeUDPAvg.Location = new Point(410, 246);
            lblProcessingTimeUDPAvg.Name = "lblProcessingTimeUDPAvg";
            lblProcessingTimeUDPAvg.Size = new Size(57, 15);
            lblProcessingTimeUDPAvg.TabIndex = 46;
            lblProcessingTimeUDPAvg.Text = "UDP time";
            // 
            // label49
            // 
            label49.AutoSize = true;
            label49.Location = new Point(410, 219);
            label49.Name = "label49";
            label49.Size = new Size(31, 15);
            label49.TabIndex = 45;
            label49.Text = "Avg:";
            toolTip1.SetToolTip(label49, "This is for the UDP packet processing");
            // 
            // lblProcessingTimeUIMin
            // 
            lblProcessingTimeUIMin.AutoSize = true;
            lblProcessingTimeUIMin.Location = new Point(294, 271);
            lblProcessingTimeUIMin.Name = "lblProcessingTimeUIMin";
            lblProcessingTimeUIMin.Size = new Size(45, 15);
            lblProcessingTimeUIMin.TabIndex = 44;
            lblProcessingTimeUIMin.Tag = "-1";
            lblProcessingTimeUIMin.Text = "UI time";
            toolTip1.SetToolTip(lblProcessingTimeUIMin, "Max UI processing time (monitor).  Ignores the first one.");
            // 
            // lblProcessingTimeUDPMin
            // 
            lblProcessingTimeUDPMin.AutoSize = true;
            lblProcessingTimeUDPMin.Location = new Point(294, 246);
            lblProcessingTimeUDPMin.Name = "lblProcessingTimeUDPMin";
            lblProcessingTimeUDPMin.Size = new Size(57, 15);
            lblProcessingTimeUDPMin.TabIndex = 43;
            lblProcessingTimeUDPMin.Text = "UDP time";
            // 
            // label46
            // 
            label46.AutoSize = true;
            label46.Location = new Point(294, 219);
            label46.Name = "label46";
            label46.Size = new Size(31, 15);
            label46.TabIndex = 42;
            label46.Text = "Min:";
            toolTip1.SetToolTip(label46, "This is for the UDP packet processing");
            // 
            // label44
            // 
            label44.AutoSize = true;
            label44.Location = new Point(121, 271);
            label44.Name = "label44";
            label44.Size = new Size(21, 15);
            label44.TabIndex = 41;
            label44.Text = "UI:";
            toolTip1.SetToolTip(label44, "This is for the UDP packet processing");
            // 
            // label42
            // 
            label42.AutoSize = true;
            label42.Location = new Point(177, 219);
            label42.Name = "label42";
            label42.Size = new Size(33, 15);
            label42.TabIndex = 40;
            label42.Text = "Max:";
            toolTip1.SetToolTip(label42, "This is for the UDP packet processing");
            // 
            // label41
            // 
            label41.AutoSize = true;
            label41.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label41.Location = new Point(13, 219);
            label41.Name = "label41";
            label41.Size = new Size(132, 15);
            label41.TabIndex = 39;
            label41.Text = "Processing Times (ms):";
            toolTip1.SetToolTip(label41, "This is for the UDP packet processing");
            // 
            // label40
            // 
            label40.AutoSize = true;
            label40.Location = new Point(455, 159);
            label40.Name = "label40";
            label40.Size = new Size(18, 15);
            label40.TabIndex = 38;
            label40.Tag = "0";
            label40.Text = "m";
            // 
            // lblAltitude
            // 
            lblAltitude.AutoSize = true;
            lblAltitude.Location = new Point(415, 159);
            lblAltitude.Name = "lblAltitude";
            lblAltitude.Size = new Size(34, 15);
            lblAltitude.TabIndex = 36;
            lblAltitude.Tag = "0";
            lblAltitude.Text = "none";
            lblAltitude.TextAlign = ContentAlignment.TopRight;
            // 
            // label43
            // 
            label43.AutoSize = true;
            label43.Location = new Point(354, 159);
            label43.Name = "label43";
            label43.Size = new Size(52, 15);
            label43.TabIndex = 35;
            label43.Text = "Altitude:";
            label43.TextAlign = ContentAlignment.TopRight;
            // 
            // lblGForces
            // 
            lblGForces.AutoSize = true;
            lblGForces.Location = new Point(415, 132);
            lblGForces.Name = "lblGForces";
            lblGForces.Size = new Size(34, 15);
            lblGForces.TabIndex = 34;
            lblGForces.Tag = "0";
            lblGForces.Text = "none";
            lblGForces.TextAlign = ContentAlignment.TopRight;
            // 
            // label45
            // 
            label45.AutoSize = true;
            label45.Location = new Point(349, 132);
            label45.Name = "label45";
            label45.Size = new Size(57, 15);
            label45.TabIndex = 33;
            label45.Text = "G-Forces:";
            label45.TextAlign = ContentAlignment.TopRight;
            // 
            // label37
            // 
            label37.AutoSize = true;
            label37.Location = new Point(285, 159);
            label37.Name = "label37";
            label37.Size = new Size(17, 15);
            label37.TabIndex = 32;
            label37.Tag = "0";
            label37.Text = "%";
            // 
            // label36
            // 
            label36.AutoSize = true;
            label36.Location = new Point(285, 132);
            label36.Name = "label36";
            label36.Size = new Size(17, 15);
            label36.TabIndex = 31;
            label36.Tag = "0";
            label36.Text = "%";
            // 
            // label35
            // 
            label35.AutoSize = true;
            label35.Location = new Point(116, 159);
            label35.Name = "label35";
            label35.Size = new Size(36, 15);
            label35.TabIndex = 30;
            label35.Tag = "0";
            label35.Text = "km/h";
            // 
            // lblTimestamp
            // 
            lblTimestamp.AutoSize = true;
            lblTimestamp.Location = new Point(410, 77);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.Size = new Size(64, 15);
            lblTimestamp.TabIndex = 29;
            lblTimestamp.Tag = "-1";
            lblTimestamp.Text = "timestamp";
            toolTip1.SetToolTip(lblTimestamp, "Timestamp in milliseconds of the last datagram received");
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Location = new Point(337, 77);
            label31.Name = "label31";
            label31.Size = new Size(69, 15);
            label31.TabIndex = 28;
            label31.Text = "Timestamp:";
            // 
            // lblUIThreadID
            // 
            lblUIThreadID.AutoSize = true;
            lblUIThreadID.Location = new Point(287, 11);
            lblUIThreadID.Name = "lblUIThreadID";
            lblUIThreadID.Size = new Size(27, 15);
            lblUIThreadID.TabIndex = 27;
            lblUIThreadID.Text = "----";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Location = new Point(203, 11);
            label27.Name = "label27";
            label27.Size = new Size(74, 15);
            label27.TabIndex = 26;
            label27.Text = "UI Thread ID:";
            label27.TextAlign = ContentAlignment.TopRight;
            // 
            // lblLastProcessorUsedUI
            // 
            lblLastProcessorUsedUI.AutoSize = true;
            lblLastProcessorUsedUI.Location = new Point(215, 186);
            lblLastProcessorUsedUI.Name = "lblLastProcessorUsedUI";
            lblLastProcessorUsedUI.Size = new Size(18, 15);
            lblLastProcessorUsedUI.TabIndex = 25;
            lblLastProcessorUsedUI.Tag = "-1";
            lblLastProcessorUsedUI.Text = "UI";
            toolTip1.SetToolTip(lblLastProcessorUsedUI, "Processor used to update the UI (monitor)");
            // 
            // lblProcessingTimeUIMax
            // 
            lblProcessingTimeUIMax.AutoSize = true;
            lblProcessingTimeUIMax.Location = new Point(177, 271);
            lblProcessingTimeUIMax.Name = "lblProcessingTimeUIMax";
            lblProcessingTimeUIMax.Size = new Size(45, 15);
            lblProcessingTimeUIMax.TabIndex = 24;
            lblProcessingTimeUIMax.Tag = "-1";
            lblProcessingTimeUIMax.Text = "UI time";
            toolTip1.SetToolTip(lblProcessingTimeUIMax, "Max UI processing time (monitor).  Ignores the first one.");
            // 
            // lblLastProcessorUsedUDP
            // 
            lblLastProcessorUsedUDP.AutoSize = true;
            lblLastProcessorUsedUDP.Location = new Point(148, 186);
            lblLastProcessorUsedUDP.Name = "lblLastProcessorUsedUDP";
            lblLastProcessorUsedUDP.Size = new Size(30, 15);
            lblLastProcessorUsedUDP.TabIndex = 23;
            lblLastProcessorUsedUDP.Tag = "-1";
            lblLastProcessorUsedUDP.Text = "UDP";
            toolTip1.SetToolTip(lblLastProcessorUsedUDP, "Processor used to serve the last UDP packet received");
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Location = new Point(19, 186);
            label28.Name = "label28";
            label28.Size = new Size(114, 15);
            label28.TabIndex = 22;
            label28.Text = "Last Processor Used:";
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(83, 159);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(34, 15);
            lblSpeed.TabIndex = 21;
            lblSpeed.Text = "none";
            lblSpeed.TextAlign = ContentAlignment.TopRight;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(33, 159);
            label20.Name = "label20";
            label20.Size = new Size(42, 15);
            label20.TabIndex = 20;
            label20.Text = "Speed:";
            // 
            // lblAoAUnits
            // 
            lblAoAUnits.AutoSize = true;
            lblAoAUnits.Location = new Point(116, 132);
            lblAoAUnits.Name = "lblAoAUnits";
            lblAoAUnits.Size = new Size(12, 15);
            lblAoAUnits.TabIndex = 19;
            lblAoAUnits.Tag = "0";
            lblAoAUnits.Text = "°";
            // 
            // lblUDPServerThread
            // 
            lblUDPServerThread.AutoSize = true;
            lblUDPServerThread.Location = new Point(153, 11);
            lblUDPServerThread.Name = "lblUDPServerThread";
            lblUDPServerThread.Size = new Size(22, 15);
            lblUDPServerThread.TabIndex = 18;
            lblUDPServerThread.Text = "---";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(13, 11);
            label18.Name = "label18";
            label18.Size = new Size(129, 15);
            label18.TabIndex = 17;
            label18.Text = "UDP Server - Thread ID:";
            // 
            // lblLastFlaps
            // 
            lblLastFlaps.AutoSize = true;
            lblLastFlaps.Location = new Point(245, 159);
            lblLastFlaps.Name = "lblLastFlaps";
            lblLastFlaps.Size = new Size(34, 15);
            lblLastFlaps.TabIndex = 16;
            lblLastFlaps.Tag = "0";
            lblLastFlaps.Text = "none";
            lblLastFlaps.TextAlign = ContentAlignment.TopRight;
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Location = new Point(199, 159);
            label32.Name = "label32";
            label32.Size = new Size(37, 15);
            label32.TabIndex = 15;
            label32.Text = "Flaps:";
            label32.TextAlign = ContentAlignment.TopRight;
            // 
            // lblDatagramsPerSecond
            // 
            lblDatagramsPerSecond.AutoSize = true;
            lblDatagramsPerSecond.Location = new Point(288, 40);
            lblDatagramsPerSecond.Name = "lblDatagramsPerSecond";
            lblDatagramsPerSecond.Size = new Size(49, 15);
            lblDatagramsPerSecond.TabIndex = 14;
            lblDatagramsPerSecond.Tag = "-1";
            lblDatagramsPerSecond.Text = "number";
            toolTip1.SetToolTip(lblDatagramsPerSecond, "Designed for ~10 per second.");
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.Location = new Point(243, 40);
            label29.Name = "label29";
            label29.Size = new Size(35, 15);
            label29.TabIndex = 13;
            label29.Text = "Dg/s:";
            label29.TextAlign = ContentAlignment.TopRight;
            toolTip1.SetToolTip(label29, "Datagrams received per second.");
            // 
            // lblProcessingTimeUDPMax
            // 
            lblProcessingTimeUDPMax.AutoSize = true;
            lblProcessingTimeUDPMax.Location = new Point(177, 246);
            lblProcessingTimeUDPMax.Name = "lblProcessingTimeUDPMax";
            lblProcessingTimeUDPMax.Size = new Size(57, 15);
            lblProcessingTimeUDPMax.TabIndex = 12;
            lblProcessingTimeUDPMax.Tag = "-1";
            lblProcessingTimeUDPMax.Text = "UDP time";
            toolTip1.SetToolTip(lblProcessingTimeUDPMax, "Max UDP Packet processing time.  This value is tracked for each different UnitType (aircraft).");
            // 
            // lblMaxProcessingTimeTitle
            // 
            lblMaxProcessingTimeTitle.AutoSize = true;
            lblMaxProcessingTimeTitle.Location = new Point(109, 246);
            lblMaxProcessingTimeTitle.Name = "lblMaxProcessingTimeTitle";
            lblMaxProcessingTimeTitle.Size = new Size(33, 15);
            lblMaxProcessingTimeTitle.TabIndex = 11;
            lblMaxProcessingTimeTitle.Text = "UDP:";
            toolTip1.SetToolTip(lblMaxProcessingTimeTitle, "This is for the UDP packet processing");
            // 
            // lblLastSpeedBrakes
            // 
            lblLastSpeedBrakes.AutoSize = true;
            lblLastSpeedBrakes.Location = new Point(245, 132);
            lblLastSpeedBrakes.Name = "lblLastSpeedBrakes";
            lblLastSpeedBrakes.Size = new Size(34, 15);
            lblLastSpeedBrakes.TabIndex = 10;
            lblLastSpeedBrakes.Tag = "0";
            lblLastSpeedBrakes.Text = "none";
            lblLastSpeedBrakes.TextAlign = ContentAlignment.TopRight;
            // 
            // lblCurrentUnitType
            // 
            lblCurrentUnitType.AutoSize = true;
            lblCurrentUnitType.Location = new Point(83, 106);
            lblCurrentUnitType.Name = "lblCurrentUnitType";
            lblCurrentUnitType.Size = new Size(34, 15);
            lblCurrentUnitType.TabIndex = 9;
            lblCurrentUnitType.Tag = "none yet";
            lblCurrentUnitType.Text = "none";
            // 
            // lblSoundStatus
            // 
            lblSoundStatus.AutoSize = true;
            lblSoundStatus.Location = new Point(62, 40);
            lblSoundStatus.Name = "lblSoundStatus";
            lblSoundStatus.Size = new Size(113, 15);
            lblSoundStatus.TabIndex = 3;
            lblSoundStatus.Tag = "0";
            lblSoundStatus.Text = "Not playing sounds.";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(12, 40);
            label19.Name = "label19";
            label19.Size = new Size(44, 15);
            label19.TabIndex = 2;
            label19.Text = "Sound:";
            // 
            // lblLastAoA
            // 
            lblLastAoA.AutoSize = true;
            lblLastAoA.Location = new Point(83, 132);
            lblLastAoA.Name = "lblLastAoA";
            lblLastAoA.Size = new Size(34, 15);
            lblLastAoA.TabIndex = 5;
            lblLastAoA.Tag = "0";
            lblLastAoA.Text = "none";
            lblLastAoA.TextAlign = ContentAlignment.TopRight;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(160, 132);
            label24.Name = "label24";
            label24.Size = new Size(79, 15);
            label24.TabIndex = 7;
            label24.Text = "Speed Brakes:";
            label24.TextAlign = ContentAlignment.TopRight;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(19, 106);
            label21.Name = "label21";
            label21.Size = new Size(56, 15);
            label21.TabIndex = 4;
            label21.Text = "Last Unit:";
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label25.Location = new Point(13, 77);
            label25.Name = "label25";
            label25.Size = new Size(144, 15);
            label25.TabIndex = 8;
            label25.Text = "Last Telemetry received:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(43, 132);
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
            chkShowStatistics.Location = new Point(15, 37);
            chkShowStatistics.Name = "chkShowStatistics";
            chkShowStatistics.Size = new Size(300, 19);
            chkShowStatistics.TabIndex = 9;
            chkShowStatistics.Text = "Show Statistics and additional info once per second.";
            chkShowStatistics.UseVisualStyleBackColor = true;
            chkShowStatistics.CheckedChanged += checkBox5_CheckedChanged;
            // 
            // chkChangeToMonitor
            // 
            chkChangeToMonitor.AutoSize = true;
            chkChangeToMonitor.Checked = true;
            chkChangeToMonitor.CheckState = CheckState.Checked;
            chkChangeToMonitor.Location = new Point(14, 12);
            chkChangeToMonitor.Name = "chkChangeToMonitor";
            chkChangeToMonitor.Size = new Size(298, 19);
            chkChangeToMonitor.TabIndex = 0;
            chkChangeToMonitor.Text = "Automatically change to monitor tab at server start.";
            chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // btnStartListening
            // 
            btnStartListening.Location = new Point(346, 428);
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
            btnStop.Location = new Point(462, 428);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 2;
            btnStop.Text = "&Abort";
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
            timer1.Interval = 500;
            timer1.Tick += timer1_Tick;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 456);
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
            // btnResetMax
            // 
            btnResetMax.Location = new Point(24, 428);
            btnResetMax.Name = "btnResetMax";
            btnResetMax.Size = new Size(75, 23);
            btnResetMax.TabIndex = 4;
            btnResetMax.Text = "Reset Max";
            btnResetMax.UseVisualStyleBackColor = true;
            btnResetMax.Click += btnResetMax_Click;
            // 
            // frmMain
            // 
            AcceptButton = btnStartListening;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnStop;
            ClientSize = new Size(566, 478);
            Controls.Add(btnResetMax);
            Controls.Add(statusStrip1);
            Controls.Add(btnStop);
            Controls.Add(btnStartListening);
            Controls.Add(tabs);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
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
            ((System.ComponentModel.ISupportInitialize)nudMinAltitude).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudMinSpeed).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkEffectTimeout).EndInit();
            Test.ResumeLayout(false);
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
        private Label lblLastSpeedBrakes;
        private Label label29;
        private Label lblProcessingTimeUDPMax;
        private Label lblMaxProcessingTimeTitle;
        private Label lblDatagramsPerSecond;
        private Label lblLastFlaps;
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
        private Label label18;
        private Label lblUDPServerThread;
        private Label lblAoAUnits;
        private Label lblSpeed;
        private Label label20;
        private Button btnResetMax;
        private Label label22;
        private NumericUpDown nudMinSpeed;
        private Label label26;
        private Label label28;
        private Label lblLastProcessorUsedUDP;
        private Label lblProcessingTimeUIMax;
        private Label lblLastProcessorUsedUI;
        private Label lblUIThreadID;
        private Label label27;
        private TabPage Test;
        private Button btnTestArduinoMotors;
        private Button btnTestSoundEffect1;
        private Button btnTestSoundEffect2;
        private Button btnTestTWatchDisplay;
        private Button btnTestTWatchMotor;
        private Label lblTestErrMsg;
        private Label label30;
        private ComboBox cmbPriorityClass;
        private CheckBox chkUseBackgroundProcessing;
        private CheckBox chkUseEfficiencyCoresOnly;
        private Label label31;
        private Label lblTimestamp;
        private Label label37;
        private Label label36;
        private Label label35;
        private NumericUpDown nudMinAltitude;
        private Label label38;
        private Label label39;
        private Label label40;
        private Label lblAltitude;
        private Label label43;
        private Label lblGForces;
        private Label label45;
        private Label label41;
        private Label label44;
        private Label label42;
        private Label label46;
        private Label lblProcessingTimeUIMin;
        private Label lblProcessingTimeUDPMin;
        private Label lblProcessingTimeUIAvg;
        private Label lblProcessingTimeUDPAvg;
        private Label label49;
    }
}
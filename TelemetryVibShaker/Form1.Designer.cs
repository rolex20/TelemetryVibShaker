﻿namespace TelemetryVibShaker
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
            label4 = new Label();
            chkEnableAoASoundEffects = new CheckBox();
            btnSoundEffect2 = new Button();
            btnSoundEffect1 = new Button();
            lblVolumeMultiplier = new Label();
            trkVolumeMultiplier = new TrackBar();
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
            checkBox2 = new CheckBox();
            numMaxIntensitySpeedBrakes = new NumericUpDown();
            numMinIntensitySpeedBrakes = new NumericUpDown();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            checkBox1 = new CheckBox();
            tabTTGO20V3 = new TabPage();
            checkBox4 = new CheckBox();
            checkBox3 = new CheckBox();
            textBox1 = new TextBox();
            label13 = new Label();
            textBox2 = new TextBox();
            label14 = new Label();
            label12 = new Label();
            tabSettings = new TabPage();
            btnJSONFile = new Button();
            txtJSON = new TextBox();
            label17 = new Label();
            lblEffectTimeout = new Label();
            trkEffectTimeout = new TrackBar();
            label16 = new Label();
            textBox3 = new TextBox();
            label15 = new Label();
            tabMonitor = new TabPage();
            panel1 = new Panel();
            label20 = new Label();
            label19 = new Label();
            chkShowStatistics = new CheckBox();
            label25 = new Label();
            label24 = new Label();
            label23 = new Label();
            label22 = new Label();
            label21 = new Label();
            chkChangeToMonitor = new CheckBox();
            btnStartListening = new Button();
            btnStop = new Button();
            openFileDialog1 = new OpenFileDialog();
            toolTip1 = new ToolTip(components);
            label18 = new Label();
            label26 = new Label();
            label27 = new Label();
            label28 = new Label();
            label29 = new Label();
            label30 = new Label();
            label31 = new Label();
            label32 = new Label();
            tabs.SuspendLayout();
            tabNormalSoundEffects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier).BeginInit();
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
            tabs.Size = new Size(517, 334);
            tabs.TabIndex = 0;
            // 
            // tabNormalSoundEffects
            // 
            tabNormalSoundEffects.Controls.Add(label4);
            tabNormalSoundEffects.Controls.Add(chkEnableAoASoundEffects);
            tabNormalSoundEffects.Controls.Add(btnSoundEffect2);
            tabNormalSoundEffects.Controls.Add(btnSoundEffect1);
            tabNormalSoundEffects.Controls.Add(lblVolumeMultiplier);
            tabNormalSoundEffects.Controls.Add(trkVolumeMultiplier);
            tabNormalSoundEffects.Controls.Add(label3);
            tabNormalSoundEffects.Controls.Add(txtSoundEffect2);
            tabNormalSoundEffects.Controls.Add(label2);
            tabNormalSoundEffects.Controls.Add(txtSoundEffect1);
            tabNormalSoundEffects.Controls.Add(label1);
            tabNormalSoundEffects.Location = new Point(4, 24);
            tabNormalSoundEffects.Name = "tabNormalSoundEffects";
            tabNormalSoundEffects.Padding = new Padding(3);
            tabNormalSoundEffects.Size = new Size(509, 306);
            tabNormalSoundEffects.TabIndex = 0;
            tabNormalSoundEffects.Text = "Normal AoA Sound Effects";
            tabNormalSoundEffects.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.Location = new Point(17, 15);
            label4.Name = "label4";
            label4.Size = new Size(476, 52);
            label4.TabIndex = 10;
            label4.Text = resources.GetString("label4.Text");
            // 
            // chkEnableAoASoundEffects
            // 
            chkEnableAoASoundEffects.AutoSize = true;
            chkEnableAoASoundEffects.Checked = true;
            chkEnableAoASoundEffects.CheckState = CheckState.Checked;
            chkEnableAoASoundEffects.Location = new Point(17, 78);
            chkEnableAoASoundEffects.Name = "chkEnableAoASoundEffects";
            chkEnableAoASoundEffects.Size = new Size(136, 19);
            chkEnableAoASoundEffects.TabIndex = 9;
            chkEnableAoASoundEffects.Text = "Enable Sound Effects";
            chkEnableAoASoundEffects.UseVisualStyleBackColor = true;
            // 
            // btnSoundEffect2
            // 
            btnSoundEffect2.Location = new Point(456, 191);
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
            btnSoundEffect1.Location = new Point(456, 130);
            btnSoundEffect1.Name = "btnSoundEffect1";
            btnSoundEffect1.Size = new Size(37, 23);
            btnSoundEffect1.TabIndex = 7;
            btnSoundEffect1.Tag = "WAV Files|*.wav|MP3 files|*.mp3|All files|*.*";
            btnSoundEffect1.Text = "...";
            btnSoundEffect1.UseVisualStyleBackColor = true;
            btnSoundEffect1.Click += btnSoundEffect1_Click;
            // 
            // lblVolumeMultiplier
            // 
            lblVolumeMultiplier.AutoSize = true;
            lblVolumeMultiplier.Location = new Point(456, 242);
            lblVolumeMultiplier.Name = "lblVolumeMultiplier";
            lblVolumeMultiplier.Size = new Size(111, 15);
            lblVolumeMultiplier.TabIndex = 6;
            lblVolumeMultiplier.Text = "lblVolumeMultiplier";
            // 
            // trkVolumeMultiplier
            // 
            trkVolumeMultiplier.LargeChange = 10;
            trkVolumeMultiplier.Location = new Point(124, 233);
            trkVolumeMultiplier.Maximum = 100;
            trkVolumeMultiplier.Minimum = 1;
            trkVolumeMultiplier.Name = "trkVolumeMultiplier";
            trkVolumeMultiplier.Size = new Size(322, 45);
            trkVolumeMultiplier.SmallChange = 5;
            trkVolumeMultiplier.TabIndex = 5;
            trkVolumeMultiplier.TickFrequency = 10;
            toolTip1.SetToolTip(trkVolumeMultiplier, "General modifier of volume for the sound effects.  100% is the original sound effect volume.");
            trkVolumeMultiplier.Value = 60;
            trkVolumeMultiplier.Scroll += trkVolumeMultiplier_Scroll;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(17, 242);
            label3.Name = "label3";
            label3.Size = new Size(101, 15);
            label3.TabIndex = 4;
            label3.Text = "Volume Multiplier";
            // 
            // txtSoundEffect2
            // 
            txtSoundEffect2.Location = new Point(17, 192);
            txtSoundEffect2.Name = "txtSoundEffect2";
            txtSoundEffect2.Size = new Size(429, 23);
            txtSoundEffect2.TabIndex = 3;
            txtSoundEffect2.Text = "C:\\Program Files\\Microsoft Office\\root\\Office16\\MEDIA\\APPLAUSE.WAV";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 174);
            label2.Name = "label2";
            label2.Size = new Size(255, 15);
            label2.TabIndex = 2;
            label2.Text = "Sound Effect when beyond optimal AoA range:";
            // 
            // txtSoundEffect1
            // 
            txtSoundEffect1.Location = new Point(17, 130);
            txtSoundEffect1.Name = "txtSoundEffect1";
            txtSoundEffect1.Size = new Size(429, 23);
            txtSoundEffect1.TabIndex = 1;
            txtSoundEffect1.Text = "C:\\Program Files\\Microsoft Office\\root\\Office16\\MEDIA\\APPLAUSE.WAV";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 112);
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
            tabArduino.Controls.Add(checkBox2);
            tabArduino.Controls.Add(numMaxIntensitySpeedBrakes);
            tabArduino.Controls.Add(numMinIntensitySpeedBrakes);
            tabArduino.Controls.Add(label7);
            tabArduino.Controls.Add(label6);
            tabArduino.Controls.Add(label5);
            tabArduino.Controls.Add(checkBox1);
            tabArduino.Location = new Point(4, 24);
            tabArduino.Name = "tabArduino";
            tabArduino.Padding = new Padding(3);
            tabArduino.Size = new Size(509, 306);
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
            numMaxIntensityFlaps.Value = new decimal(new int[] { 200, 0, 0, 0 });
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
            // checkBox2
            // 
            checkBox2.Location = new Point(11, 165);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(486, 41);
            checkBox2.TabIndex = 17;
            checkBox2.Text = "Vibrate Motors with Flaps Telemetry:  Attach vibration motor where you want to feel this haptic effect.";
            checkBox2.UseVisualStyleBackColor = true;
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
            numMaxIntensitySpeedBrakes.Value = new decimal(new int[] { 200, 0, 0, 0 });
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
            // checkBox1
            // 
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Location = new Point(17, 78);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(486, 41);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Vibrate Motors with Speed Brake Telemetry:  Attach vibration motor near your throttle.";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // tabTTGO20V3
            // 
            tabTTGO20V3.Controls.Add(checkBox4);
            tabTTGO20V3.Controls.Add(checkBox3);
            tabTTGO20V3.Controls.Add(textBox1);
            tabTTGO20V3.Controls.Add(label13);
            tabTTGO20V3.Controls.Add(textBox2);
            tabTTGO20V3.Controls.Add(label14);
            tabTTGO20V3.Controls.Add(label12);
            tabTTGO20V3.Location = new Point(4, 24);
            tabTTGO20V3.Name = "tabTTGO20V3";
            tabTTGO20V3.Padding = new Padding(3);
            tabTTGO20V3.Size = new Size(509, 306);
            tabTTGO20V3.TabIndex = 2;
            tabTTGO20V3.Text = "T-Watch 2020 V3";
            tabTTGO20V3.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            checkBox4.Checked = true;
            checkBox4.CheckState = CheckState.Checked;
            checkBox4.Location = new Point(17, 133);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(468, 41);
            checkBox4.TabIndex = 31;
            checkBox4.Text = "Change display background color based on AoA:  Yellow when below optimal AoA, Green when in optimal AoA and Red when above optimal AoA.";
            checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            checkBox3.Checked = true;
            checkBox3.CheckState = CheckState.Checked;
            checkBox3.Location = new Point(17, 86);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new Size(283, 41);
            checkBox3.TabIndex = 30;
            checkBox3.Text = "Vibrate Motors with AoA Telemetry:  XXX";
            checkBox3.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(338, 203);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(106, 23);
            textBox1.TabIndex = 29;
            textBox1.Text = "54671";
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
            // textBox2
            // 
            textBox2.Location = new Point(134, 203);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(106, 23);
            textBox2.TabIndex = 27;
            textBox2.Text = "192.168.1.248";
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
            tabSettings.Controls.Add(textBox3);
            tabSettings.Controls.Add(label15);
            tabSettings.Location = new Point(4, 24);
            tabSettings.Name = "tabSettings";
            tabSettings.Size = new Size(509, 306);
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
            trkEffectTimeout.Scroll += trackBar1_Scroll;
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
            // textBox3
            // 
            textBox3.Location = new Point(106, 12);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(116, 23);
            textBox3.TabIndex = 1;
            textBox3.Text = "54671";
            toolTip1.SetToolTip(textBox3, "Make sure your export.lua script sends UDP packets to this port");
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
            tabMonitor.Size = new Size(509, 306);
            tabMonitor.TabIndex = 3;
            tabMonitor.Text = "Monitor";
            tabMonitor.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(label31);
            panel1.Controls.Add(label32);
            panel1.Controls.Add(label30);
            panel1.Controls.Add(label29);
            panel1.Controls.Add(label28);
            panel1.Controls.Add(label27);
            panel1.Controls.Add(label26);
            panel1.Controls.Add(label18);
            panel1.Controls.Add(label20);
            panel1.Controls.Add(label19);
            panel1.Controls.Add(label22);
            panel1.Controls.Add(label24);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(label25);
            panel1.Controls.Add(label23);
            panel1.Location = new Point(3, 97);
            panel1.Name = "panel1";
            panel1.Size = new Size(492, 197);
            panel1.TabIndex = 10;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(61, 12);
            label20.Name = "label20";
            label20.Size = new Size(113, 15);
            label20.TabIndex = 3;
            label20.Text = "Not playing sounds.";
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
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(202, 78);
            label24.Name = "label24";
            label24.Size = new Size(79, 15);
            label24.TabIndex = 7;
            label24.Text = "Speed Brakes:";
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
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(101, 104);
            label22.Name = "label22";
            label22.Size = new Size(34, 15);
            label22.TabIndex = 5;
            label22.Text = "none";
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
            // chkChangeToMonitor
            // 
            chkChangeToMonitor.AutoSize = true;
            chkChangeToMonitor.Checked = true;
            chkChangeToMonitor.CheckState = CheckState.Checked;
            chkChangeToMonitor.Location = new Point(14, 20);
            chkChangeToMonitor.Name = "chkChangeToMonitor";
            chkChangeToMonitor.Size = new Size(333, 19);
            chkChangeToMonitor.TabIndex = 0;
            chkChangeToMonitor.Text = "Automatically change to monitor tab when Starting Server";
            chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // btnStartListening
            // 
            btnStartListening.Location = new Point(346, 378);
            btnStartListening.Name = "btnStartListening";
            btnStartListening.Size = new Size(100, 23);
            btnStartListening.TabIndex = 0;
            btnStartListening.Text = "Start &Listening";
            btnStartListening.UseVisualStyleBackColor = true;
            btnStartListening.Click += btnStartListening_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(462, 378);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 2;
            btnStop.Text = "&Stop";
            btnStop.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "WAV Files|*.wav|MP3 files|*.mp3|All files|*.*";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(101, 78);
            label18.Name = "label18";
            label18.Size = new Size(34, 15);
            label18.TabIndex = 9;
            label18.Text = "none";
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
            // label27
            // 
            label27.AutoSize = true;
            label27.Location = new Point(12, 161);
            label27.Name = "label27";
            label27.Size = new Size(123, 15);
            label27.TabIndex = 11;
            label27.Text = "Processing Time (ms):";
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Location = new Point(141, 161);
            label28.Name = "label28";
            label28.Size = new Size(44, 15);
            label28.TabIndex = 12;
            label28.Text = "label28";
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
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new Point(388, 162);
            label30.Name = "label30";
            label30.Size = new Size(44, 15);
            label30.TabIndex = 14;
            label30.Text = "label30";
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
            // frmMain
            // 
            AcceptButton = btnStartListening;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnStop;
            ClientSize = new Size(577, 422);
            Controls.Add(btnStop);
            Controls.Add(btnStartListening);
            Controls.Add(tabs);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "frmMain";
            Text = "Telemetry Vib-Sound-Shaker [UDP Server]";
            Load += frmMain_Load;
            tabs.ResumeLayout(false);
            tabNormalSoundEffects.ResumeLayout(false);
            tabNormalSoundEffects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkVolumeMultiplier).EndInit();
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
            ResumeLayout(false);
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
        private TrackBar trkVolumeMultiplier;
        private Label label3;
        private Label lblVolumeMultiplier;
        private Button btnSoundEffect2;
        private Button btnSoundEffect1;
        private CheckBox chkChangeToMonitor;
        private OpenFileDialog openFileDialog1;
        private TabPage tabSettings;
        private CheckBox chkEnableAoASoundEffects;
        private Label label4;
        private CheckBox checkBox1;
        private Label label5;
        private Label label7;
        private Label label6;
        private NumericUpDown numMinIntensitySpeedBrakes;
        private NumericUpDown numMaxIntensitySpeedBrakes;
        private ToolTip toolTip1;
        private CheckBox checkBox2;
        private NumericUpDown numMaxIntensityFlaps;
        private NumericUpDown numMinIntensityFlaps;
        private Label label8;
        private Label label9;
        private Label label10;
        private TextBox txtArduinoIP;
        private TextBox txtArduinoPort;
        private Label label11;
        private Label label12;
        private TextBox textBox1;
        private Label label13;
        private TextBox textBox2;
        private Label label14;
        private CheckBox checkBox3;
        private CheckBox checkBox4;
        private Label label15;
        private TextBox textBox3;
        private TrackBar trkEffectTimeout;
        private Label label16;
        private Label lblEffectTimeout;
        private Button btnJSONFile;
        private TextBox txtJSON;
        private Label label17;
        private Label label20;
        private Label label19;
        private Label label24;
        private Label label23;
        private Label label22;
        private Label label21;
        private Label label25;
        private CheckBox chkShowStatistics;
        private Panel panel1;
        private Label label18;
        private Label label26;
        private Label label29;
        private Label label28;
        private Label label27;
        private Label label30;
        private Label label31;
        private Label label32;
    }
}
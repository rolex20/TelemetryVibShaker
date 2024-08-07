namespace SimConnectExporter
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
            Settings = new TabPage();
            cmbSimConnectPeriod = new ComboBox();
            label13 = new Label();
            label12 = new Label();
            label8 = new Label();
            nudFrequency = new NumericUpDown();
            chkUseEfficiencyCoresOnly = new CheckBox();
            chkUseBackgroundProcessing = new CheckBox();
            cmbPriorityClass = new ComboBox();
            label30 = new Label();
            txtDestinationPort = new TextBox();
            label2 = new Label();
            label1 = new Label();
            txtDestinationHostname = new TextBox();
            Monitor = new TabPage();
            label11 = new Label();
            label10 = new Label();
            lblMaxGForce = new Label();
            label9 = new Label();
            label7 = new Label();
            label4 = new Label();
            lblAltitude = new Label();
            label6 = new Label();
            lblGforce = new Label();
            label5 = new Label();
            lblGear = new Label();
            label3 = new Label();
            lblProcessingTimeUDP = new Label();
            lblMaxProcessingTimeTitle = new Label();
            lblTimestamp = new Label();
            label31 = new Label();
            lblLastProcessorUsedUDP = new Label();
            label28 = new Label();
            lblSpeed = new Label();
            label20 = new Label();
            lblAoAUnits = new Label();
            lblLastFlaps = new Label();
            label32 = new Label();
            lblLastSpeedBrakes = new Label();
            lblCurrentUnitType = new Label();
            lblLastAoA = new Label();
            label24 = new Label();
            label21 = new Label();
            label25 = new Label();
            label23 = new Label();
            chkShowStatistics = new CheckBox();
            chkChangeToMonitor = new CheckBox();
            tbGForces = new TabPage();
            dgGForces = new DataGridView();
            AircraftName = new DataGridViewTextBoxColumn();
            MaxGForce = new DataGridViewTextBoxColumn();
            Timestamp = new DataGridViewTextBoxColumn();
            chkTrackGForces = new CheckBox();
            btnConnect = new Button();
            statusStrip1 = new StatusStrip();
            tsStatusBar1 = new ToolStripStatusLabel();
            btnDisconnect = new Button();
            btnResetMax = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            lblCallbacksPerSec = new Label();
            label15 = new Label();
            tabs.SuspendLayout();
            Settings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).BeginInit();
            Monitor.SuspendLayout();
            tbGForces.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgGForces).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(Settings);
            tabs.Controls.Add(Monitor);
            tabs.Controls.Add(tbGForces);
            tabs.Location = new Point(12, 24);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(469, 318);
            tabs.TabIndex = 0;
            // 
            // Settings
            // 
            Settings.Controls.Add(cmbSimConnectPeriod);
            Settings.Controls.Add(label13);
            Settings.Controls.Add(label12);
            Settings.Controls.Add(label8);
            Settings.Controls.Add(nudFrequency);
            Settings.Controls.Add(chkUseEfficiencyCoresOnly);
            Settings.Controls.Add(chkUseBackgroundProcessing);
            Settings.Controls.Add(cmbPriorityClass);
            Settings.Controls.Add(label30);
            Settings.Controls.Add(txtDestinationPort);
            Settings.Controls.Add(label2);
            Settings.Controls.Add(label1);
            Settings.Controls.Add(txtDestinationHostname);
            Settings.Location = new Point(4, 24);
            Settings.Name = "Settings";
            Settings.Padding = new Padding(3);
            Settings.Size = new Size(461, 290);
            Settings.TabIndex = 0;
            Settings.Text = "Settings";
            Settings.UseVisualStyleBackColor = true;
            // 
            // cmbSimConnectPeriod
            // 
            cmbSimConnectPeriod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSimConnectPeriod.FormattingEnabled = true;
            cmbSimConnectPeriod.Items.AddRange(new object[] { "SIMCONNECT_PERIOD_VISUAL_FRAME", "SIMCONNECT_PERIOD_SIM_FRAME", "SIMCONNECT_PERIOD_SECOND" });
            cmbSimConnectPeriod.Location = new Point(152, 186);
            cmbSimConnectPeriod.Name = "cmbSimConnectPeriod";
            cmbSimConnectPeriod.Size = new Size(234, 23);
            cmbSimConnectPeriod.TabIndex = 27;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(24, 189);
            label13.Name = "label13";
            label13.Size = new Size(112, 15);
            label13.TabIndex = 26;
            label13.Text = "SimConnect Period:";
            label13.TextAlign = ContentAlignment.TopRight;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(313, 147);
            label12.Name = "label12";
            label12.Size = new Size(73, 15);
            label12.TabIndex = 25;
            label12.Text = "milliseconds";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(17, 147);
            label8.Name = "label8";
            label8.Size = new Size(119, 15);
            label8.TabIndex = 24;
            label8.Text = "Telemetry Frequency:";
            label8.TextAlign = ContentAlignment.TopRight;
            // 
            // nudFrequency
            // 
            nudFrequency.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            nudFrequency.Location = new Point(152, 145);
            nudFrequency.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudFrequency.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            nudFrequency.Name = "nudFrequency";
            nudFrequency.Size = new Size(155, 23);
            nudFrequency.TabIndex = 23;
            nudFrequency.Value = new decimal(new int[] { 300, 0, 0, 0 });
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(13, 252);
            chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            chkUseEfficiencyCoresOnly.Size = new Size(260, 19);
            chkUseEfficiencyCoresOnly.TabIndex = 22;
            chkUseEfficiencyCoresOnly.Text = " detected:  Use Efficiency cores only.";
            chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            chkUseEfficiencyCoresOnly.Visible = false;
            chkUseEfficiencyCoresOnly.CheckedChanged += chkUseEfficiencyCoresOnly_CheckedChanged;
            // 
            // chkUseBackgroundProcessing
            // 
            chkUseBackgroundProcessing.AutoSize = true;
            chkUseBackgroundProcessing.Location = new Point(13, 225);
            chkUseBackgroundProcessing.Name = "chkUseBackgroundProcessing";
            chkUseBackgroundProcessing.Size = new Size(206, 19);
            chkUseBackgroundProcessing.TabIndex = 21;
            chkUseBackgroundProcessing.Text = "Use background processing mode";
            chkUseBackgroundProcessing.UseVisualStyleBackColor = true;
            chkUseBackgroundProcessing.Visible = false;
            chkUseBackgroundProcessing.CheckedChanged += chkUseBackgroundProcessing_CheckedChanged;
            // 
            // cmbPriorityClass
            // 
            cmbPriorityClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriorityClass.FormattingEnabled = true;
            cmbPriorityClass.Items.AddRange(new object[] { "NORMAL", "BELOW NORMAL", "IDLE" });
            cmbPriorityClass.Location = new Point(152, 102);
            cmbPriorityClass.Name = "cmbPriorityClass";
            cmbPriorityClass.Size = new Size(155, 23);
            cmbPriorityClass.TabIndex = 20;
            cmbPriorityClass.SelectedIndexChanged += cmbPriorityClass_SelectedIndexChanged;
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new Point(17, 105);
            label30.Name = "label30";
            label30.Size = new Size(121, 15);
            label30.TabIndex = 19;
            label30.Text = "Process Priority Class:";
            label30.TextAlign = ContentAlignment.TopRight;
            // 
            // txtDestinationPort
            // 
            txtDestinationPort.Location = new Point(152, 59);
            txtDestinationPort.Name = "txtDestinationPort";
            txtDestinationPort.Size = new Size(155, 23);
            txtDestinationPort.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(43, 62);
            label2.Name = "label2";
            label2.Size = new Size(95, 15);
            label2.TabIndex = 2;
            label2.Text = "Destination Port:";
            label2.TextAlign = ContentAlignment.TopRight;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 19);
            label1.Name = "label1";
            label1.Size = new Size(128, 15);
            label1.TabIndex = 1;
            label1.Text = "Destination Hostname:";
            label1.TextAlign = ContentAlignment.TopRight;
            // 
            // txtDestinationHostname
            // 
            txtDestinationHostname.Location = new Point(152, 16);
            txtDestinationHostname.Name = "txtDestinationHostname";
            txtDestinationHostname.Size = new Size(155, 23);
            txtDestinationHostname.TabIndex = 0;
            // 
            // Monitor
            // 
            Monitor.Controls.Add(lblCallbacksPerSec);
            Monitor.Controls.Add(label15);
            Monitor.Controls.Add(label11);
            Monitor.Controls.Add(label10);
            Monitor.Controls.Add(lblMaxGForce);
            Monitor.Controls.Add(label9);
            Monitor.Controls.Add(label7);
            Monitor.Controls.Add(label4);
            Monitor.Controls.Add(lblAltitude);
            Monitor.Controls.Add(label6);
            Monitor.Controls.Add(lblGforce);
            Monitor.Controls.Add(label5);
            Monitor.Controls.Add(lblGear);
            Monitor.Controls.Add(label3);
            Monitor.Controls.Add(lblProcessingTimeUDP);
            Monitor.Controls.Add(lblMaxProcessingTimeTitle);
            Monitor.Controls.Add(lblTimestamp);
            Monitor.Controls.Add(label31);
            Monitor.Controls.Add(lblLastProcessorUsedUDP);
            Monitor.Controls.Add(label28);
            Monitor.Controls.Add(lblSpeed);
            Monitor.Controls.Add(label20);
            Monitor.Controls.Add(lblAoAUnits);
            Monitor.Controls.Add(lblLastFlaps);
            Monitor.Controls.Add(label32);
            Monitor.Controls.Add(lblLastSpeedBrakes);
            Monitor.Controls.Add(lblCurrentUnitType);
            Monitor.Controls.Add(lblLastAoA);
            Monitor.Controls.Add(label24);
            Monitor.Controls.Add(label21);
            Monitor.Controls.Add(label25);
            Monitor.Controls.Add(label23);
            Monitor.Controls.Add(chkShowStatistics);
            Monitor.Controls.Add(chkChangeToMonitor);
            Monitor.Location = new Point(4, 24);
            Monitor.Name = "Monitor";
            Monitor.Padding = new Padding(3);
            Monitor.Size = new Size(461, 290);
            Monitor.TabIndex = 1;
            Monitor.Text = "Monitor";
            Monitor.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(284, 153);
            label11.Name = "label11";
            label11.Size = new Size(17, 15);
            label11.TabIndex = 57;
            label11.Tag = "0";
            label11.Text = "%";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(284, 126);
            label10.Name = "label10";
            label10.Size = new Size(17, 15);
            label10.TabIndex = 56;
            label10.Tag = "0";
            label10.Text = "%";
            // 
            // lblMaxGForce
            // 
            lblMaxGForce.AutoSize = true;
            lblMaxGForce.Location = new Point(395, 180);
            lblMaxGForce.Name = "lblMaxGForce";
            lblMaxGForce.Size = new Size(27, 15);
            lblMaxGForce.TabIndex = 55;
            lblMaxGForce.Tag = "-1";
            lblMaxGForce.Text = "----";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(313, 180);
            label9.Name = "label9";
            label9.Size = new Size(76, 15);
            label9.TabIndex = 54;
            label9.Text = "Max G Force:";
            label9.TextAlign = ContentAlignment.TopRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(93, 180);
            label7.Name = "label7";
            label7.Size = new Size(151, 15);
            label7.TabIndex = 53;
            label7.Text = "meters (Above the Ground)";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(93, 153);
            label4.Name = "label4";
            label4.Size = new Size(36, 15);
            label4.TabIndex = 52;
            label4.Text = "knots";
            // 
            // lblAltitude
            // 
            lblAltitude.AutoSize = true;
            lblAltitude.Location = new Point(60, 180);
            lblAltitude.Name = "lblAltitude";
            lblAltitude.Size = new Size(22, 15);
            lblAltitude.TabIndex = 51;
            lblAltitude.Tag = "-1";
            lblAltitude.Text = "---";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(4, 180);
            label6.Name = "label6";
            label6.Size = new Size(52, 15);
            label6.TabIndex = 50;
            label6.Text = "Altitude:";
            // 
            // lblGforce
            // 
            lblGforce.AutoSize = true;
            lblGforce.Location = new Point(395, 153);
            lblGforce.Name = "lblGforce";
            lblGforce.Size = new Size(27, 15);
            lblGforce.TabIndex = 49;
            lblGforce.Tag = "-1";
            lblGforce.Text = "----";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(339, 153);
            label5.Name = "label5";
            label5.Size = new Size(50, 15);
            label5.TabIndex = 48;
            label5.Text = "G Force:";
            label5.TextAlign = ContentAlignment.TopRight;
            // 
            // lblGear
            // 
            lblGear.AutoSize = true;
            lblGear.Location = new Point(395, 126);
            lblGear.Name = "lblGear";
            lblGear.Size = new Size(27, 15);
            lblGear.TabIndex = 47;
            lblGear.Tag = "-1";
            lblGear.Text = "----";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(355, 126);
            label3.Name = "label3";
            label3.Size = new Size(34, 15);
            label3.TabIndex = 46;
            label3.Text = "Gear:";
            label3.TextAlign = ContentAlignment.TopRight;
            // 
            // lblProcessingTimeUDP
            // 
            lblProcessingTimeUDP.AutoSize = true;
            lblProcessingTimeUDP.Location = new Point(170, 225);
            lblProcessingTimeUDP.Name = "lblProcessingTimeUDP";
            lblProcessingTimeUDP.Size = new Size(57, 15);
            lblProcessingTimeUDP.TabIndex = 14;
            lblProcessingTimeUDP.Tag = "-1";
            lblProcessingTimeUDP.Text = "UDP time";
            // 
            // lblMaxProcessingTimeTitle
            // 
            lblMaxProcessingTimeTitle.AutoSize = true;
            lblMaxProcessingTimeTitle.Location = new Point(15, 225);
            lblMaxProcessingTimeTitle.Name = "lblMaxProcessingTimeTitle";
            lblMaxProcessingTimeTitle.Size = new Size(149, 15);
            lblMaxProcessingTimeTitle.TabIndex = 13;
            lblMaxProcessingTimeTitle.Text = "Max Processing Time (ms):";
            // 
            // lblTimestamp
            // 
            lblTimestamp.AutoSize = true;
            lblTimestamp.Location = new Point(170, 253);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.Size = new Size(27, 15);
            lblTimestamp.TabIndex = 45;
            lblTimestamp.Tag = "-1";
            lblTimestamp.Text = "----";
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Location = new Point(95, 253);
            label31.Name = "label31";
            label31.Size = new Size(69, 15);
            label31.TabIndex = 44;
            label31.Text = "Timestamp:";
            // 
            // lblLastProcessorUsedUDP
            // 
            lblLastProcessorUsedUDP.AutoSize = true;
            lblLastProcessorUsedUDP.Location = new Point(395, 225);
            lblLastProcessorUsedUDP.Name = "lblLastProcessorUsedUDP";
            lblLastProcessorUsedUDP.Size = new Size(27, 15);
            lblLastProcessorUsedUDP.TabIndex = 43;
            lblLastProcessorUsedUDP.Tag = "-1";
            lblLastProcessorUsedUDP.Text = "----";
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Location = new Point(275, 225);
            label28.Name = "label28";
            label28.Size = new Size(114, 15);
            label28.TabIndex = 42;
            label28.Text = "Last Processor Used:";
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(60, 153);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(22, 15);
            lblSpeed.TabIndex = 41;
            lblSpeed.Tag = "-1";
            lblSpeed.Text = "---";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(14, 153);
            label20.Name = "label20";
            label20.Size = new Size(42, 15);
            label20.TabIndex = 40;
            label20.Text = "Speed:";
            // 
            // lblAoAUnits
            // 
            lblAoAUnits.AutoSize = true;
            lblAoAUnits.Location = new Point(93, 126);
            lblAoAUnits.Name = "lblAoAUnits";
            lblAoAUnits.Size = new Size(12, 15);
            lblAoAUnits.TabIndex = 39;
            lblAoAUnits.Tag = "0";
            lblAoAUnits.Text = "°";
            // 
            // lblLastFlaps
            // 
            lblLastFlaps.AutoSize = true;
            lblLastFlaps.Location = new Point(256, 153);
            lblLastFlaps.Name = "lblLastFlaps";
            lblLastFlaps.Size = new Size(22, 15);
            lblLastFlaps.TabIndex = 38;
            lblLastFlaps.Tag = "-1";
            lblLastFlaps.Text = "---";
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Location = new Point(210, 153);
            label32.Name = "label32";
            label32.Size = new Size(37, 15);
            label32.TabIndex = 37;
            label32.Text = "Flaps:";
            label32.TextAlign = ContentAlignment.TopRight;
            // 
            // lblLastSpeedBrakes
            // 
            lblLastSpeedBrakes.AutoSize = true;
            lblLastSpeedBrakes.Location = new Point(256, 126);
            lblLastSpeedBrakes.Name = "lblLastSpeedBrakes";
            lblLastSpeedBrakes.Size = new Size(22, 15);
            lblLastSpeedBrakes.TabIndex = 36;
            lblLastSpeedBrakes.Tag = "-1";
            lblLastSpeedBrakes.Text = "---";
            // 
            // lblCurrentUnitType
            // 
            lblCurrentUnitType.AutoSize = true;
            lblCurrentUnitType.Location = new Point(105, 100);
            lblCurrentUnitType.Name = "lblCurrentUnitType";
            lblCurrentUnitType.Size = new Size(22, 15);
            lblCurrentUnitType.TabIndex = 35;
            lblCurrentUnitType.Tag = "none yet";
            lblCurrentUnitType.Text = "---";
            // 
            // lblLastAoA
            // 
            lblLastAoA.AutoSize = true;
            lblLastAoA.Location = new Point(60, 126);
            lblLastAoA.Name = "lblLastAoA";
            lblLastAoA.Size = new Size(22, 15);
            lblLastAoA.TabIndex = 31;
            lblLastAoA.Tag = "-1";
            lblLastAoA.Text = "---";
            lblLastAoA.TextAlign = ContentAlignment.TopRight;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(171, 126);
            label24.Name = "label24";
            label24.Size = new Size(79, 15);
            label24.TabIndex = 33;
            label24.Text = "Speed Brakes:";
            label24.TextAlign = ContentAlignment.TopRight;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(7, 100);
            label21.Name = "label21";
            label21.Size = new Size(91, 15);
            label21.TabIndex = 30;
            label21.Text = "Last Unit Name:";
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label25.Location = new Point(16, 71);
            label25.Name = "label25";
            label25.Size = new Size(230, 15);
            label25.TabIndex = 34;
            label25.Text = "Last Telemetry received by SimConnect:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(23, 126);
            label23.Name = "label23";
            label23.Size = new Size(33, 15);
            label23.TabIndex = 32;
            label23.Text = "AoA:";
            // 
            // chkShowStatistics
            // 
            chkShowStatistics.AutoSize = true;
            chkShowStatistics.Checked = true;
            chkShowStatistics.CheckState = CheckState.Checked;
            chkShowStatistics.Location = new Point(17, 38);
            chkShowStatistics.Name = "chkShowStatistics";
            chkShowStatistics.Size = new Size(300, 19);
            chkShowStatistics.TabIndex = 11;
            chkShowStatistics.Text = "Show Statistics and additional info once per second.";
            chkShowStatistics.UseVisualStyleBackColor = true;
            // 
            // chkChangeToMonitor
            // 
            chkChangeToMonitor.AutoSize = true;
            chkChangeToMonitor.Checked = true;
            chkChangeToMonitor.CheckState = CheckState.Checked;
            chkChangeToMonitor.Location = new Point(16, 10);
            chkChangeToMonitor.Name = "chkChangeToMonitor";
            chkChangeToMonitor.Size = new Size(316, 19);
            chkChangeToMonitor.TabIndex = 10;
            chkChangeToMonitor.Text = "Automatically change to monitor tab when connected.";
            chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // tbGForces
            // 
            tbGForces.Controls.Add(dgGForces);
            tbGForces.Controls.Add(chkTrackGForces);
            tbGForces.Location = new Point(4, 24);
            tbGForces.Name = "tbGForces";
            tbGForces.Size = new Size(461, 290);
            tbGForces.TabIndex = 2;
            tbGForces.Text = "G Forces Tracker";
            tbGForces.UseVisualStyleBackColor = true;
            // 
            // dgGForces
            // 
            dgGForces.AllowUserToDeleteRows = false;
            dgGForces.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgGForces.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgGForces.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgGForces.Columns.AddRange(new DataGridViewColumn[] { AircraftName, MaxGForce, Timestamp });
            dgGForces.EditMode = DataGridViewEditMode.EditProgrammatically;
            dgGForces.Location = new Point(8, 39);
            dgGForces.Name = "dgGForces";
            dgGForces.ReadOnly = true;
            dgGForces.RowTemplate.Height = 25;
            dgGForces.Size = new Size(441, 198);
            dgGForces.TabIndex = 1;
            dgGForces.CellClick += dgGForces_CellClick;
            // 
            // AircraftName
            // 
            AircraftName.HeaderText = "Aircraft Name";
            AircraftName.Name = "AircraftName";
            AircraftName.ReadOnly = true;
            AircraftName.Width = 106;
            // 
            // MaxGForce
            // 
            MaxGForce.HeaderText = "Max G Force";
            MaxGForce.Name = "MaxGForce";
            MaxGForce.ReadOnly = true;
            MaxGForce.Width = 98;
            // 
            // Timestamp
            // 
            Timestamp.HeaderText = "Timestamp";
            Timestamp.Name = "Timestamp";
            Timestamp.ReadOnly = true;
            Timestamp.Width = 91;
            // 
            // chkTrackGForces
            // 
            chkTrackGForces.AutoSize = true;
            chkTrackGForces.Location = new Point(13, 14);
            chkTrackGForces.Name = "chkTrackGForces";
            chkTrackGForces.Size = new Size(101, 19);
            chkTrackGForces.TabIndex = 0;
            chkTrackGForces.Text = "Track G Forces";
            chkTrackGForces.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(321, 353);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(75, 23);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "&Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { tsStatusBar1 });
            statusStrip1.Location = new Point(0, 391);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(493, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // tsStatusBar1
            // 
            tsStatusBar1.Name = "tsStatusBar1";
            tsStatusBar1.Size = new Size(478, 17);
            tsStatusBar1.Spring = true;
            tsStatusBar1.Text = "Idle";
            tsStatusBar1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(402, 353);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(75, 23);
            btnDisconnect.TabIndex = 3;
            btnDisconnect.Text = "&Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // btnResetMax
            // 
            btnResetMax.Location = new Point(16, 353);
            btnResetMax.Name = "btnResetMax";
            btnResetMax.Size = new Size(75, 23);
            btnResetMax.TabIndex = 4;
            btnResetMax.Text = "&Reset Max";
            btnResetMax.UseVisualStyleBackColor = true;
            btnResetMax.Click += btnResetMax_Click;
            // 
            // timer1
            // 
            timer1.Interval = 1500;
            timer1.Tick += timer1_Tick;
            // 
            // lblCallbacksPerSec
            // 
            lblCallbacksPerSec.AutoSize = true;
            lblCallbacksPerSec.Location = new Point(395, 253);
            lblCallbacksPerSec.Name = "lblCallbacksPerSec";
            lblCallbacksPerSec.Size = new Size(27, 15);
            lblCallbacksPerSec.TabIndex = 59;
            lblCallbacksPerSec.Tag = "-1";
            lblCallbacksPerSec.Text = "----";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(307, 253);
            label15.Name = "label15";
            label15.Size = new Size(82, 15);
            label15.TabIndex = 58;
            label15.Text = "Callbacks/sec:";
            label15.TextAlign = ContentAlignment.TopRight;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(493, 413);
            Controls.Add(btnResetMax);
            Controls.Add(btnDisconnect);
            Controls.Add(statusStrip1);
            Controls.Add(btnConnect);
            Controls.Add(tabs);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "frmMain";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "MSFS-SimConnect Exporter for TelemetryVibShaker";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            tabs.ResumeLayout(false);
            Settings.ResumeLayout(false);
            Settings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).EndInit();
            Monitor.ResumeLayout(false);
            Monitor.PerformLayout();
            tbGForces.ResumeLayout(false);
            tbGForces.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgGForces).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabs;
        private TabPage Settings;
        private TabPage Monitor;
        private Button btnConnect;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tsStatusBar1;
        private Button btnDisconnect;
        private TextBox txtDestinationHostname;
        private Label label1;
        private Label label2;
        private TextBox txtDestinationPort;
        private CheckBox chkShowStatistics;
        private CheckBox chkChangeToMonitor;
        private Label lblTimestamp;
        private Label label31;
        private Label lblLastProcessorUsedUDP;
        private Label label28;
        private Label lblSpeed;
        private Label label20;
        private Label lblAoAUnits;
        private Label lblLastFlaps;
        private Label label32;
        private Label lblLastSpeedBrakes;
        private Label lblCurrentUnitType;
        private Label lblLastAoA;
        private Label label24;
        private Label label21;
        private Label label25;
        private Label label23;
        private CheckBox chkUseEfficiencyCoresOnly;
        private CheckBox chkUseBackgroundProcessing;
        private ComboBox cmbPriorityClass;
        private Label label30;
        private Label lblProcessingTimeUDP;
        private Label lblMaxProcessingTimeTitle;
        private Button btnResetMax;
        private System.Windows.Forms.Timer timer1;
        private Label lblGear;
        private Label label3;
        private Label lblGforce;
        private Label label5;
        private Label lblAltitude;
        private Label label6;
        private Label label7;
        private Label label4;
        private Label lblMaxGForce;
        private Label label9;
        private TabPage tbGForces;
        private DataGridView dgGForces;
        private CheckBox chkTrackGForces;
        private DataGridViewTextBoxColumn AircraftName;
        private DataGridViewTextBoxColumn MaxGForce;
        private DataGridViewTextBoxColumn Timestamp;
        private NumericUpDown nudFrequency;
        private Label label8;
        private Label label10;
        private Label label11;
        private Label label12;
        private ComboBox cmbSimConnectPeriod;
        private Label label13;
        private Label lblCallbacksPerSec;
        private Label label15;
    }
}

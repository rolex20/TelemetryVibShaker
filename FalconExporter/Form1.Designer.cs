namespace FalconExporter
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.txtAircraftName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.chkUseEfficiencyCoresOnly = new System.Windows.Forms.CheckBox();
            this.label15 = new System.Windows.Forms.Label();
            this.nudFrequency = new System.Windows.Forms.NumericUpDown();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.cmbPriorityClass = new System.Windows.Forms.ComboBox();
            this.txtDestinationPort = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtDestinationHostname = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tabMonitor = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblTimeStamp = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.lblMaxProcessingTime = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblAoA = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.lblVehicleType = new System.Windows.Forms.Label();
            this.lblFuel = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.lblGForces = new System.Windows.Forms.Label();
            this.lblAltitude = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblTrueAirspeed = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblSpeedBrakes = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.chkShowStatistics = new System.Windows.Forms.CheckBox();
            this.chkChangeToMonitor = new System.Windows.Forms.CheckBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsStatus = new System.Windows.Forms.ToolStripLabel();
            this.tsAircraftChange = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnResetMax = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFrequency)).BeginInit();
            this.tabMonitor.SuspendLayout();
            this.panel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabSettings);
            this.tabControl1.Controls.Add(this.tabMonitor);
            this.tabControl1.Location = new System.Drawing.Point(24, 34);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(472, 303);
            this.tabControl1.TabIndex = 0;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.txtAircraftName);
            this.tabSettings.Controls.Add(this.label5);
            this.tabSettings.Controls.Add(this.chkUseEfficiencyCoresOnly);
            this.tabSettings.Controls.Add(this.label15);
            this.tabSettings.Controls.Add(this.nudFrequency);
            this.tabSettings.Controls.Add(this.label14);
            this.tabSettings.Controls.Add(this.label13);
            this.tabSettings.Controls.Add(this.cmbPriorityClass);
            this.tabSettings.Controls.Add(this.txtDestinationPort);
            this.tabSettings.Controls.Add(this.label12);
            this.tabSettings.Controls.Add(this.txtDestinationHostname);
            this.tabSettings.Controls.Add(this.label10);
            this.tabSettings.Location = new System.Drawing.Point(4, 22);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(464, 277);
            this.tabSettings.TabIndex = 0;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // txtAircraftName
            // 
            this.txtAircraftName.Location = new System.Drawing.Point(152, 192);
            this.txtAircraftName.Name = "txtAircraftName";
            this.txtAircraftName.Size = new System.Drawing.Size(161, 20);
            this.txtAircraftName.TabIndex = 11;
            this.toolTip1.SetToolTip(this.txtAircraftName, "Always send this aircraft name when new mission is started.");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(32, 195);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Aircraft Name to Send:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            this.chkUseEfficiencyCoresOnly.AutoSize = true;
            this.chkUseEfficiencyCoresOnly.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUseEfficiencyCoresOnly.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(155)))), ((int)(((byte)(213)))));
            this.chkUseEfficiencyCoresOnly.Location = new System.Drawing.Point(20, 242);
            this.chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            this.chkUseEfficiencyCoresOnly.Size = new System.Drawing.Size(290, 19);
            this.chkUseEfficiencyCoresOnly.TabIndex = 9;
            this.chkUseEfficiencyCoresOnly.Text = "Use Efficiency cores only on Intel 12700K";
            this.chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            this.chkUseEfficiencyCoresOnly.Visible = false;
            this.chkUseEfficiencyCoresOnly.CheckedChanged += new System.EventHandler(this.chkUseEfficiencyCoresOnly_CheckedChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(319, 151);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(63, 13);
            this.label15.TabIndex = 8;
            this.label15.Text = "milliseconds";
            this.label15.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // nudFrequency
            // 
            this.nudFrequency.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudFrequency.Location = new System.Drawing.Point(152, 149);
            this.nudFrequency.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudFrequency.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudFrequency.Name = "nudFrequency";
            this.nudFrequency.Size = new System.Drawing.Size(161, 20);
            this.nudFrequency.TabIndex = 7;
            this.toolTip1.SetToolTip(this.nudFrequency, "Values of 50 ms or less are too much for the timer control.");
            this.nudFrequency.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudFrequency.ValueChanged += new System.EventHandler(this.nudFrequency_ValueChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(37, 151);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(109, 13);
            this.label14.TabIndex = 6;
            this.label14.Text = "Telemetry Frequency:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(77, 107);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(69, 13);
            this.label13.TabIndex = 5;
            this.label13.Text = "Priority Class:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // cmbPriorityClass
            // 
            this.cmbPriorityClass.FormattingEnabled = true;
            this.cmbPriorityClass.Items.AddRange(new object[] {
            "NORMAL",
            "BELOW NORMAL",
            "IDLE"});
            this.cmbPriorityClass.Location = new System.Drawing.Point(152, 104);
            this.cmbPriorityClass.Name = "cmbPriorityClass";
            this.cmbPriorityClass.Size = new System.Drawing.Size(161, 21);
            this.cmbPriorityClass.TabIndex = 4;
            this.cmbPriorityClass.SelectedIndexChanged += new System.EventHandler(this.cmbPriorityClass_SelectedIndexChanged);
            // 
            // txtDestinationPort
            // 
            this.txtDestinationPort.Location = new System.Drawing.Point(152, 59);
            this.txtDestinationPort.Name = "txtDestinationPort";
            this.txtDestinationPort.Size = new System.Drawing.Size(161, 20);
            this.txtDestinationPort.TabIndex = 3;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(61, 62);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(85, 13);
            this.label12.TabIndex = 2;
            this.label12.Text = "Destination Port:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtDestinationHostname
            // 
            this.txtDestinationHostname.Location = new System.Drawing.Point(152, 19);
            this.txtDestinationHostname.Name = "txtDestinationHostname";
            this.txtDestinationHostname.Size = new System.Drawing.Size(161, 20);
            this.txtDestinationHostname.TabIndex = 1;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(17, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(129, 13);
            this.label10.TabIndex = 0;
            this.label10.Text = "Destination Hostname/IP:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tabMonitor
            // 
            this.tabMonitor.Controls.Add(this.panel1);
            this.tabMonitor.Controls.Add(this.label16);
            this.tabMonitor.Controls.Add(this.chkShowStatistics);
            this.tabMonitor.Controls.Add(this.chkChangeToMonitor);
            this.tabMonitor.Location = new System.Drawing.Point(4, 22);
            this.tabMonitor.Name = "tabMonitor";
            this.tabMonitor.Padding = new System.Windows.Forms.Padding(3);
            this.tabMonitor.Size = new System.Drawing.Size(464, 277);
            this.tabMonitor.TabIndex = 1;
            this.tabMonitor.Text = "Monitor";
            this.tabMonitor.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblTimeStamp);
            this.panel1.Controls.Add(this.label23);
            this.panel1.Controls.Add(this.label22);
            this.panel1.Controls.Add(this.label21);
            this.panel1.Controls.Add(this.label20);
            this.panel1.Controls.Add(this.label19);
            this.panel1.Controls.Add(this.label18);
            this.panel1.Controls.Add(this.lblMaxProcessingTime);
            this.panel1.Controls.Add(this.label17);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.lblAoA);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.lblSpeed);
            this.panel1.Controls.Add(this.lblVehicleType);
            this.panel1.Controls.Add(this.lblFuel);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.lblGForces);
            this.panel1.Controls.Add(this.lblAltitude);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.lblTrueAirspeed);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.lblSpeedBrakes);
            this.panel1.Location = new System.Drawing.Point(6, 100);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(452, 156);
            this.panel1.TabIndex = 23;
            // 
            // lblTimeStamp
            // 
            this.lblTimeStamp.AutoSize = true;
            this.lblTimeStamp.Location = new System.Drawing.Point(377, 130);
            this.lblTimeStamp.Name = "lblTimeStamp";
            this.lblTimeStamp.Size = new System.Drawing.Size(16, 13);
            this.lblTimeStamp.TabIndex = 28;
            this.lblTimeStamp.Text = "---";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(288, 130);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(84, 13);
            this.label23.TabIndex = 27;
            this.label23.Text = "Last Timestamp:";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(102, 56);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(25, 13);
            this.label22.TabIndex = 26;
            this.label22.Text = "feet";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(237, 130);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(20, 13);
            this.label21.TabIndex = 25;
            this.label21.Text = "ms";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(414, 56);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(33, 13);
            this.label20.TabIndex = 24;
            this.label20.Text = "knots";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(243, 20);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(15, 13);
            this.label19.TabIndex = 23;
            this.label19.Text = "%";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(93, 20);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(11, 13);
            this.label18.TabIndex = 22;
            this.label18.Text = "°";
            // 
            // lblMaxProcessingTime
            // 
            this.lblMaxProcessingTime.AutoSize = true;
            this.lblMaxProcessingTime.Location = new System.Drawing.Point(213, 130);
            this.lblMaxProcessingTime.Name = "lblMaxProcessingTime";
            this.lblMaxProcessingTime.Size = new System.Drawing.Size(16, 13);
            this.lblMaxProcessingTime.TabIndex = 21;
            this.lblMaxProcessingTime.Text = "---";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(96, 130);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(111, 13);
            this.label17.TabIndex = 20;
            this.label17.Text = "Max Processing Time:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "AoA:";
            // 
            // lblAoA
            // 
            this.lblAoA.AutoSize = true;
            this.lblAoA.Location = new System.Drawing.Point(66, 20);
            this.lblAoA.Name = "lblAoA";
            this.lblAoA.Size = new System.Drawing.Size(16, 13);
            this.lblAoA.TabIndex = 1;
            this.lblAoA.Text = "---";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(173, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "KIAS:";
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(213, 56);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(16, 13);
            this.lblSpeed.TabIndex = 3;
            this.lblSpeed.Text = "---";
            // 
            // lblVehicleType
            // 
            this.lblVehicleType.AutoSize = true;
            this.lblVehicleType.Location = new System.Drawing.Point(213, 94);
            this.lblVehicleType.Name = "lblVehicleType";
            this.lblVehicleType.Size = new System.Drawing.Size(16, 13);
            this.lblVehicleType.TabIndex = 17;
            this.lblVehicleType.Text = "---";
            // 
            // lblFuel
            // 
            this.lblFuel.AutoSize = true;
            this.lblFuel.Location = new System.Drawing.Point(377, 20);
            this.lblFuel.Name = "lblFuel";
            this.lblFuel.Size = new System.Drawing.Size(16, 13);
            this.lblFuel.TabIndex = 19;
            this.lblFuel.Text = "---";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(135, 94);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(72, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Vehicle Type:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Altitude:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(342, 20);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(30, 13);
            this.label11.TabIndex = 18;
            this.label11.Text = "Fuel:";
            // 
            // lblGForces
            // 
            this.lblGForces.AutoSize = true;
            this.lblGForces.Location = new System.Drawing.Point(66, 94);
            this.lblGForces.Name = "lblGForces";
            this.lblGForces.Size = new System.Drawing.Size(16, 13);
            this.lblGForces.TabIndex = 13;
            this.lblGForces.Text = "---";
            // 
            // lblAltitude
            // 
            this.lblAltitude.AutoSize = true;
            this.lblAltitude.Location = new System.Drawing.Point(66, 56);
            this.lblAltitude.Name = "lblAltitude";
            this.lblAltitude.Size = new System.Drawing.Size(25, 13);
            this.lblAltitude.TabIndex = 5;
            this.lblAltitude.Text = "------";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 94);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "G Forces:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(296, 56);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "True Airspeed:";
            // 
            // lblTrueAirspeed
            // 
            this.lblTrueAirspeed.AutoSize = true;
            this.lblTrueAirspeed.Location = new System.Drawing.Point(377, 56);
            this.lblTrueAirspeed.Name = "lblTrueAirspeed";
            this.lblTrueAirspeed.Size = new System.Drawing.Size(16, 13);
            this.lblTrueAirspeed.TabIndex = 9;
            this.lblTrueAirspeed.Text = "---";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(131, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(76, 13);
            this.label7.TabIndex = 10;
            this.label7.Text = "Speed brakes:";
            // 
            // lblSpeedBrakes
            // 
            this.lblSpeedBrakes.AutoSize = true;
            this.lblSpeedBrakes.Location = new System.Drawing.Point(213, 19);
            this.lblSpeedBrakes.Name = "lblSpeedBrakes";
            this.lblSpeedBrakes.Size = new System.Drawing.Size(16, 13);
            this.lblSpeedBrakes.TabIndex = 11;
            this.lblSpeedBrakes.Text = "---";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(10, 83);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(225, 13);
            this.label16.TabIndex = 22;
            this.label16.Text = "Last Telemetry shared by Falcon BMS:";
            // 
            // chkShowStatistics
            // 
            this.chkShowStatistics.AutoSize = true;
            this.chkShowStatistics.Location = new System.Drawing.Point(13, 43);
            this.chkShowStatistics.Name = "chkShowStatistics";
            this.chkShowStatistics.Size = new System.Drawing.Size(268, 17);
            this.chkShowStatistics.TabIndex = 21;
            this.chkShowStatistics.Text = "Show Statistics and additional info during each poll.";
            this.chkShowStatistics.UseVisualStyleBackColor = true;
            this.chkShowStatistics.CheckedChanged += new System.EventHandler(this.chkShowStatistics_CheckedChanged);
            // 
            // chkChangeToMonitor
            // 
            this.chkChangeToMonitor.AutoSize = true;
            this.chkChangeToMonitor.Location = new System.Drawing.Point(13, 20);
            this.chkChangeToMonitor.Name = "chkChangeToMonitor";
            this.chkChangeToMonitor.Size = new System.Drawing.Size(280, 17);
            this.chkChangeToMonitor.TabIndex = 20;
            this.chkChangeToMonitor.Text = "Automatically change to monitor tab when connected.";
            this.chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(335, 343);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Connect";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(417, 343);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 3;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsStatus,
            this.tsAircraftChange,
            this.toolStripSeparator1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 375);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(512, 25);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsStatus
            // 
            this.tsStatus.Name = "tsStatus";
            this.tsStatus.Size = new System.Drawing.Size(26, 22);
            this.tsStatus.Tag = "none";
            this.tsStatus.Text = "Idle";
            // 
            // tsAircraftChange
            // 
            this.tsAircraftChange.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsAircraftChange.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tsAircraftChange.Name = "tsAircraftChange";
            this.tsAircraftChange.Size = new System.Drawing.Size(28, 22);
            this.tsAircraftChange.Text = "---";
            this.tsAircraftChange.ToolTipText = "Time stamp of the last time the aircraft name string was sent via UDP";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnResetMax
            // 
            this.btnResetMax.Location = new System.Drawing.Point(28, 343);
            this.btnResetMax.Name = "btnResetMax";
            this.btnResetMax.Size = new System.Drawing.Size(75, 23);
            this.btnResetMax.TabIndex = 5;
            this.btnResetMax.Text = "Reset Max";
            this.btnResetMax.UseVisualStyleBackColor = true;
            this.btnResetMax.Click += new System.EventHandler(this.btnResetMax_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 400);
            this.Controls.Add(this.btnResetMax);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Falcon 4 - Telemetry Exporter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFrequency)).EndInit();
            this.tabMonitor.ResumeLayout(false);
            this.tabMonitor.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.TabPage tabMonitor;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblAoA;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblAltitude;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblTrueAirspeed;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblSpeedBrakes;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblGForces;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblVehicleType;
        private System.Windows.Forms.Label lblFuel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtDestinationHostname;
        private System.Windows.Forms.TextBox txtDestinationPort;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cmbPriorityClass;
        private System.Windows.Forms.NumericUpDown nudFrequency;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.CheckBox chkUseEfficiencyCoresOnly;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.CheckBox chkChangeToMonitor;
        private System.Windows.Forms.CheckBox chkShowStatistics;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtAircraftName;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblMaxProcessingTime;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel tsStatus;
        private System.Windows.Forms.Label lblTimeStamp;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Button btnResetMax;
        private System.Windows.Forms.ToolStripLabel tsAircraftChange;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}


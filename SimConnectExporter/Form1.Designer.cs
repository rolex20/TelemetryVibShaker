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
            tabs = new TabControl();
            Settings = new TabPage();
            chkUseEfficiencyCoresOnly = new CheckBox();
            chkUseBackgroundProcessing = new CheckBox();
            cmbPriorityClass = new ComboBox();
            label30 = new Label();
            txtDestinationPort = new TextBox();
            label2 = new Label();
            label1 = new Label();
            txtDestinationHostname = new TextBox();
            Monitor = new TabPage();
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
            btnConnect = new Button();
            statusStrip1 = new StatusStrip();
            tsStatusBar1 = new ToolStripStatusLabel();
            btnDisconnect = new Button();
            tabs.SuspendLayout();
            Settings.SuspendLayout();
            Monitor.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(Settings);
            tabs.Controls.Add(Monitor);
            tabs.Location = new Point(21, 32);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(443, 237);
            tabs.TabIndex = 0;
            // 
            // Settings
            // 
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
            Settings.Size = new Size(435, 209);
            Settings.TabIndex = 0;
            Settings.Text = "Settings";
            Settings.UseVisualStyleBackColor = true;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(13, 174);
            chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            chkUseEfficiencyCoresOnly.Size = new Size(290, 19);
            chkUseEfficiencyCoresOnly.TabIndex = 22;
            chkUseEfficiencyCoresOnly.Text = "Use Efficiency cores only on Intel 12700K";
            chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            chkUseEfficiencyCoresOnly.Visible = false;
            chkUseEfficiencyCoresOnly.CheckedChanged += chkUseEfficiencyCoresOnly_CheckedChanged;
            // 
            // chkUseBackgroundProcessing
            // 
            chkUseBackgroundProcessing.AutoSize = true;
            chkUseBackgroundProcessing.Location = new Point(13, 147);
            chkUseBackgroundProcessing.Name = "chkUseBackgroundProcessing";
            chkUseBackgroundProcessing.Size = new Size(206, 19);
            chkUseBackgroundProcessing.TabIndex = 21;
            chkUseBackgroundProcessing.Text = "Use background processing mode";
            chkUseBackgroundProcessing.UseVisualStyleBackColor = true;
            chkUseBackgroundProcessing.CheckedChanged += chkUseBackgroundProcessing_CheckedChanged;
            // 
            // cmbPriorityClass
            // 
            cmbPriorityClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriorityClass.FormattingEnabled = true;
            cmbPriorityClass.Items.AddRange(new object[] { "NORMAL", "BELOW NORMAL", "IDLE" });
            cmbPriorityClass.Location = new Point(140, 110);
            cmbPriorityClass.Name = "cmbPriorityClass";
            cmbPriorityClass.Size = new Size(121, 23);
            cmbPriorityClass.TabIndex = 20;
            cmbPriorityClass.SelectedIndexChanged += cmbPriorityClass_SelectedIndexChanged;
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new Point(13, 113);
            label30.Name = "label30";
            label30.Size = new Size(121, 15);
            label30.TabIndex = 19;
            label30.Text = "Process Priority Class:";
            // 
            // txtDestinationPort
            // 
            txtDestinationPort.Location = new Point(140, 67);
            txtDestinationPort.Name = "txtDestinationPort";
            txtDestinationPort.Size = new Size(131, 23);
            txtDestinationPort.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(39, 70);
            label2.Name = "label2";
            label2.Size = new Size(95, 15);
            label2.TabIndex = 2;
            label2.Text = "Destination Port:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 27);
            label1.Name = "label1";
            label1.Size = new Size(128, 15);
            label1.TabIndex = 1;
            label1.Text = "Destination Hostname:";
            // 
            // txtDestinationHostname
            // 
            txtDestinationHostname.Location = new Point(140, 24);
            txtDestinationHostname.Name = "txtDestinationHostname";
            txtDestinationHostname.Size = new Size(131, 23);
            txtDestinationHostname.TabIndex = 0;
            // 
            // Monitor
            // 
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
            Monitor.Size = new Size(435, 209);
            Monitor.TabIndex = 1;
            Monitor.Text = "Monitor";
            Monitor.UseVisualStyleBackColor = true;
            // 
            // lblProcessingTimeUDP
            // 
            lblProcessingTimeUDP.AutoSize = true;
            lblProcessingTimeUDP.Location = new Point(170, 182);
            lblProcessingTimeUDP.Name = "lblProcessingTimeUDP";
            lblProcessingTimeUDP.Size = new Size(57, 15);
            lblProcessingTimeUDP.TabIndex = 14;
            lblProcessingTimeUDP.Tag = "0";
            lblProcessingTimeUDP.Text = "UDP time";
            // 
            // lblMaxProcessingTimeTitle
            // 
            lblMaxProcessingTimeTitle.AutoSize = true;
            lblMaxProcessingTimeTitle.Location = new Point(15, 182);
            lblMaxProcessingTimeTitle.Name = "lblMaxProcessingTimeTitle";
            lblMaxProcessingTimeTitle.Size = new Size(149, 15);
            lblMaxProcessingTimeTitle.TabIndex = 13;
            lblMaxProcessingTimeTitle.Text = "Max Processing Time (ms):";
            // 
            // lblTimestamp
            // 
            lblTimestamp.AutoSize = true;
            lblTimestamp.Location = new Point(392, 126);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.Size = new Size(27, 15);
            lblTimestamp.TabIndex = 45;
            lblTimestamp.Tag = "-1";
            lblTimestamp.Text = "----";
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Location = new Point(317, 126);
            label31.Name = "label31";
            label31.Size = new Size(69, 15);
            label31.TabIndex = 44;
            label31.Text = "Timestamp:";
            // 
            // lblLastProcessorUsedUDP
            // 
            lblLastProcessorUsedUDP.AutoSize = true;
            lblLastProcessorUsedUDP.Location = new Point(392, 182);
            lblLastProcessorUsedUDP.Name = "lblLastProcessorUsedUDP";
            lblLastProcessorUsedUDP.Size = new Size(27, 15);
            lblLastProcessorUsedUDP.TabIndex = 43;
            lblLastProcessorUsedUDP.Text = "----";
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Location = new Point(272, 182);
            label28.Name = "label28";
            label28.Size = new Size(114, 15);
            label28.TabIndex = 42;
            label28.Text = "Last Processor Used:";
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(105, 153);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(22, 15);
            lblSpeed.TabIndex = 41;
            lblSpeed.Text = "---";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(55, 153);
            label20.Name = "label20";
            label20.Size = new Size(42, 15);
            label20.TabIndex = 40;
            label20.Text = "Speed:";
            // 
            // lblAoAUnits
            // 
            lblAoAUnits.AutoSize = true;
            lblAoAUnits.Location = new Point(138, 126);
            lblAoAUnits.Name = "lblAoAUnits";
            lblAoAUnits.Size = new Size(12, 15);
            lblAoAUnits.TabIndex = 39;
            lblAoAUnits.Tag = "0";
            lblAoAUnits.Text = "°";
            // 
            // lblLastFlaps
            // 
            lblLastFlaps.AutoSize = true;
            lblLastFlaps.Location = new Point(266, 153);
            lblLastFlaps.Name = "lblLastFlaps";
            lblLastFlaps.Size = new Size(22, 15);
            lblLastFlaps.TabIndex = 38;
            lblLastFlaps.Tag = "0";
            lblLastFlaps.Text = "---";
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Location = new Point(220, 153);
            label32.Name = "label32";
            label32.Size = new Size(37, 15);
            label32.TabIndex = 37;
            label32.Text = "Flaps:";
            label32.TextAlign = ContentAlignment.TopRight;
            // 
            // lblLastSpeedBrakes
            // 
            lblLastSpeedBrakes.AutoSize = true;
            lblLastSpeedBrakes.Location = new Point(266, 126);
            lblLastSpeedBrakes.Name = "lblLastSpeedBrakes";
            lblLastSpeedBrakes.Size = new Size(22, 15);
            lblLastSpeedBrakes.TabIndex = 36;
            lblLastSpeedBrakes.Tag = "0";
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
            lblLastAoA.Location = new Point(105, 126);
            lblLastAoA.Name = "lblLastAoA";
            lblLastAoA.Size = new Size(22, 15);
            lblLastAoA.TabIndex = 31;
            lblLastAoA.Tag = "0";
            lblLastAoA.Text = "---";
            lblLastAoA.TextAlign = ContentAlignment.TopRight;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(181, 126);
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
            label25.Size = new Size(220, 15);
            label25.TabIndex = 34;
            label25.Text = "Last Telemetry received by export.lua:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(65, 126);
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
            // btnConnect
            // 
            btnConnect.Location = new Point(299, 275);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(75, 23);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { tsStatusBar1 });
            statusStrip1.Location = new Point(0, 314);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(478, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // tsStatusBar1
            // 
            tsStatusBar1.Name = "tsStatusBar1";
            tsStatusBar1.Size = new Size(39, 17);
            tsStatusBar1.Text = "Status";
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(385, 275);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(75, 23);
            btnDisconnect.TabIndex = 3;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 336);
            Controls.Add(btnDisconnect);
            Controls.Add(statusStrip1);
            Controls.Add(btnConnect);
            Controls.Add(tabs);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "frmMain";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "MSFS-SimConnect Exporter for TelemetryVibShaker";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            tabs.ResumeLayout(false);
            Settings.ResumeLayout(false);
            Settings.PerformLayout();
            Monitor.ResumeLayout(false);
            Monitor.PerformLayout();
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
    }
}

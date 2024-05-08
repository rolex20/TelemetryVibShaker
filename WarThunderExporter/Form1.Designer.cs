namespace WarThunderExporter
{
    partial class frmWarThunderTelemetry
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmWarThunderTelemetry));
            tabControl1 = new TabControl();
            tabSettings = new TabPage();
            txtWtUrl = new TextBox();
            label5 = new Label();
            nudFrequency = new NumericUpDown();
            label4 = new Label();
            cmbPriorityClass = new ComboBox();
            label3 = new Label();
            txtDestinationPort = new TextBox();
            label2 = new Label();
            txtDestinationHostname = new TextBox();
            label1 = new Label();
            chkUseEfficiencyCoresOnly = new CheckBox();
            tabMonitor = new TabPage();
            panel1 = new Panel();
            lblGForces = new Label();
            label15 = new Label();
            lblLastTimeStamp = new Label();
            label14 = new Label();
            lblMaxProcessingTime = new Label();
            label12 = new Label();
            lblFlaps = new Label();
            lblAltitude = new Label();
            lblAircraftType = new Label();
            lblSpeedBrakes = new Label();
            lblSpeed = new Label();
            lblAoA = new Label();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            chkShowStatistics = new CheckBox();
            chkChangeToMonitor = new CheckBox();
            tabDebug = new TabPage();
            lblDebugTitle = new Label();
            txtDebug = new TextBox();
            btnStart = new Button();
            btnStop = new Button();
            btnResetMax = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            toolStrip1 = new ToolStrip();
            tsStatus = new ToolStripLabel();
            tabControl1.SuspendLayout();
            tabSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).BeginInit();
            tabMonitor.SuspendLayout();
            panel1.SuspendLayout();
            tabDebug.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabSettings);
            tabControl1.Controls.Add(tabMonitor);
            tabControl1.Controls.Add(tabDebug);
            tabControl1.Location = new Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(474, 335);
            tabControl1.TabIndex = 0;
            // 
            // tabSettings
            // 
            tabSettings.Controls.Add(txtWtUrl);
            tabSettings.Controls.Add(label5);
            tabSettings.Controls.Add(nudFrequency);
            tabSettings.Controls.Add(label4);
            tabSettings.Controls.Add(cmbPriorityClass);
            tabSettings.Controls.Add(label3);
            tabSettings.Controls.Add(txtDestinationPort);
            tabSettings.Controls.Add(label2);
            tabSettings.Controls.Add(txtDestinationHostname);
            tabSettings.Controls.Add(label1);
            tabSettings.Controls.Add(chkUseEfficiencyCoresOnly);
            tabSettings.Location = new Point(4, 24);
            tabSettings.Name = "tabSettings";
            tabSettings.Padding = new Padding(3);
            tabSettings.Size = new Size(466, 307);
            tabSettings.TabIndex = 0;
            tabSettings.Text = "Settings";
            tabSettings.UseVisualStyleBackColor = true;
            // 
            // txtWtUrl
            // 
            txtWtUrl.Location = new Point(178, 196);
            txtWtUrl.Name = "txtWtUrl";
            txtWtUrl.Size = new Size(177, 23);
            txtWtUrl.TabIndex = 10;
            txtWtUrl.TextChanged += txtWtUrl_TextChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(10, 199);
            label5.Name = "label5";
            label5.Size = new Size(156, 15);
            label5.TabIndex = 9;
            label5.Text = "War Thunder Telemetry URL:";
            label5.TextAlign = ContentAlignment.MiddleRight;
            // 
            // nudFrequency
            // 
            nudFrequency.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            nudFrequency.Location = new Point(178, 151);
            nudFrequency.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudFrequency.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudFrequency.Name = "nudFrequency";
            nudFrequency.Size = new Size(177, 23);
            nudFrequency.TabIndex = 8;
            nudFrequency.Value = new decimal(new int[] { 50, 0, 0, 0 });
            nudFrequency.ValueChanged += nudFrequency_ValueChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(7, 153);
            label4.Name = "label4";
            label4.Size = new Size(159, 15);
            label4.TabIndex = 7;
            label4.Text = "Telemetry Polling Frequency:";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbPriorityClass
            // 
            cmbPriorityClass.FormattingEnabled = true;
            cmbPriorityClass.Items.AddRange(new object[] { "NORMAL", "BELOW NORMAL", "IDLE" });
            cmbPriorityClass.Location = new Point(178, 106);
            cmbPriorityClass.Name = "cmbPriorityClass";
            cmbPriorityClass.Size = new Size(177, 23);
            cmbPriorityClass.TabIndex = 6;
            cmbPriorityClass.SelectedIndexChanged += cmbPriorityClass_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(88, 109);
            label3.Name = "label3";
            label3.Size = new Size(78, 15);
            label3.TabIndex = 5;
            label3.Text = "Priority Class:";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtDestinationPort
            // 
            txtDestinationPort.Location = new Point(178, 57);
            txtDestinationPort.Name = "txtDestinationPort";
            txtDestinationPort.Size = new Size(177, 23);
            txtDestinationPort.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(71, 60);
            label2.Name = "label2";
            label2.Size = new Size(95, 15);
            label2.TabIndex = 3;
            label2.Text = "Destination Port:";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtDestinationHostname
            // 
            txtDestinationHostname.Location = new Point(178, 17);
            txtDestinationHostname.Name = "txtDestinationHostname";
            txtDestinationHostname.Size = new Size(177, 23);
            txtDestinationHostname.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 20);
            label1.Name = "label1";
            label1.Size = new Size(143, 15);
            label1.TabIndex = 1;
            label1.Text = "Destination Hostname/IP:";
            label1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(37, 245);
            chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            chkUseEfficiencyCoresOnly.Size = new Size(290, 19);
            chkUseEfficiencyCoresOnly.TabIndex = 0;
            chkUseEfficiencyCoresOnly.Text = "Use Efficiency cores only on Intel 12700K";
            chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            chkUseEfficiencyCoresOnly.Visible = false;
            chkUseEfficiencyCoresOnly.CheckedChanged += chkUseEfficiencyCoresOnly_CheckedChanged;
            // 
            // tabMonitor
            // 
            tabMonitor.Controls.Add(panel1);
            tabMonitor.Controls.Add(chkShowStatistics);
            tabMonitor.Controls.Add(chkChangeToMonitor);
            tabMonitor.Location = new Point(4, 24);
            tabMonitor.Name = "tabMonitor";
            tabMonitor.Padding = new Padding(3);
            tabMonitor.Size = new Size(466, 307);
            tabMonitor.TabIndex = 1;
            tabMonitor.Text = "Monitor";
            tabMonitor.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(lblGForces);
            panel1.Controls.Add(label15);
            panel1.Controls.Add(lblLastTimeStamp);
            panel1.Controls.Add(label14);
            panel1.Controls.Add(lblMaxProcessingTime);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(lblFlaps);
            panel1.Controls.Add(lblAltitude);
            panel1.Controls.Add(lblAircraftType);
            panel1.Controls.Add(lblSpeedBrakes);
            panel1.Controls.Add(lblSpeed);
            panel1.Controls.Add(lblAoA);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label6);
            panel1.Location = new Point(15, 83);
            panel1.Name = "panel1";
            panel1.Size = new Size(432, 207);
            panel1.TabIndex = 2;
            // 
            // lblGForces
            // 
            lblGForces.AutoSize = true;
            lblGForces.Location = new Point(318, 19);
            lblGForces.Name = "lblGForces";
            lblGForces.Size = new Size(27, 15);
            lblGForces.TabIndex = 15;
            lblGForces.Text = "----";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(254, 19);
            label15.Name = "label15";
            label15.Size = new Size(55, 15);
            label15.TabIndex = 14;
            label15.Text = "G Forces:";
            label15.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblLastTimeStamp
            // 
            lblLastTimeStamp.AutoSize = true;
            lblLastTimeStamp.Location = new Point(354, 168);
            lblLastTimeStamp.Name = "lblLastTimeStamp";
            lblLastTimeStamp.Size = new Size(27, 15);
            lblLastTimeStamp.TabIndex = 13;
            lblLastTimeStamp.Text = "----";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(251, 168);
            label14.Name = "label14";
            label14.Size = new Size(97, 15);
            label14.TabIndex = 12;
            label14.Text = "Last Time Stamp:";
            // 
            // lblMaxProcessingTime
            // 
            lblMaxProcessingTime.AutoSize = true;
            lblMaxProcessingTime.Location = new Point(166, 168);
            lblMaxProcessingTime.Name = "lblMaxProcessingTime";
            lblMaxProcessingTime.Size = new Size(27, 15);
            lblMaxProcessingTime.TabIndex = 11;
            lblMaxProcessingTime.Text = "----";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(11, 168);
            label12.Name = "label12";
            label12.Size = new Size(149, 15);
            label12.TabIndex = 10;
            label12.Text = "Max Processing Time (ms):";
            // 
            // lblFlaps
            // 
            lblFlaps.AutoSize = true;
            lblFlaps.Location = new Point(315, 88);
            lblFlaps.Name = "lblFlaps";
            lblFlaps.Size = new Size(27, 15);
            lblFlaps.TabIndex = 9;
            lblFlaps.Text = "----";
            // 
            // lblAltitude
            // 
            lblAltitude.AutoSize = true;
            lblAltitude.Location = new Point(315, 57);
            lblAltitude.Name = "lblAltitude";
            lblAltitude.Size = new Size(27, 15);
            lblAltitude.TabIndex = 8;
            lblAltitude.Text = "----";
            // 
            // lblAircraftType
            // 
            lblAircraftType.AutoSize = true;
            lblAircraftType.Location = new Point(146, 125);
            lblAircraftType.Name = "lblAircraftType";
            lblAircraftType.Size = new Size(27, 15);
            lblAircraftType.TabIndex = 7;
            lblAircraftType.Tag = "empty";
            lblAircraftType.Text = "----";
            // 
            // lblSpeedBrakes
            // 
            lblSpeedBrakes.AutoSize = true;
            lblSpeedBrakes.Location = new Point(146, 88);
            lblSpeedBrakes.Name = "lblSpeedBrakes";
            lblSpeedBrakes.Size = new Size(27, 15);
            lblSpeedBrakes.TabIndex = 7;
            lblSpeedBrakes.Text = "----";
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(146, 57);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(27, 15);
            lblSpeed.TabIndex = 7;
            lblSpeed.Text = "----";
            // 
            // lblAoA
            // 
            lblAoA.AutoSize = true;
            lblAoA.Location = new Point(146, 19);
            lblAoA.Name = "lblAoA";
            lblAoA.Size = new Size(27, 15);
            lblAoA.TabIndex = 6;
            lblAoA.Text = "----";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(65, 125);
            label11.Name = "label11";
            label11.Size = new Size(75, 15);
            label11.TabIndex = 5;
            label11.Text = "Aircraft type:";
            label11.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(40, 88);
            label10.Name = "label10";
            label10.Size = new Size(100, 15);
            label10.TabIndex = 4;
            label10.Text = "Speed Brakes (%):";
            label10.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(251, 88);
            label9.Name = "label9";
            label9.Size = new Size(58, 15);
            label9.TabIndex = 3;
            label9.Text = "Flaps (%):";
            label9.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(226, 57);
            label8.Name = "label8";
            label8.Size = new Size(83, 15);
            label8.TabIndex = 2;
            label8.Text = "Altitude (feet):";
            label8.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(18, 57);
            label7.Name = "label7";
            label7.Size = new Size(122, 15);
            label7.TabIndex = 1;
            label7.Text = "True Airspeed (Knots):";
            label7.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(54, 19);
            label6.Name = "label6";
            label6.Size = new Size(86, 15);
            label6.TabIndex = 0;
            label6.Text = "AoA (Degrees):";
            label6.TextAlign = ContentAlignment.MiddleRight;
            // 
            // chkShowStatistics
            // 
            chkShowStatistics.AutoSize = true;
            chkShowStatistics.Location = new Point(15, 45);
            chkShowStatistics.Name = "chkShowStatistics";
            chkShowStatistics.Size = new Size(299, 19);
            chkShowStatistics.TabIndex = 1;
            chkShowStatistics.Text = "Show Statistics and additional info during each poll.";
            chkShowStatistics.UseVisualStyleBackColor = true;
            chkShowStatistics.CheckedChanged += chkShowStatistics_CheckedChanged;
            // 
            // chkChangeToMonitor
            // 
            chkChangeToMonitor.AutoSize = true;
            chkChangeToMonitor.Location = new Point(15, 20);
            chkChangeToMonitor.Name = "chkChangeToMonitor";
            chkChangeToMonitor.Size = new Size(316, 19);
            chkChangeToMonitor.TabIndex = 0;
            chkChangeToMonitor.Text = "Automatically change to monitor tab when connected.";
            chkChangeToMonitor.UseVisualStyleBackColor = true;
            // 
            // tabDebug
            // 
            tabDebug.Controls.Add(lblDebugTitle);
            tabDebug.Controls.Add(txtDebug);
            tabDebug.Location = new Point(4, 24);
            tabDebug.Name = "tabDebug";
            tabDebug.Size = new Size(466, 307);
            tabDebug.TabIndex = 2;
            tabDebug.Text = "Debug";
            tabDebug.UseVisualStyleBackColor = true;
            // 
            // lblDebugTitle
            // 
            lblDebugTitle.AutoSize = true;
            lblDebugTitle.Location = new Point(25, 16);
            lblDebugTitle.Name = "lblDebugTitle";
            lblDebugTitle.Size = new Size(258, 15);
            lblDebugTitle.TabIndex = 1;
            lblDebugTitle.Text = "Errors Detected (only first 100 errors are shown):";
            // 
            // txtDebug
            // 
            txtDebug.Location = new Point(25, 44);
            txtDebug.Multiline = true;
            txtDebug.Name = "txtDebug";
            txtDebug.ReadOnly = true;
            txtDebug.ScrollBars = ScrollBars.Both;
            txtDebug.Size = new Size(407, 239);
            txtDebug.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(326, 353);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 1;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(407, 353);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 2;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnResetMax
            // 
            btnResetMax.Location = new Point(16, 353);
            btnResetMax.Name = "btnResetMax";
            btnResetMax.Size = new Size(75, 23);
            btnResetMax.TabIndex = 3;
            btnResetMax.Text = "Reset Max";
            btnResetMax.UseVisualStyleBackColor = true;
            btnResetMax.Click += btnResetMax_Click;
            // 
            // timer1
            // 
            timer1.Interval = 500;
            timer1.Tick += timer1_Tick;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.Bottom;
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsStatus });
            toolStrip1.Location = new Point(0, 390);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(501, 25);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsStatus
            // 
            tsStatus.AutoToolTip = true;
            tsStatus.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsStatus.Name = "tsStatus";
            tsStatus.Size = new Size(26, 22);
            tsStatus.Tag = "empty";
            tsStatus.Text = "Idle";
            tsStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // frmWarThunderTelemetry
            // 
            AcceptButton = btnStart;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(501, 415);
            Controls.Add(toolStrip1);
            Controls.Add(btnResetMax);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "frmWarThunderTelemetry";
            Text = "War Thunder Telemetry Exporter";
            FormClosing += frmWarThunderTelemetry_FormClosing;
            Load += Form1_Load;
            tabControl1.ResumeLayout(false);
            tabSettings.ResumeLayout(false);
            tabSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).EndInit();
            tabMonitor.ResumeLayout(false);
            tabMonitor.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            tabDebug.ResumeLayout(false);
            tabDebug.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabSettings;
        private TabPage tabMonitor;
        private Button btnStart;
        private Button btnStop;
        private Button btnResetMax;
        private CheckBox chkUseEfficiencyCoresOnly;
        private Label label1;
        private TextBox txtDestinationHostname;
        private TextBox txtDestinationPort;
        private Label label2;
        private Label label3;
        private ComboBox cmbPriorityClass;
        private Label label4;
        private NumericUpDown nudFrequency;
        private TextBox txtWtUrl;
        private Label label5;
        private System.Windows.Forms.Timer timer1;
        private CheckBox chkChangeToMonitor;
        private CheckBox chkShowStatistics;
        private Panel panel1;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label lblAircraftType;
        private Label lblSpeedBrakes;
        private Label lblSpeed;
        private Label lblAoA;
        private Label lblFlaps;
        private Label lblAltitude;
        private ToolStrip toolStrip1;
        private ToolStripLabel tsStatus;
        private Label label12;
        private Label lblMaxProcessingTime;
        private Label lblLastTimeStamp;
        private Label label14;
        private Label lblGForces;
        private Label label15;
        private TabPage tabDebug;
        private TextBox txtDebug;
        private Label lblDebugTitle;
    }
}

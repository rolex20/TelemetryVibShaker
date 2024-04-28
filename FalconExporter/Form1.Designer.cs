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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.tabMonitor = new System.Windows.Forms.TabPage();
            this.lblAltitude = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblAoA = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsTimestamp = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblRALT = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblTrueAirspeed = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblSpeedBrakes = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lblGForces = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblTotalStates = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lblVehicleType = new System.Windows.Forms.Label();
            this.lblFuel = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabMonitor.SuspendLayout();
            this.statusStrip1.SuspendLayout();
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
            this.tabSettings.Location = new System.Drawing.Point(4, 22);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(464, 277);
            this.tabSettings.TabIndex = 0;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // tabMonitor
            // 
            this.tabMonitor.Controls.Add(this.lblFuel);
            this.tabMonitor.Controls.Add(this.label11);
            this.tabMonitor.Controls.Add(this.lblVehicleType);
            this.tabMonitor.Controls.Add(this.label9);
            this.tabMonitor.Controls.Add(this.lblTotalStates);
            this.tabMonitor.Controls.Add(this.label3);
            this.tabMonitor.Controls.Add(this.lblGForces);
            this.tabMonitor.Controls.Add(this.label8);
            this.tabMonitor.Controls.Add(this.lblSpeedBrakes);
            this.tabMonitor.Controls.Add(this.label7);
            this.tabMonitor.Controls.Add(this.lblTrueAirspeed);
            this.tabMonitor.Controls.Add(this.label6);
            this.tabMonitor.Controls.Add(this.lblRALT);
            this.tabMonitor.Controls.Add(this.label5);
            this.tabMonitor.Controls.Add(this.lblAltitude);
            this.tabMonitor.Controls.Add(this.label4);
            this.tabMonitor.Controls.Add(this.lblSpeed);
            this.tabMonitor.Controls.Add(this.label2);
            this.tabMonitor.Controls.Add(this.lblAoA);
            this.tabMonitor.Controls.Add(this.label1);
            this.tabMonitor.Location = new System.Drawing.Point(4, 22);
            this.tabMonitor.Name = "tabMonitor";
            this.tabMonitor.Padding = new System.Windows.Forms.Padding(3);
            this.tabMonitor.Size = new System.Drawing.Size(464, 277);
            this.tabMonitor.TabIndex = 1;
            this.tabMonitor.Text = "Monitor";
            this.tabMonitor.UseVisualStyleBackColor = true;
            // 
            // lblAltitude
            // 
            this.lblAltitude.AutoSize = true;
            this.lblAltitude.Location = new System.Drawing.Point(82, 144);
            this.lblAltitude.Name = "lblAltitude";
            this.lblAltitude.Size = new System.Drawing.Size(16, 13);
            this.lblAltitude.TabIndex = 5;
            this.lblAltitude.Text = "---";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(28, 145);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Altitude:";
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(79, 107);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(16, 13);
            this.lblSpeed.TabIndex = 3;
            this.lblSpeed.Text = "---";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 107);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Speed:";
            // 
            // lblAoA
            // 
            this.lblAoA.AutoSize = true;
            this.lblAoA.Location = new System.Drawing.Point(79, 66);
            this.lblAoA.Name = "lblAoA";
            this.lblAoA.Size = new System.Drawing.Size(16, 13);
            this.lblAoA.TabIndex = 1;
            this.lblAoA.Text = "---";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(43, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "AoA:";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(417, 343);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsStatus,
            this.tsTimestamp});
            this.statusStrip1.Location = new System.Drawing.Point(0, 378);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(512, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsStatus
            // 
            this.tsStatus.Name = "tsStatus";
            this.tsStatus.Size = new System.Drawing.Size(26, 17);
            this.tsStatus.Text = "Idle";
            // 
            // tsTimestamp
            // 
            this.tsTimestamp.Name = "tsTimestamp";
            this.tsTimestamp.Size = new System.Drawing.Size(22, 17);
            this.tsTimestamp.Text = "---";
            // 
            // lblRALT
            // 
            this.lblRALT.AutoSize = true;
            this.lblRALT.Location = new System.Drawing.Point(82, 177);
            this.lblRALT.Name = "lblRALT";
            this.lblRALT.Size = new System.Drawing.Size(16, 13);
            this.lblRALT.TabIndex = 7;
            this.lblRALT.Text = "---";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(28, 178);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "RALT:";
            // 
            // lblTrueAirspeed
            // 
            this.lblTrueAirspeed.AutoSize = true;
            this.lblTrueAirspeed.Location = new System.Drawing.Point(282, 107);
            this.lblTrueAirspeed.Name = "lblTrueAirspeed";
            this.lblTrueAirspeed.Size = new System.Drawing.Size(16, 13);
            this.lblTrueAirspeed.TabIndex = 9;
            this.lblTrueAirspeed.Text = "---";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(200, 107);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "True Airspeed:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(203, 66);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(76, 13);
            this.label7.TabIndex = 10;
            this.label7.Text = "Speed brakes:";
            // 
            // lblSpeedBrakes
            // 
            this.lblSpeedBrakes.AutoSize = true;
            this.lblSpeedBrakes.Location = new System.Drawing.Point(285, 65);
            this.lblSpeedBrakes.Name = "lblSpeedBrakes";
            this.lblSpeedBrakes.Size = new System.Drawing.Size(16, 13);
            this.lblSpeedBrakes.TabIndex = 11;
            this.lblSpeedBrakes.Text = "---";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(223, 145);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "G Forces:";
            // 
            // lblGForces
            // 
            this.lblGForces.AutoSize = true;
            this.lblGForces.Location = new System.Drawing.Point(282, 145);
            this.lblGForces.Name = "lblGForces";
            this.lblGForces.Size = new System.Drawing.Size(16, 13);
            this.lblGForces.TabIndex = 13;
            this.lblGForces.Text = "---";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(209, 178);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Total States:";
            // 
            // lblTotalStates
            // 
            this.lblTotalStates.AutoSize = true;
            this.lblTotalStates.Location = new System.Drawing.Point(282, 178);
            this.lblTotalStates.Name = "lblTotalStates";
            this.lblTotalStates.Size = new System.Drawing.Size(16, 13);
            this.lblTotalStates.TabIndex = 15;
            this.lblTotalStates.Text = "---";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(204, 210);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(72, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Vehicle Type:";
            // 
            // lblVehicleType
            // 
            this.lblVehicleType.AutoSize = true;
            this.lblVehicleType.Location = new System.Drawing.Point(282, 210);
            this.lblVehicleType.Name = "lblVehicleType";
            this.lblVehicleType.Size = new System.Drawing.Size(16, 13);
            this.lblVehicleType.TabIndex = 17;
            this.lblVehicleType.Text = "---";
            // 
            // lblFuel
            // 
            this.lblFuel.AutoSize = true;
            this.lblFuel.Location = new System.Drawing.Point(282, 240);
            this.lblFuel.Name = "lblFuel";
            this.lblFuel.Size = new System.Drawing.Size(16, 13);
            this.lblFuel.TabIndex = 19;
            this.lblFuel.Text = "---";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(246, 240);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(30, 13);
            this.label11.TabIndex = 18;
            this.label11.Text = "Fuel:";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 400);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Falcon 4 - Telemetry Exporter";
            this.tabControl1.ResumeLayout(false);
            this.tabMonitor.ResumeLayout(false);
            this.tabMonitor.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
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
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ToolStripStatusLabel tsTimestamp;
        private System.Windows.Forms.Label lblRALT;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblTrueAirspeed;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblSpeedBrakes;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblGForces;
        private System.Windows.Forms.Label lblTotalStates;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblVehicleType;
        private System.Windows.Forms.Label lblFuel;
        private System.Windows.Forms.Label label11;
    }
}


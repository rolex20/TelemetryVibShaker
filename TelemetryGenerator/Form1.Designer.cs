namespace TelemetryGenerator
{
    partial class Form1
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
            label1 = new Label();
            txtAircraftName = new TextBox();
            btnSendUnitName = new Button();
            panel1 = new Panel();
            label15 = new Label();
            label14 = new Label();
            label13 = new Label();
            label12 = new Label();
            label11 = new Label();
            nudDatagrams = new NumericUpDown();
            label10 = new Label();
            nudWaittime = new NumericUpDown();
            label6 = new Label();
            nudAltitude = new NumericUpDown();
            label9 = new Label();
            nudGForces = new NumericUpDown();
            label8 = new Label();
            nudSpeed = new NumericUpDown();
            nudFlaps = new NumericUpDown();
            nudSpeedBrake = new NumericUpDown();
            nudAoA = new NumericUpDown();
            label7 = new Label();
            label5 = new Label();
            label4 = new Label();
            label2 = new Label();
            label3 = new Label();
            label16 = new Label();
            txtIP = new TextBox();
            label17 = new Label();
            txtPort = new TextBox();
            btnStart = new Button();
            btnCancel = new Button();
            progressBar1 = new ProgressBar();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudDatagrams).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudWaittime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAltitude).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudGForces).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSpeed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFlaps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSpeedBrake).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAoA).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(30, 70);
            label1.Name = "label1";
            label1.Size = new Size(84, 15);
            label1.TabIndex = 0;
            label1.Text = "Aircraft Name:";
            // 
            // txtAircraftName
            // 
            txtAircraftName.Location = new Point(120, 67);
            txtAircraftName.Name = "txtAircraftName";
            txtAircraftName.Size = new Size(152, 23);
            txtAircraftName.TabIndex = 1;
            txtAircraftName.Text = "F-16C_50";
            // 
            // btnSendUnitName
            // 
            btnSendUnitName.Location = new Point(289, 67);
            btnSendUnitName.Name = "btnSendUnitName";
            btnSendUnitName.Size = new Size(75, 23);
            btnSendUnitName.TabIndex = 2;
            btnSendUnitName.Text = "Send Unit Name";
            btnSendUnitName.UseVisualStyleBackColor = true;
            btnSendUnitName.Click += btnSendUnitName_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(label15);
            panel1.Controls.Add(label14);
            panel1.Controls.Add(label13);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(nudDatagrams);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(nudWaittime);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(nudAltitude);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(nudGForces);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(nudSpeed);
            panel1.Controls.Add(nudFlaps);
            panel1.Controls.Add(nudSpeedBrake);
            panel1.Controls.Add(nudAoA);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label2);
            panel1.Location = new Point(30, 128);
            panel1.Name = "panel1";
            panel1.Size = new Size(334, 391);
            panel1.TabIndex = 5;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(252, 66);
            label15.Name = "label15";
            label15.Size = new Size(17, 15);
            label15.TabIndex = 26;
            label15.Text = "%";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(248, 109);
            label14.Name = "label14";
            label14.Size = new Size(17, 15);
            label14.TabIndex = 25;
            label14.Text = "%";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(248, 156);
            label13.Name = "label13";
            label13.Size = new Size(35, 15);
            label13.TabIndex = 24;
            label13.Text = "dm/s";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(252, 257);
            label12.Name = "label12";
            label12.Size = new Size(68, 15);
            label12.TabIndex = 23;
            label12.Text = "decameters";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(248, 303);
            label11.Name = "label11";
            label11.Size = new Size(23, 15);
            label11.TabIndex = 22;
            label11.Text = "ms";
            // 
            // nudDatagrams
            // 
            nudDatagrams.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            nudDatagrams.Location = new Point(122, 350);
            nudDatagrams.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudDatagrams.Name = "nudDatagrams";
            nudDatagrams.Size = new Size(120, 23);
            nudDatagrams.TabIndex = 21;
            nudDatagrams.Value = new decimal(new int[] { 200, 0, 0, 0 });
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(3, 352);
            label10.Name = "label10";
            label10.Size = new Size(110, 15);
            label10.TabIndex = 20;
            label10.Text = "Datagrams to Send:";
            // 
            // nudWaittime
            // 
            nudWaittime.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudWaittime.Location = new Point(122, 301);
            nudWaittime.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudWaittime.Name = "nudWaittime";
            nudWaittime.Size = new Size(120, 23);
            nudWaittime.TabIndex = 19;
            nudWaittime.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(25, 303);
            label6.Name = "label6";
            label6.Size = new Size(63, 15);
            label6.TabIndex = 18;
            label6.Text = "Wait Time:";
            // 
            // nudAltitude
            // 
            nudAltitude.Location = new Point(122, 253);
            nudAltitude.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            nudAltitude.Name = "nudAltitude";
            nudAltitude.Size = new Size(120, 23);
            nudAltitude.TabIndex = 17;
            nudAltitude.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(38, 255);
            label9.Name = "label9";
            label9.Size = new Size(52, 15);
            label9.TabIndex = 16;
            label9.Text = "Altitude:";
            // 
            // nudGForces
            // 
            nudGForces.Location = new Point(122, 202);
            nudGForces.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            nudGForces.Name = "nudGForces";
            nudGForces.Size = new Size(120, 23);
            nudGForces.TabIndex = 17;
            nudGForces.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(35, 204);
            label8.Name = "label8";
            label8.Size = new Size(55, 15);
            label8.TabIndex = 16;
            label8.Text = "G Forces:";
            // 
            // nudSpeed
            // 
            nudSpeed.Location = new Point(122, 154);
            nudSpeed.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            nudSpeed.Name = "nudSpeed";
            nudSpeed.Size = new Size(120, 23);
            nudSpeed.TabIndex = 15;
            nudSpeed.Value = new decimal(new int[] { 55, 0, 0, 0 });
            // 
            // nudFlaps
            // 
            nudFlaps.Location = new Point(122, 107);
            nudFlaps.Name = "nudFlaps";
            nudFlaps.Size = new Size(120, 23);
            nudFlaps.TabIndex = 13;
            // 
            // nudSpeedBrake
            // 
            nudSpeedBrake.Location = new Point(122, 64);
            nudSpeedBrake.Name = "nudSpeedBrake";
            nudSpeedBrake.Size = new Size(120, 23);
            nudSpeedBrake.TabIndex = 12;
            // 
            // nudAoA
            // 
            nudAoA.Location = new Point(122, 19);
            nudAoA.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
            nudAoA.Name = "nudAoA";
            nudAoA.Size = new Size(120, 23);
            nudAoA.TabIndex = 11;
            nudAoA.Value = new decimal(new int[] { 9, 0, 0, 0 });
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(48, 156);
            label7.Name = "label7";
            label7.Size = new Size(42, 15);
            label7.TabIndex = 10;
            label7.Text = "Speed:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(53, 109);
            label5.Name = "label5";
            label5.Size = new Size(37, 15);
            label5.TabIndex = 6;
            label5.Text = "Flaps:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(16, 64);
            label4.Name = "label4";
            label4.Size = new Size(74, 15);
            label4.TabIndex = 4;
            label4.Text = "Speed Brake:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(57, 27);
            label2.Name = "label2";
            label2.Size = new Size(33, 15);
            label2.TabIndex = 2;
            label2.Text = "AoA:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(30, 110);
            label3.Name = "label3";
            label3.Size = new Size(40, 15);
            label3.TabIndex = 6;
            label3.Text = "Batch:";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(33, 27);
            label16.Name = "label16";
            label16.Size = new Size(77, 15);
            label16.TabIndex = 7;
            label16.Text = "DestinationIP";
            // 
            // txtIP
            // 
            txtIP.Location = new Point(120, 24);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(104, 23);
            txtIP.TabIndex = 8;
            txtIP.Text = "192.168.1.17";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(236, 27);
            label17.Name = "label17";
            label17.Size = new Size(32, 15);
            label17.TabIndex = 9;
            label17.Text = "Port:";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(274, 24);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(100, 23);
            txtPort.TabIndex = 10;
            txtPort.Text = "54671";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(197, 538);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 11;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(286, 538);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 12;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(33, 538);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(100, 23);
            progressBar1.TabIndex = 13;
            progressBar1.Value = 50;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(402, 578);
            Controls.Add(progressBar1);
            Controls.Add(btnCancel);
            Controls.Add(btnStart);
            Controls.Add(txtPort);
            Controls.Add(label17);
            Controls.Add(txtIP);
            Controls.Add(label16);
            Controls.Add(label3);
            Controls.Add(panel1);
            Controls.Add(btnSendUnitName);
            Controls.Add(txtAircraftName);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "Form1";
            Text = "Telemetry Generator";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudDatagrams).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudWaittime).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAltitude).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudGForces).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSpeed).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudFlaps).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSpeedBrake).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAoA).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtAircraftName;
        private Button btnSendUnitName;
        private Panel panel1;
        private Label label2;
        private Label label3;
        private Label label5;
        private Label label4;
        private Label label7;
        private NumericUpDown nudAoA;
        private NumericUpDown nudGForces;
        private Label label8;
        private NumericUpDown nudSpeed;
        private NumericUpDown nudFlaps;
        private NumericUpDown nudSpeedBrake;
        private NumericUpDown nudAltitude;
        private Label label9;
        private NumericUpDown nudWaittime;
        private Label label6;
        private NumericUpDown nudDatagrams;
        private Label label10;
        private Label label13;
        private Label label12;
        private Label label11;
        private Label label15;
        private Label label14;
        private Label label16;
        private TextBox txtIP;
        private Label label17;
        private TextBox txtPort;
        private Button btnStart;
        private Button btnCancel;
        private ProgressBar progressBar1;
    }
}

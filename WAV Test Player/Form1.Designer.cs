namespace WAV_Test_Player
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtWAVFile = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.tbVolume = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.lblVolume = new System.Windows.Forms.Label();
            this.cmbSoundDevice = new System.Windows.Forms.ComboBox();
            this.lblVolume2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbVolume2 = new System.Windows.Forms.TrackBar();
            this.txtWAVFile2 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume2)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(33, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Sound File 1";
            // 
            // txtWAVFile
            // 
            this.txtWAVFile.Location = new System.Drawing.Point(139, 77);
            this.txtWAVFile.Name = "txtWAVFile";
            this.txtWAVFile.Size = new System.Drawing.Size(261, 20);
            this.txtWAVFile.TabIndex = 1;
            this.txtWAVFile.Text = "C:\\Users\\ralch\\Music\\AoA\\Full Throttle Electric.wav";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Enabled = false;
            this.checkBox1.Location = new System.Drawing.Point(139, 12);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(61, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Repeat";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(92, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Select Sound Device";
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(422, 73);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(63, 27);
            this.btnPlay.TabIndex = 5;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(422, 107);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(63, 27);
            this.btnStop.TabIndex = 6;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // tbVolume
            // 
            this.tbVolume.LargeChange = 10;
            this.tbVolume.Location = new System.Drawing.Point(139, 114);
            this.tbVolume.Maximum = 100;
            this.tbVolume.Name = "tbVolume";
            this.tbVolume.Size = new System.Drawing.Size(222, 45);
            this.tbVolume.TabIndex = 7;
            this.tbVolume.TickFrequency = 10;
            this.tbVolume.Value = 100;
            this.tbVolume.Scroll += new System.EventHandler(this.tbVolume_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(90, 121);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Volume";
            // 
            // lblVolume
            // 
            this.lblVolume.AutoSize = true;
            this.lblVolume.Location = new System.Drawing.Point(367, 121);
            this.lblVolume.Name = "lblVolume";
            this.lblVolume.Size = new System.Drawing.Size(33, 13);
            this.lblVolume.TabIndex = 9;
            this.lblVolume.Text = "100%";
            // 
            // cmbSoundDevice
            // 
            this.cmbSoundDevice.FormattingEnabled = true;
            this.cmbSoundDevice.Location = new System.Drawing.Point(208, 34);
            this.cmbSoundDevice.Name = "cmbSoundDevice";
            this.cmbSoundDevice.Size = new System.Drawing.Size(261, 21);
            this.cmbSoundDevice.TabIndex = 10;
            // 
            // lblVolume2
            // 
            this.lblVolume2.AutoSize = true;
            this.lblVolume2.Location = new System.Drawing.Point(367, 221);
            this.lblVolume2.Name = "lblVolume2";
            this.lblVolume2.Size = new System.Drawing.Size(33, 13);
            this.lblVolume2.TabIndex = 17;
            this.lblVolume2.Text = "100%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(90, 221);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Volume";
            // 
            // tbVolume2
            // 
            this.tbVolume2.LargeChange = 10;
            this.tbVolume2.Location = new System.Drawing.Point(139, 214);
            this.tbVolume2.Maximum = 100;
            this.tbVolume2.Name = "tbVolume2";
            this.tbVolume2.Size = new System.Drawing.Size(222, 45);
            this.tbVolume2.TabIndex = 15;
            this.tbVolume2.TickFrequency = 10;
            this.tbVolume2.Value = 100;
            this.tbVolume2.Scroll += new System.EventHandler(this.tbVolume2_Scroll);
            // 
            // txtWAVFile2
            // 
            this.txtWAVFile2.Location = new System.Drawing.Point(139, 174);
            this.txtWAVFile2.Name = "txtWAVFile2";
            this.txtWAVFile2.Size = new System.Drawing.Size(261, 20);
            this.txtWAVFile2.TabIndex = 12;
            this.txtWAVFile2.Text = "C:\\Users\\ralch\\Music\\AoA\\VMS_stall.wav";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(33, 177);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(99, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Select Sound File 2";
            // 
            // Form1
            // 
            this.AcceptButton = this.btnPlay;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(511, 274);
            this.Controls.Add(this.lblVolume2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbVolume2);
            this.Controls.Add(this.txtWAVFile2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cmbSoundDevice);
            this.Controls.Add(this.lblVolume);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbVolume);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.txtWAVFile);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "WAV Test Player";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtWAVFile;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TrackBar tbVolume;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblVolume;
        private System.Windows.Forms.ComboBox cmbSoundDevice;
        private System.Windows.Forms.Label lblVolume2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TrackBar tbVolume2;
        private System.Windows.Forms.TextBox txtWAVFile2;
        private System.Windows.Forms.Label label6;
    }
}


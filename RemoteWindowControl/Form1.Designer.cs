namespace RemoteWindowControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            tabControls = new TabControl();
            tabSettings = new TabPage();
            nudInstance = new NumericUpDown();
            chkUseCachedHTML = new CheckBox();
            lblCountDownTimer = new Label();
            chkAutostart = new CheckBox();
            chkUseEfficiencyCoresOnly = new CheckBox();
            btnStartWebServer = new Button();
            txtHTML = new TextBox();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            lblLink = new LinkLabel();
            lblWebServerThreadId = new Label();
            txtPort = new TextBox();
            txtIPAddress = new TextBox();
            tabDebug = new TabPage();
            txtDebug = new TextBox();
            label1 = new Label();
            txtProgramsConfigurationFile = new TextBox();
            tabControls.SuspendLayout();
            tabSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).BeginInit();
            tabDebug.SuspendLayout();
            SuspendLayout();
            // 
            // tabControls
            // 
            tabControls.Controls.Add(tabSettings);
            tabControls.Controls.Add(tabDebug);
            tabControls.Location = new Point(21, 24);
            tabControls.Name = "tabControls";
            tabControls.SelectedIndex = 0;
            tabControls.Size = new Size(376, 362);
            tabControls.TabIndex = 0;
            // 
            // tabSettings
            // 
            tabSettings.Controls.Add(txtProgramsConfigurationFile);
            tabSettings.Controls.Add(label1);
            tabSettings.Controls.Add(nudInstance);
            tabSettings.Controls.Add(chkUseCachedHTML);
            tabSettings.Controls.Add(lblCountDownTimer);
            tabSettings.Controls.Add(chkAutostart);
            tabSettings.Controls.Add(chkUseEfficiencyCoresOnly);
            tabSettings.Controls.Add(btnStartWebServer);
            tabSettings.Controls.Add(txtHTML);
            tabSettings.Controls.Add(label6);
            tabSettings.Controls.Add(label5);
            tabSettings.Controls.Add(label4);
            tabSettings.Controls.Add(label3);
            tabSettings.Controls.Add(lblLink);
            tabSettings.Controls.Add(lblWebServerThreadId);
            tabSettings.Controls.Add(txtPort);
            tabSettings.Controls.Add(txtIPAddress);
            tabSettings.Location = new Point(4, 24);
            tabSettings.Name = "tabSettings";
            tabSettings.Padding = new Padding(3);
            tabSettings.Size = new Size(368, 334);
            tabSettings.TabIndex = 1;
            tabSettings.Text = "Settings";
            tabSettings.UseVisualStyleBackColor = true;
            // 
            // nudInstance
            // 
            nudInstance.Location = new Point(310, 16);
            nudInstance.Name = "nudInstance";
            nudInstance.Size = new Size(39, 23);
            nudInstance.TabIndex = 15;
            // 
            // chkUseCachedHTML
            // 
            chkUseCachedHTML.AutoSize = true;
            chkUseCachedHTML.Checked = true;
            chkUseCachedHTML.CheckState = CheckState.Checked;
            chkUseCachedHTML.Location = new Point(21, 239);
            chkUseCachedHTML.Name = "chkUseCachedHTML";
            chkUseCachedHTML.Size = new Size(172, 19);
            chkUseCachedHTML.TabIndex = 13;
            chkUseCachedHTML.Text = "Use cached HTML Template";
            chkUseCachedHTML.UseVisualStyleBackColor = true;
            // 
            // lblCountDownTimer
            // 
            lblCountDownTimer.AutoSize = true;
            lblCountDownTimer.Location = new Point(298, 215);
            lblCountDownTimer.Name = "lblCountDownTimer";
            lblCountDownTimer.Size = new Size(22, 15);
            lblCountDownTimer.TabIndex = 12;
            lblCountDownTimer.Tag = "";
            lblCountDownTimer.Text = "---";
            // 
            // chkAutostart
            // 
            chkAutostart.AutoSize = true;
            chkAutostart.Checked = true;
            chkAutostart.CheckState = CheckState.Checked;
            chkAutostart.Location = new Point(21, 214);
            chkAutostart.Name = "chkAutostart";
            chkAutostart.Size = new Size(262, 19);
            chkAutostart.TabIndex = 11;
            chkAutostart.Text = "Auto Minimize and Start web server on load: ";
            chkAutostart.UseVisualStyleBackColor = true;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Checked = true;
            chkUseEfficiencyCoresOnly.CheckState = CheckState.Checked;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(21, 265);
            chkUseEfficiencyCoresOnly.Name = "chkUseEfficiencyCoresOnly";
            chkUseEfficiencyCoresOnly.Size = new Size(290, 19);
            chkUseEfficiencyCoresOnly.TabIndex = 10;
            chkUseEfficiencyCoresOnly.Text = "Use Efficiency cores only on Intel 12700K";
            chkUseEfficiencyCoresOnly.UseVisualStyleBackColor = true;
            chkUseEfficiencyCoresOnly.Visible = false;
            chkUseEfficiencyCoresOnly.CheckedChanged += chkUseEfficiencyCoresOnly_CheckedChanged;
            // 
            // btnStartWebServer
            // 
            btnStartWebServer.Location = new Point(223, 299);
            btnStartWebServer.Name = "btnStartWebServer";
            btnStartWebServer.Size = new Size(126, 23);
            btnStartWebServer.TabIndex = 9;
            btnStartWebServer.Text = "Start Web Server";
            btnStartWebServer.UseVisualStyleBackColor = true;
            btnStartWebServer.Click += btnStartWebServer_Click;
            // 
            // txtHTML
            // 
            txtHTML.Location = new Point(153, 173);
            txtHTML.Name = "txtHTML";
            txtHTML.Size = new Size(196, 23);
            txtHTML.TabIndex = 8;
            txtHTML.Text = ".\\Remote.Control.html";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(44, 176);
            label6.Name = "label6";
            label6.Size = new Size(93, 15);
            label6.TabIndex = 7;
            label6.Text = "HTML Template:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(21, 54);
            label5.Name = "label5";
            label5.Size = new Size(116, 15);
            label5.TabIndex = 6;
            label5.Text = "Web Server Listen IP:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(43, 92);
            label4.Name = "label4";
            label4.Size = new Size(94, 15);
            label4.TabIndex = 5;
            label4.Text = "Web Server Port:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(16, 125);
            label3.Name = "label3";
            label3.Size = new Size(121, 15);
            label3.TabIndex = 4;
            label3.Text = "Web Server Thread Id:";
            // 
            // lblLink
            // 
            lblLink.AutoSize = true;
            lblLink.Location = new Point(153, 148);
            lblLink.Name = "lblLink";
            lblLink.Size = new Size(60, 15);
            lblLink.TabIndex = 3;
            lblLink.TabStop = true;
            lblLink.Text = "linkLabel1";
            lblLink.LinkClicked += lblLink_LinkClicked;
            // 
            // lblWebServerThreadId
            // 
            lblWebServerThreadId.AutoSize = true;
            lblWebServerThreadId.Location = new Point(153, 125);
            lblWebServerThreadId.Name = "lblWebServerThreadId";
            lblWebServerThreadId.Size = new Size(22, 15);
            lblWebServerThreadId.TabIndex = 2;
            lblWebServerThreadId.Text = "---";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(153, 89);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(196, 23);
            txtPort.TabIndex = 1;
            txtPort.Text = "8081";
            txtPort.TextChanged += txtPort_TextChanged;
            // 
            // txtIPAddress
            // 
            txtIPAddress.Location = new Point(153, 51);
            txtIPAddress.Name = "txtIPAddress";
            txtIPAddress.Size = new Size(196, 23);
            txtIPAddress.TabIndex = 0;
            txtIPAddress.Text = "192.168.1.5";
            txtIPAddress.TextChanged += txtIPAddress_TextChanged;
            // 
            // tabDebug
            // 
            tabDebug.Controls.Add(txtDebug);
            tabDebug.Location = new Point(4, 24);
            tabDebug.Name = "tabDebug";
            tabDebug.Size = new Size(368, 334);
            tabDebug.TabIndex = 2;
            tabDebug.Text = "Debug";
            tabDebug.UseVisualStyleBackColor = true;
            // 
            // txtDebug
            // 
            txtDebug.Location = new Point(12, 12);
            txtDebug.Multiline = true;
            txtDebug.Name = "txtDebug";
            txtDebug.ReadOnly = true;
            txtDebug.ScrollBars = ScrollBars.Both;
            txtDebug.Size = new Size(290, 297);
            txtDebug.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(37, 19);
            label1.Name = "label1";
            label1.Size = new Size(100, 15);
            label1.TabIndex = 16;
            label1.Text = "Programs Config:";
            // 
            // txtProgramsConfigurationFile
            // 
            txtProgramsConfigurationFile.Location = new Point(153, 16);
            txtProgramsConfigurationFile.Name = "txtProgramsConfigurationFile";
            txtProgramsConfigurationFile.Size = new Size(151, 23);
            txtProgramsConfigurationFile.TabIndex = 17;
            // 
            // frmMain
            // 
            AcceptButton = btnStartWebServer;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(409, 398);
            Controls.Add(tabControls);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "frmMain";
            Text = "Window Remote Control";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            tabControls.ResumeLayout(false);
            tabSettings.ResumeLayout(false);
            tabSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).EndInit();
            tabDebug.ResumeLayout(false);
            tabDebug.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControls;
        private TabPage tabSettings;
        private TextBox txtIPAddress;
        private TextBox txtPort;
        private TabPage tabDebug;
        private TextBox txtDebug;
        private Label lblWebServerThreadId;
        private LinkLabel lblLink;
        private Label label3;
        private Label label5;
        private Label label4;
        private TextBox txtHTML;
        private Label label6;
        private Button btnStartWebServer;
        private CheckBox chkUseEfficiencyCoresOnly;
        private CheckBox chkAutostart;
        private Label lblCountDownTimer;
        private CheckBox chkUseCachedHTML;
        private NumericUpDown nudInstance;
        private TextBox txtProgramsConfigurationFile;
        private Label label1;
    }
}

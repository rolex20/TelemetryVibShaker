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
            tabControls = new TabControl();
            tabMovements = new TabPage();
            button1 = new Button();
            textBox1 = new TextBox();
            nudInstance = new NumericUpDown();
            btnReadPosition = new Button();
            btnSendChanges = new Button();
            rbRestore = new RadioButton();
            rbMinimize = new RadioButton();
            rbNoChange = new RadioButton();
            label2 = new Label();
            txtYCoord = new TextBox();
            cmbProcesses = new ComboBox();
            label1 = new Label();
            txtXCoord = new TextBox();
            tabSettings = new TabPage();
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
            tabControls.SuspendLayout();
            tabMovements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).BeginInit();
            tabSettings.SuspendLayout();
            tabDebug.SuspendLayout();
            SuspendLayout();
            // 
            // tabControls
            // 
            tabControls.Controls.Add(tabMovements);
            tabControls.Controls.Add(tabSettings);
            tabControls.Controls.Add(tabDebug);
            tabControls.Location = new Point(21, 24);
            tabControls.Name = "tabControls";
            tabControls.SelectedIndex = 0;
            tabControls.Size = new Size(333, 352);
            tabControls.TabIndex = 0;
            // 
            // tabMovements
            // 
            tabMovements.Controls.Add(button1);
            tabMovements.Controls.Add(textBox1);
            tabMovements.Controls.Add(nudInstance);
            tabMovements.Controls.Add(btnReadPosition);
            tabMovements.Controls.Add(btnSendChanges);
            tabMovements.Controls.Add(rbRestore);
            tabMovements.Controls.Add(rbMinimize);
            tabMovements.Controls.Add(rbNoChange);
            tabMovements.Controls.Add(label2);
            tabMovements.Controls.Add(txtYCoord);
            tabMovements.Controls.Add(cmbProcesses);
            tabMovements.Controls.Add(label1);
            tabMovements.Controls.Add(txtXCoord);
            tabMovements.Location = new Point(4, 24);
            tabMovements.Name = "tabMovements";
            tabMovements.Padding = new Padding(3);
            tabMovements.Size = new Size(325, 324);
            tabMovements.TabIndex = 0;
            tabMovements.Text = "Windows Control";
            tabMovements.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(215, 279);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 12;
            button1.Text = "Log Text";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(46, 279);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(146, 23);
            textBox1.TabIndex = 11;
            textBox1.Text = "Log Text";
            // 
            // nudInstance
            // 
            nudInstance.Location = new Point(215, 24);
            nudInstance.Name = "nudInstance";
            nudInstance.Size = new Size(60, 23);
            nudInstance.TabIndex = 10;
            // 
            // btnReadPosition
            // 
            btnReadPosition.Location = new Point(183, 235);
            btnReadPosition.Name = "btnReadPosition";
            btnReadPosition.Size = new Size(107, 23);
            btnReadPosition.TabIndex = 9;
            btnReadPosition.Text = "Read Position";
            btnReadPosition.UseVisualStyleBackColor = true;
            btnReadPosition.Click += btnReadPosition_Click;
            // 
            // btnSendChanges
            // 
            btnSendChanges.Location = new Point(40, 235);
            btnSendChanges.Name = "btnSendChanges";
            btnSendChanges.Size = new Size(106, 23);
            btnSendChanges.TabIndex = 8;
            btnSendChanges.Text = "Send Changes";
            btnSendChanges.UseVisualStyleBackColor = true;
            btnSendChanges.Click += btnSendChanges_Click;
            // 
            // rbRestore
            // 
            rbRestore.AutoSize = true;
            rbRestore.Location = new Point(244, 179);
            rbRestore.Name = "rbRestore";
            rbRestore.Size = new Size(64, 19);
            rbRestore.TabIndex = 7;
            rbRestore.Text = "Restore";
            rbRestore.UseVisualStyleBackColor = true;
            // 
            // rbMinimize
            // 
            rbMinimize.AutoSize = true;
            rbMinimize.Location = new Point(153, 179);
            rbMinimize.Name = "rbMinimize";
            rbMinimize.Size = new Size(74, 19);
            rbMinimize.TabIndex = 6;
            rbMinimize.Text = "Minimize";
            rbMinimize.UseVisualStyleBackColor = true;
            // 
            // rbNoChange
            // 
            rbNoChange.AutoSize = true;
            rbNoChange.Checked = true;
            rbNoChange.Location = new Point(40, 179);
            rbNoChange.Name = "rbNoChange";
            rbNoChange.Size = new Size(85, 19);
            rbNoChange.TabIndex = 5;
            rbNoChange.TabStop = true;
            rbNoChange.Text = "No Change";
            rbNoChange.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(35, 123);
            label2.Name = "label2";
            label2.Size = new Size(14, 15);
            label2.TabIndex = 4;
            label2.Text = "Y";
            // 
            // txtYCoord
            // 
            txtYCoord.Location = new Point(71, 120);
            txtYCoord.Name = "txtYCoord";
            txtYCoord.Size = new Size(100, 23);
            txtYCoord.TabIndex = 3;
            txtYCoord.Text = "15";
            // 
            // cmbProcesses
            // 
            cmbProcesses.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProcesses.FormattingEnabled = true;
            cmbProcesses.Items.AddRange(new object[] { "Calc", "Notepad", "WarThunderExporter", "TelemetryVibShaker", "FalconExporter", "PerformanceMonitor", "RemoteWindowControl", "SimConnectExporter", "HWiNFO64" });
            cmbProcesses.Location = new Point(71, 24);
            cmbProcesses.Name = "cmbProcesses";
            cmbProcesses.Size = new Size(121, 23);
            cmbProcesses.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(35, 83);
            label1.Name = "label1";
            label1.Size = new Size(14, 15);
            label1.TabIndex = 1;
            label1.Text = "X";
            // 
            // txtXCoord
            // 
            txtXCoord.Location = new Point(71, 80);
            txtXCoord.Name = "txtXCoord";
            txtXCoord.Size = new Size(100, 23);
            txtXCoord.TabIndex = 0;
            txtXCoord.Text = "10";
            // 
            // tabSettings
            // 
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
            tabSettings.Size = new Size(325, 324);
            tabSettings.TabIndex = 1;
            tabSettings.Text = "Settings";
            tabSettings.UseVisualStyleBackColor = true;
            // 
            // chkUseEfficiencyCoresOnly
            // 
            chkUseEfficiencyCoresOnly.AutoSize = true;
            chkUseEfficiencyCoresOnly.Checked = true;
            chkUseEfficiencyCoresOnly.CheckState = CheckState.Checked;
            chkUseEfficiencyCoresOnly.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            chkUseEfficiencyCoresOnly.ForeColor = Color.FromArgb(91, 155, 213);
            chkUseEfficiencyCoresOnly.Location = new Point(21, 228);
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
            btnStartWebServer.Location = new Point(153, 283);
            btnStartWebServer.Name = "btnStartWebServer";
            btnStartWebServer.Size = new Size(126, 23);
            btnStartWebServer.TabIndex = 9;
            btnStartWebServer.Text = "Start Web Server";
            btnStartWebServer.UseVisualStyleBackColor = true;
            btnStartWebServer.Click += btnStartWebServer_Click;
            // 
            // txtHTML
            // 
            txtHTML.Location = new Point(153, 175);
            txtHTML.Name = "txtHTML";
            txtHTML.Size = new Size(126, 23);
            txtHTML.TabIndex = 8;
            txtHTML.Text = ".\\Remote.Control.html";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(44, 178);
            label6.Name = "label6";
            label6.Size = new Size(93, 15);
            label6.TabIndex = 7;
            label6.Text = "HTML Template:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(21, 24);
            label5.Name = "label5";
            label5.Size = new Size(116, 15);
            label5.TabIndex = 6;
            label5.Text = "Web Server Listen IP:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(43, 65);
            label4.Name = "label4";
            label4.Size = new Size(94, 15);
            label4.TabIndex = 5;
            label4.Text = "Web Server Port:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(16, 106);
            label3.Name = "label3";
            label3.Size = new Size(121, 15);
            label3.TabIndex = 4;
            label3.Text = "Web Server Thread Id:";
            // 
            // lblLink
            // 
            lblLink.AutoSize = true;
            lblLink.Location = new Point(153, 140);
            lblLink.Name = "lblLink";
            lblLink.Size = new Size(60, 15);
            lblLink.TabIndex = 3;
            lblLink.TabStop = true;
            lblLink.Text = "linkLabel1";
            // 
            // lblWebServerThreadId
            // 
            lblWebServerThreadId.AutoSize = true;
            lblWebServerThreadId.Location = new Point(153, 106);
            lblWebServerThreadId.Name = "lblWebServerThreadId";
            lblWebServerThreadId.Size = new Size(22, 15);
            lblWebServerThreadId.TabIndex = 2;
            lblWebServerThreadId.Text = "---";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(153, 62);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(126, 23);
            txtPort.TabIndex = 1;
            txtPort.Text = "8081";
            txtPort.TextChanged += txtPort_TextChanged;
            // 
            // txtIPAddress
            // 
            txtIPAddress.Location = new Point(153, 21);
            txtIPAddress.Name = "txtIPAddress";
            txtIPAddress.Size = new Size(126, 23);
            txtIPAddress.TabIndex = 0;
            txtIPAddress.Text = "192.168.1.5";
            txtIPAddress.TextChanged += txtIPAddress_TextChanged;
            // 
            // tabDebug
            // 
            tabDebug.Controls.Add(txtDebug);
            tabDebug.Location = new Point(4, 24);
            tabDebug.Name = "tabDebug";
            tabDebug.Size = new Size(325, 324);
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
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(376, 391);
            Controls.Add(tabControls);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "frmMain";
            Text = "Window Remote Control";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            tabControls.ResumeLayout(false);
            tabMovements.ResumeLayout(false);
            tabMovements.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).EndInit();
            tabSettings.ResumeLayout(false);
            tabSettings.PerformLayout();
            tabDebug.ResumeLayout(false);
            tabDebug.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControls;
        private TabPage tabMovements;
        private TabPage tabSettings;
        private Label label1;
        private TextBox txtXCoord;
        private Label label2;
        private TextBox txtYCoord;
        private ComboBox cmbProcesses;
        private RadioButton rbMinimize;
        private RadioButton rbNoChange;
        private RadioButton rbRestore;
        private Button btnSendChanges;
        private Button btnReadPosition;
        private NumericUpDown nudInstance;
        private TextBox txtIPAddress;
        private TextBox txtPort;
        private TabPage tabDebug;
        private TextBox txtDebug;
        private Label lblWebServerThreadId;
        private LinkLabel lblLink;
        private TextBox textBox1;
        private Label label3;
        private Label label5;
        private Label label4;
        private TextBox txtHTML;
        private Label label6;
        private Button btnStartWebServer;
        private Button button1;
        private CheckBox chkUseEfficiencyCoresOnly;
    }
}

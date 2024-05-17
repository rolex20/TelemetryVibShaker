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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
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
            tabPage2 = new TabPage();
            lblLink = new LinkLabel();
            lblWebServerThreadId = new Label();
            txtPort = new TextBox();
            txtIPAddress = new TextBox();
            tabPage3 = new TabPage();
            txtErrors = new TextBox();
            textBox1 = new TextBox();
            button1 = new Button();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).BeginInit();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Location = new Point(30, 34);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(393, 352);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(button1);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Controls.Add(nudInstance);
            tabPage1.Controls.Add(btnReadPosition);
            tabPage1.Controls.Add(btnSendChanges);
            tabPage1.Controls.Add(rbRestore);
            tabPage1.Controls.Add(rbMinimize);
            tabPage1.Controls.Add(rbNoChange);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(txtYCoord);
            tabPage1.Controls.Add(cmbProcesses);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(txtXCoord);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(385, 324);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // nudInstance
            // 
            nudInstance.Location = new Point(244, 25);
            nudInstance.Name = "nudInstance";
            nudInstance.Size = new Size(120, 23);
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
            cmbProcesses.Items.AddRange(new object[] { "Notepad", "calc.exe", "explorer.exe" });
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
            // tabPage2
            // 
            tabPage2.Controls.Add(lblLink);
            tabPage2.Controls.Add(lblWebServerThreadId);
            tabPage2.Controls.Add(txtPort);
            tabPage2.Controls.Add(txtIPAddress);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(385, 324);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // lblLink
            // 
            lblLink.AutoSize = true;
            lblLink.Location = new Point(109, 197);
            lblLink.Name = "lblLink";
            lblLink.Size = new Size(60, 15);
            lblLink.TabIndex = 3;
            lblLink.TabStop = true;
            lblLink.Text = "linkLabel1";
            // 
            // lblWebServerThreadId
            // 
            lblWebServerThreadId.AutoSize = true;
            lblWebServerThreadId.Location = new Point(103, 144);
            lblWebServerThreadId.Name = "lblWebServerThreadId";
            lblWebServerThreadId.Size = new Size(22, 15);
            lblWebServerThreadId.TabIndex = 2;
            lblWebServerThreadId.Text = "---";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(103, 83);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(126, 23);
            txtPort.TabIndex = 1;
            txtPort.Text = "8081";
            txtPort.TextChanged += txtPort_TextChanged;
            // 
            // txtIPAddress
            // 
            txtIPAddress.Location = new Point(103, 39);
            txtIPAddress.Name = "txtIPAddress";
            txtIPAddress.Size = new Size(126, 23);
            txtIPAddress.TabIndex = 0;
            txtIPAddress.Text = "192.168.1.17";
            txtIPAddress.TextChanged += txtIPAddress_TextChanged;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(txtErrors);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(385, 324);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "tabPage3";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // txtErrors
            // 
            txtErrors.Location = new Point(24, 46);
            txtErrors.Multiline = true;
            txtErrors.Name = "txtErrors";
            txtErrors.ReadOnly = true;
            txtErrors.Size = new Size(325, 248);
            txtErrors.TabIndex = 0;
            txtErrors.Text = "xxx";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(46, 279);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(146, 23);
            textBox1.TabIndex = 11;
            // 
            // button1
            // 
            button1.Location = new Point(215, 279);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 12;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(477, 429);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "frmMain";
            Text = "Window Remote Control";
            Load += frmMain_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudInstance).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
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
        private TabPage tabPage3;
        private TextBox txtErrors;
        private Label lblWebServerThreadId;
        private LinkLabel lblLink;
        private Button button1;
        private TextBox textBox1;
    }
}

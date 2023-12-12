


namespace TelemetryVibShaker
{
    public partial class frmMain : Form
    {



        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            updateVolumeMultiplier();
            updateEffectsTimeout();
        }

        private void updateVolumeMultiplier()
        {
            lblVolumeMultiplier.Text = trkVolumeMultiplier.Value.ToString() + "%";
        }

        private void trkVolumeMultiplier_Scroll(object sender, EventArgs e)
        {
            updateVolumeMultiplier();
        }

        private void btnStartListening_Click(object sender, EventArgs e)
        {
            // Check if files exist
            if (!CheckFileExists(txtSoundEffect1)) return;
            if (!CheckFileExists(txtSoundEffect2)) return;
            if (!CheckFileExists(txtJSON)) return;

            // Display stats if required
            if (chkChangeToMonitor.Checked) tabs.SelectTab(3);
        }



        private bool CheckFileExists(TextBox tb)
        {
            bool exist = File.Exists(tb.Text);
            if (!exist)
            {
                MessageBox.Show("The file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tb.SelectAll();
                tb.Focus();
            }
            return exist;
        }

        private void btnSoundEffect1_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtSoundEffect1, "Select an NWAVE compatible audio file", (String)btnSoundEffect1.Tag);
        }

        private void btnSoundEffect2_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtSoundEffect2, "Select an NWAVE compatible audio file", (String)btnSoundEffect2.Tag);
        }

        private void updateSelectedFile(TextBox tb, String title, String filter)
        {
            openFileDialog1.Filter = filter;
            openFileDialog1.FileName = tb.Text;
            openFileDialog1.Title = title;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                tb.Text = openFileDialog1.FileName;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            updateEffectsTimeout();
        }

        private void updateEffectsTimeout()
        {
            lblEffectTimeout.Text = trkEffectTimeout.Value.ToString() + " second(s)";
        }

        private void btnJSONFile_Click(object sender, EventArgs e)
        {
            updateSelectedFile(txtJSON, "Select an JSON file defining AoA for each aircraft", (String)btnJSONFile.Tag);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Visible = chkShowStatistics.Checked;
        }
    }
}
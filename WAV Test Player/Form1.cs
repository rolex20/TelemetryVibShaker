using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NAudio.CoreAudioApi;
using NAudio.Wave;


namespace WAV_Test_Player
{
    public partial class Form1 : Form
    {

        WaveOut waveOut;
        AudioFileReader waveProvider;
        

        int deviceIndex = 0;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // create a device enumerator
            var enumerator = new MMDeviceEnumerator();

            // get the index of the device named "Output Sound Device 2"
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];

                cmbSoundDevice.Items.Add(device.FriendlyName);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            // create a device enumerator
            var enumerator = new MMDeviceEnumerator();

            // get the index of the device named "Output Sound Device 2"
            int deviceIndex = cmbSoundDevice.SelectedIndex;

            // check if the device was found
            if (deviceIndex == -1)
            {
                MessageBox.Show("Audio device not found");
                return;
            }


            // create a wave out object and set the device number
            waveOut = new WaveOut();
            waveOut.DeviceNumber = deviceIndex;

            // create a wave provider from a file

            var waveProvider = new AudioFileReader(txtWAVFile.Text);


            // create a loop stream from the wave provider
            var loopStream = new LoopStream(waveProvider);

            // initialize the wave out object with the loop stream
            waveOut.Init(loopStream);


            // set the initial volume to 100%
            waveOut.Volume = tbVolume.Value / 100.0f;


            waveOut.Play();


        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (waveOut != null) { 
            waveOut.Stop();
            waveOut.Dispose();
            waveOut = null;
            }
        }

        private void txtSoundDevice_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbVolume_Scroll(object sender, EventArgs e)
        {
            lblVolume.Text = tbVolume.Value.ToString() + "%";
            if (waveOut != null)
                waveOut.Volume = tbVolume.Value / 100.0f; ;
        }  
    }
}

using System;
using System.Windows.Forms;

using NAudio.CoreAudioApi;
using NAudio.Wave;


namespace WAV_Test_Player
{
    public partial class Form1 : Form
    {

        WaveOut waveOut1, waveOut2;
        AudioFileReader waveProvider1, waveProvider2;
        LoopStream loopStream1, loopStream2;

        

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
            waveOut1 = new WaveOut();
            waveOut1.DeviceNumber = deviceIndex;

            // create a wave provider from a file
            waveProvider1 = new AudioFileReader(txtWAVFile.Text);

            // create a loop stream from the wave provider
            loopStream1 = new LoopStream(waveProvider1);

            // initialize the wave out object with the loop stream
            waveOut1.Init(loopStream1);

            // set the initial volume to 100%
            waveOut1.Volume = tbVolume.Value / 100.0f;

            waveOut1.Play();


            // create a wave out object and set the device number
            waveOut2 = new WaveOut();
            waveOut2.DeviceNumber = deviceIndex;

            // create a wave provider from a file
            waveProvider2 = new AudioFileReader(txtWAVFile2.Text);

            // create a loop stream from the wave provider
            loopStream2 = new LoopStream(waveProvider2);

            // initialize the wave out object with the loop stream
            waveOut2.Init(loopStream2);

            // set the initial volume to 100%
            waveOut2.Volume = tbVolume2.Value / 100.0f;

            waveOut2.Play();



        }

        private void tbVolume2_Scroll(object sender, EventArgs e)
        {
            lblVolume2.Text = tbVolume2.Value.ToString() + "%";
            float newvolume = tbVolume2.Value / 100.0f;
            if (waveOut2 != null)
                waveOut2.Volume = newvolume;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (waveOut1 != null) { 
            waveOut1.Stop();
            waveOut1.Dispose();
            waveOut1 = null;
            }
        }

        private void txtSoundDevice_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbVolume_Scroll(object sender, EventArgs e)
        {
            lblVolume.Text = tbVolume.Value.ToString() + "%";
            float newvolume = tbVolume.Value / 100.0f;
            if (waveOut1 != null)
                waveOut1.Volume = newvolume ;
        }  
    }
}

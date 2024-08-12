﻿using NAudio.Wave;


namespace PerformanceMonitor
{
    internal class MediaPlayer
    {
        public MediaPlayer(int AudioDeviceIndex) {
            volume = 1.0f; // i.e. 100%
            deviceIndex = AudioDeviceIndex;
        }

        private float volume; // the backing field
        public float Volume // Valid values between 0.0f and 1.0f
        {
            get { return volume; }
            set {
                    volume = value; 
                    if (waveProvider != null && waveProvider.Volume != volume)
                        waveProvider.Volume = volume; 
            }
        }

        private WaveOut waveOut = null;
        private int deviceIndex = 0;
        private string audioFile= null;
        private AudioFileReader waveProvider = null;
        private LoopStream loopStream = null;
        public void Open(string AudioFilePath)
        {
            audioFile = AudioFilePath;
        }

        public void PlayLooping()
        {
            // create a wave out object and set the device number
            waveOut = new WaveOut();
            waveOut.DeviceNumber = deviceIndex;
            
            waveProvider = new AudioFileReader(audioFile); // create a wave provider from a file
            loopStream = new LoopStream(waveProvider); // create a loop stream from the wave provider
            waveOut.Init(loopStream); // initialize the wave out object with the loop stream
            waveOut.Volume = 1.0f; // this is the volume of the whole device, don't use this to control each sound
            waveProvider.Volume = volume; // this is what should be used to control the volume of each sound file
            waveOut.Play();
        }

        public void Stop()
        {
            if (waveOut == null) return;            
            waveOut.Stop();
        }

        public void Dispose()
        {
            if (waveOut == null) return;
            waveOut.Dispose();
        }

        public void ChangeAudioDevice(int AudioDeviceIndex)
        {
            deviceIndex = AudioDeviceIndex;
            waveOut.DeviceNumber = deviceIndex;
        }
    }
}

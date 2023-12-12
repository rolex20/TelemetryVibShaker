using NAudio.Wave;


namespace TelemetryVibShaker
{
    internal class MediaPlayer
    {
        public MediaPlayer(int AudioDeviceIndex) {
            volume = 1.0f; // i.e. 100%
            deviceIndex = AudioDeviceIndex;
        }

        private float volume; // the backing field
        public float Volume // Valid values between 0 and 1
        {
            get { return volume; }
            set { if (value>=0.0f && value<=1.0f) volume = value; }
        }

        private WaveOut? waveOut = null;
        private AudioFileReader? waveProvider = null;
        private int deviceIndex = 0;
        private String? audioFile= null;
        public void Open(string AudioFilePath)
        {
            audioFile = AudioFilePath;
        }

        public void PlayLooping()
        {
            // create a wave out object and set the device number
            waveOut = new WaveOut();
            waveOut.DeviceNumber = deviceIndex;
            
            var waveProvider = new AudioFileReader(audioFile); // create a wave provider from a file
            var loopStream = new LoopStream(waveProvider); // create a loop stream from the wave provider
            waveOut.Init(loopStream); // initialize the wave out object with the loop stream
            waveOut.Volume = volume; // set the initial volume
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
    }
}

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.Json;
using System.Text;

namespace TelemetryVibShaker
{
    internal class TelemetryServer
    {
        private string currentUnitType; // current type of aircraft used by the player
        public string CurrentUnitInfo { get; } // AoA ranges information for the current type of aircraft used by the player
        private string ErrorMsg { get; } 

        private long lastSecond; // second of the last received datagram
        private float lastAoA;  // last AoA correctly parsed
        private int dps; // datagrams received per second

        private AoA_SoundManager soundManager;

        private string JSON_FilePath; // JSON file path

        private bool CancelationToken;
        private bool Statistics;

        public void Stop()
        {
            cancelationToken = true;
        }

        public TelemetryServer(AoA_SoundManager sm, string json, bool calculateStatistics)
        {
            soundManager = sm;
            JSON_FilePath = json;
            Statistics = calculateStatistics;
        }

        public static bool TestJSONFile(string JsonFilePath)
        {
            bool result = false;
            string json = File.ReadAllText(JsonFilePath);
            try
            {
                // Try to parse with a test
                // Throw exception if there is a problem with the .json file                
                private Root JSONroot = JsonSerializer.Deserialize<Root>(json);
                string datagram = "F-16C_50";  // At least this test unit must exist in .JSON file
                var unit = JSONroot.units.unit.FirstOrDefault(u => u.typeName == datagram);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("There was a problem with the .JSON file, please check the file.  " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return (unit != null);
        }

        public void Run()
        {
            cancelationToken = false;
            currentUnitType = "";

            // Creates the UDP socket
            UdpClient listenerUdp = new UdpClient(int.Parse(txtListeningPort.Text));

            StopWatch stopwatch = new StopWatch();

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            byte[] receiveData;

            long lastSecond;

            while (listenerUdp != null)
            {
                try
                {
                    receiveData = null;
                    receiveData = listenerUdp.Receive(ref sender); // Wait here until a new datagram is received
                }
                catch (Exception ex)
                {
                    if (cancelationToken) // The user requested to stop
                    {
                        btnStop.Tag = false; // Stop-request clear
                        return; // Stop listenning
                    }

                    // Some error did happened
                    ErrorMsg = ex.Message;
                    return; // Stop listening
                }


                // Process the datagram received


                stopwatch.Restart();  // Track the time to process this datagram
                bool needs_update = false;

                string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);

                // Update statistics and UI status controls
                long newSecond = Environment.TickCount64 / 1000;
                if (lastSecond != newSecond && Statistics)
                {
                    //lblDatagramsPerSecond.Text = dps.ToString();  // update datagrams per second
                    BeginInvoke(new Action(() => { lblDatagramsPerSecond.Text = dps.ToString();  /* update datagrams per second  */ }));
                    dps = 1; // reset the counter
                    lastSecond = newSecond;
                    needs_update = true;  // Update required for statistics, but only if the user wants to see them
                }
                else
                {
                    needs_update = false;
                    dps++;
                }

                // Always process each datagram received
                if (Single.TryParse(datagram, out float AoA))
                {
                    if (soundManager.UpdateEffect(AoA))
                    {
                        // If volume has changed after updating the AoA then update UI
                        // right now, this UI status is updated more than once per second, but only if the new effect status has changed
                        if (soundManager.SoundIsActive())
                            UpdateSoundEffectStatus(EffectStatus.PlayingEffect);
                        else
                            UpdateSoundEffectStatus(EffectStatus.SoundEffectsReady);
                    }


                    // Track lastAoA even if a second has not yet been completed
                    // This is to make sure we report the last AoA received, at least, in timer1()
                    if (lastAoA != AoA)
                        lastAoA = AoA;

                }
                else  // If not numeric, then datagram received must be an aircraft type name
                {
                    var unit = JSONroot.units.unit.FirstOrDefault(u => u.typeName == datagram);
                    string lastUnitType = datagram;

                    if (unit != null)  // If found, use the limits defined in the JSON file
                    {
                        soundManager.AoA1 = unit.AoA1;
                        soundManager.AoA2 = unit.AoA2;
                    }
                    else  // basically ignore the limits, because the unit type was not found in the JSON File
                    {
                        soundManager.AoA1 = 360;
                        soundManager.AoA2 = 360;
                    }

                    if (currentUnitType != lastUnitType) // this is not expected to change often, so I am ok with updating it as soon as possible
                    {
                        currentUnitType = lastUnitType;
                        CurrentUnitInfo = currentUnitType + $"({soundManager.AoA1},{soundManager.AoA2})";
                    }


                }
                stopwatch.Stop(); // at this point the datagram has been fully processed

                if (needs_update) // update the processing time once every second only and if requested by user
                {
                    TimeSpan elapsed = stopwatch.Elapsed;
                    if ((int)lblProcessingTime.Tag < elapsed.Milliseconds)
                    {
                        lblProcessingTime.Tag = elapsed.Milliseconds;
                        BeginInvoke(new Action(() => { lblProcessingTime.Text = elapsed.Milliseconds.ToString(); }));
                    }
                }
            } // end-while


}
        
    }

}

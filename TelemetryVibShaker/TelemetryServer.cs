using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using TelemetryVibShaker;
using System.Diagnostics;
using System.Xml.XPath;

namespace TelemetryVibShaker
{
    internal class TelemetryServer
    {
        public long lastSecond; // second of the last received datagram
        public int lastAoA;  // last AoA correctly parsed
        public int dps; // datagrams received per second
        public int maxProcessingTime; // time of the datagram that took longer to process

        SoundEffectStatus soundEffectStatus;
        public SoundEffectStatus SoundEffectStatus { get { return soundEffectStatus; } }


        public string CurrentUnitType; // current type of aircraft used by the player
        public string CurrentUnitInfo { 
            get 
            {
                return (soundManager == null)? string.Empty: CurrentUnitType + $"({soundManager.AoA1},{soundManager.AoA2})"; // AoA ranges information for the current type of aircraft used by the player
            } 
        } 

        private string lastErrorMsg;
        public string LastErrorMsg { get { return lastErrorMsg; } }


        private AoA_SoundManager soundManager;

        private Root jsonRoot;

        private bool cancelationToken;
        private bool statistics;
        private int listeningPort;

        public void Stop()
        {
            cancelationToken = true;
        }

        public bool IsRunning()
        {
            return (!cancelationToken);
        }

        public TelemetryServer(AoA_SoundManager SoundManager, bool CalculateStats, int ListeningPort)
        {
            soundManager = SoundManager;
            jsonRoot = null;
            statistics = CalculateStats;
            this.listeningPort = ListeningPort;
            lastErrorMsg = string.Empty;
            cancelationToken = false;
        }

        public bool SetJSON(string FilePath)
        {
            bool result = false;
            // Check and parse .json file
            // Throw exception if there is a problem
            try
            {
                string json = File.ReadAllText(FilePath);
                jsonRoot = JsonSerializer.Deserialize<Root>(json);
                result = true;
            }
            catch (Exception ex)
            {
                lastErrorMsg = "There was a problem with the .JSON file, please check the file: " + ex.Message;
                result = false;
            }
            return result;
        }


        public void Run()
        {
            cancelationToken = false;
            CurrentUnitType = "none";
            lastErrorMsg = string.Empty;
            maxProcessingTime = 0;

            // Creates the UDP socket
            UdpClient listenerUdp = new UdpClient(listeningPort);

            Stopwatch stopwatch = new Stopwatch();

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            byte[] receiveData;

            lastSecond = 0;

            while (listenerUdp != null)
            {
                try
                {
                    receiveData = null;
                    receiveData = listenerUdp.Receive(ref sender); // Wait here until a new datagram is received
                }
                catch (Exception ex)
                {
                    if (cancelationToken) // The user requested to stop in another thread (UI Thread)
                        return; // Stop listenning

                    // Some error did happened
                    lastErrorMsg = ex.Message;
                    return; // Stop listening
                }


                // Process the datagram received


                stopwatch.Restart();  // Track the time to process this datagram
                bool needs_update = false;

                string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);

                // Update statistics and UI status controls
                long newSecond = Environment.TickCount64 / 1000;
                if (lastSecond != newSecond && statistics)
                {
                    //lblDatagramsPerSecond.Text = dps.ToString();  // update datagrams per second
                    //BeginInvoke(new Action(() => { lblDatagramsPerSecond.Text = dps.ToString();  /* update datagrams per second  */ }));
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
                if (Int32.TryParse(datagram, out int AoA))
                {
                    if (soundManager.UpdateEffect(AoA))
                    {
                        // If volume has changed after updating the AoA then update UI
                        // right now, this UI status is updated more than once per second, but only if the new effect status has changed
                        if (soundManager.SoundIsActive())
                            soundEffectStatus = SoundEffectStatus.Playing;
                        else
                            soundEffectStatus = SoundEffectStatus.Ready;
                    }


                    // Track lastAoA even if a second has not yet been completed
                    // This is to make sure we report the last AoA received, at least, in timer1()
                    if (lastAoA != AoA) lastAoA = AoA;

                }
                else  // If not numeric, then datagram received must be an aircraft type name
                {
                    var unit = jsonRoot.units.unit.FirstOrDefault(u => u.typeName == datagram);
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

                    if (CurrentUnitType != lastUnitType) // this is not expected to change often, so I am ok with updating it as soon as possible
                    {
                        CurrentUnitType = lastUnitType;
                    }


                }
                stopwatch.Stop(); // at this point the datagram has been fully processed

                if (needs_update) // update the processing time once every second only and if requested by user
                {
                    TimeSpan elapsed = stopwatch.Elapsed;
                    if (maxProcessingTime < elapsed.Milliseconds)
                        maxProcessingTime = elapsed.Milliseconds;
                }
            } // end-while


        } // Run()

        public static string TestJSONFile(string JsonFilePath)
        {
            string result = string.Empty;
            string json = File.ReadAllText(JsonFilePath);
            string intro = "There was a problem with the .JSON file, please check the file.  ";

            try
            {
                // Try to parse with a test
                // Throw exception if there is a problem with the .json file                
                Root JSONroot = JsonSerializer.Deserialize<Root>(json);
                string datagram = "F-16C_50";  // At least this test unit must exist in .JSON file
                var unit = JSONroot.units.unit.FirstOrDefault(u => u.typeName == datagram);

                result = (unit != null) ? string.Empty : intro;
            }
            catch (Exception ex)
            {
                result = intro + ex.Message;
            }
            return result;          
        }


        
    } // class TelemetryServer

} // namespace

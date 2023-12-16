using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Diagnostics;


namespace TelemetryVibShaker
{
    internal class TelemetryServer
    {
        public long LastSecond; // second of the last received datagram
        public TelemetryData LastData; // last telemetry datagram received
        public int DPS; // datagrams received per second
        public int MaxProcessingTime; // time of the datagram that took longer to process
        public string CurrentUnitType; // current type of aircraft used by the player

        private AoA_SoundManager soundManager;
        private MotorController[] vibMotor;
        private Root jsonRoot;
        private bool cancelationToken;
        private bool statistics;
        private int listeningPort;

        public string CurrentUnitInfo { 
            get 
            {
                return (soundManager == null)? string.Empty: CurrentUnitType + $"({soundManager.AoA1},{soundManager.AoA2})"; // AoA ranges information for the current type of aircraft used by the player
            } 
        } 

        private string lastErrorMsg;
        public string LastErrorMsg { get { return lastErrorMsg; } }



        public void Abort()
        {
            cancelationToken = true; // it might not be immediate, but it's simpler and it works
        }

        public bool IsRunning()
        {
            return (!cancelationToken);
        }

        public TelemetryServer(AoA_SoundManager SoundManager, MotorController[] Motor, bool CalculateStats, int ListeningPort)
        {
            soundManager = SoundManager;
            vibMotor = Motor;
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
            MaxProcessingTime = 0;

            // Creates the UDP socket
            UdpClient listenerUdp = new UdpClient(listeningPort);

            Stopwatch stopwatch = new Stopwatch();

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            byte[] receiveData;

            LastSecond = 0;

            // Establish UDP target to send packets
            for (int i = 0; i < vibMotor.Length; i++)
                vibMotor[i].Connect();


            while (!cancelationToken && listenerUdp != null)
            {
                try
                {
                    receiveData = listenerUdp.Receive(ref sender); // Wait here until a new datagram is received
                }
                catch (Exception ex)
                {
                    if (cancelationToken) // The user requested to stop in another thread (UI Thread)
                        return; // Abort listenning

                    // Some error did happened
                    lastErrorMsg = ex.Message;
                    return; // Abort listening
                }


                // Process the datagram received


                if (statistics) stopwatch.Restart();  // Track the time to process this datagram
                bool needs_update = false;

                //string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);

                // Update statistics and UI status controls
                long newSecond = Environment.TickCount64 / 1000;
                if (statistics)
                    if (LastSecond != newSecond )
                    {
                        //lblDatagramsPerSecond.Text = DPS.ToString();  // update datagrams per second
                        //BeginInvoke(new Action(() => { lblDatagramsPerSecond.Text = DPS.ToString();  /* update datagrams per second  */ }));
                        DPS = 1; // reset the counter
                        LastSecond = newSecond;
                        needs_update = true;  // Update required for statistics, but only if the user wants to see them
                    }
                    else
                    {
                        needs_update = false;
                        DPS++;
                    }

                // Always process each datagram received
                // datagram is composed of: AoA, SpeedBrakes, Flaps (each one in one byte)
                // AoA possible values: 0-255
                // SpeedBreaks possible values: 0-100
                // Flaps possible values: 0-100

                if (receiveData.Length == 3)
                {
                    // Obtain telemetry data
                    LastData.AoA = receiveData[0];
                    LastData.SpeedBrakes = receiveData[1];
                    LastData.Flaps = receiveData[2];

                    // Update the sound effects
                    soundManager.UpdateEffect(LastData.AoA);

                    // Update vibration-motors effects
                    for (int i = 0; i < vibMotor.Length; i++)
                        vibMotor[i].ProcessEffect(LastData);


                }
                else  // If not numeric, then datagram received must be an aircraft type name
                {
                    string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
                    var unit = jsonRoot.units.unit.FirstOrDefault(u => u.typeName == datagram);
                    string lastUnitType = datagram;

                    if (unit != null)  // If found, use the limits defined in the JSON file
                    {
                        soundManager.AoA1 = unit.AoA1;
                        soundManager.AoA2 = unit.AoA2;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(unit.AoA1, unit.AoA2);
                    }
                    else  // basically ignore the limits, because the unit type was not found in the JSON File
                    {
                        soundManager.AoA1 = 360;
                        soundManager.AoA2 = 360;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(360, 360);

                    }

                    // this is not expected to change often, so I am ok with updating it as soon as possible
                    if (CurrentUnitType != lastUnitType) CurrentUnitType = lastUnitType;
                }
                
                if (statistics) stopwatch.Stop(); // at this point the datagram has been fully processed
                if (needs_update) // update the processing time once every second only and if requested by user
                {                   
                    TimeSpan elapsed = stopwatch.Elapsed;
                    if (MaxProcessingTime < elapsed.Milliseconds) MaxProcessingTime = elapsed.Milliseconds;
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

﻿using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;



namespace TelemetryVibShaker
{
    internal class TelemetryServer
    {
        public long LastSecond; // second of the last received datagram
        public long TimeStamp; // timestamp of the last datagram received
        public TelemetryData LastData; // last telemetry datagram received
        public int DPS; // datagrams received per second
        public SimpleStatsCalculator Stats = new SimpleStatsCalculator(); // time of the datagram that took longer to process, this is per aircraft
        public int LastProcessorUsed; // processor used in the last udp packet received
        public int ThreadId; // OS Thread ID for the UDP Telemetry Server
        public string? CurrentUnitType; // current type of aircraft used by the player

        private AoA_SoundManager soundManager_AoA;
        private MotorController[] vibMotor;
        private Root? jsonRoot;
        private bool cancelationToken;
        private bool Statistics;
        private int listeningPort;
        private UdpClient? listenerUdp;
        public int MinSpeed; // km/h (below this speed, effects won't be active)
        public int MinAltitude; // Meters above the ground (below this Altitude, effects won't be active)



        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();
       
        //[DllImport("kernel32.dll")]
        //private static extern uint GetTickCount();

        //[DllImport("kernel32.dll")]
        //private static extern ulong GetTickCount64();


        public string CurrentUnitInfo { 
            get 
            {
                return (soundManager_AoA == null)? string.Empty: CurrentUnitType + $"({soundManager_AoA.AoA1},{soundManager_AoA.AoA2})"; // AoA ranges information for the current type of aircraft used by the player
            } 
        } 

        private string lastErrorMsg;
        public string LastErrorMsg { get { return lastErrorMsg; } }

        
        // With this property now I can efficiently track NotFounds
        private bool unitHasChanged = true;
        public bool UnitHaschanged {  get { 
                bool retValue = unitHasChanged;
                unitHasChanged = false;
                return retValue; 
            } }

        public void Abort()
        {
            cancelationToken = true; // it might not be immediate, but it's simpler and it works

            //force the udp server to abort
            if (listenerUdp!=null)
            {
                listenerUdp.Close();
                listenerUdp = null;
            }
        }

        public bool IsRunning()
        {
            return (!cancelationToken);
        }

        public TelemetryServer(AoA_SoundManager SoundManager, MotorController[] Motor, bool CalculateStats, int ListeningPort)
        {
            soundManager_AoA = SoundManager;
            vibMotor = Motor;
            jsonRoot = null;
            Statistics = CalculateStats;
            this.listeningPort = ListeningPort;
            lastErrorMsg = string.Empty;
            cancelationToken = false;
            LastData = new TelemetryData();
            listenerUdp = null;
            MinSpeed = 0;
            MinAltitude = 0;
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

        private void Log(string message)
        {
            string tempPath = Path.GetTempPath();
            string logFilePath = Path.Combine(tempPath, "log.txt");

            // Including milliseconds in the timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";

            File.AppendAllText(logFilePath, logEntry);
        }

        public void Run()
        {
            cancelationToken = false;
            CurrentUnitType = "[no unit information received]";
            lastErrorMsg = string.Empty;
            Stats.Initialize();
            LastProcessorUsed = -1;
            DPS = 0;
            int iDPS = 0; // intermediate DPS

            // Creates the UDP socket
            listenerUdp = new UdpClient(listeningPort);

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
                    ThreadId = (int)GetCurrentThreadId(); // Let's see if it changes on receiving packets
                    receiveData = listenerUdp.Receive(ref sender); // Wait here until a new datagram is received
                    if (Statistics) stopwatch.Restart();  // Track the time to process this datagram

                }
                catch (Exception ex)
                {
                    if (cancelationToken) // The user requested to stop in another thread (UI Thread)
                        return; // Abort listenning

                    // Some error did happen
                    lastErrorMsg = ex.Message;
                    return; // Abort listening
                }

                TimeStamp = Stopwatch.GetTimestamp();
                LastProcessorUsed = (int)GetCurrentProcessorNumber();
                long newSecond = Stopwatch.GetTimestamp() / Stopwatch.Frequency;


                // Even if Statistics==false, we need always to keep track of the LastSecond
                // and if we are doing this, let's also keep track of DPS
                if (LastSecond != newSecond)
                {
                    DPS = iDPS; // Only update when the last DPS per second has been calculated which is now
                    iDPS = 1; // reset the counter
                    LastSecond = newSecond;
                }
                else
                {
                    iDPS++;
                }
                

                // Always process each datagram received
                // datagram is composed of: AoA, SpeedBrakes, Flaps (each one in one byte)
                // AoA possible values: 0-255
                // SpeedBreaks possible values: 0-100
                // Flaps possible values: 0-100
                // Speed (optional): 0-255.  Units in 10th's of Km, so 10 is 100Km
                // G-Forces 
                // Altitud in decameters to fit in 1 byte
                // Gear: 0-100
                if (receiveData[0]==1) // is this a telemetry datagram with 8 bytes total?
                {
                    // Obtain telemetry data
                    LastData.AoA = receiveData[1];
                    LastData.SpeedBrakes = receiveData[2];
                    LastData.Flaps = receiveData[3];

                                       
                    /* Speed Reception and Conversion
                     * receivedData[3] is in decameters/s
                     * This unit was selected to make it fit in 8 bits (1 byte)
                     * Since it is received obviously without decimals, some resolution is lost but not needed in this program.
                     * To convert decameters/s -> km/h we use 
                     * km/h = decameters per second x 36
                     */
                     LastData.Speed = receiveData[4] * 36; // After this, Speed now is in km/h

                    // GForces 
                    LastData.GForces = receiveData[5];

                    // Altitude is sent in Decameters without decimals: some accuracy lost here, maximum is 2550 meters
                    LastData.Altitude = receiveData[6] * 10; // Now Altitude is in meters

                    // Gear 0-100
                    LastData.Gear = receiveData[7];

                    // Process the Effects only if the current plane is moving above the MinSpeed required by the user
                    if (LastData.Speed >= MinSpeed && LastData.Altitude >= MinAltitude) { 
                        
                        // Update the sound effects
                        soundManager_AoA.UpdateEffect(LastData.AoA);

                        // Update vibration-motors effects
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ProcessEffect(LastData);
                    } else if (soundManager_AoA.EffectsAreActive()) // if sound was active due to previous datagrams, we need to check if sound needs to be turned off now
                    {
                        // Update the sound effects
                        soundManager_AoA.UpdateEffect(LastData.AoA);
                    }



                }
                else  // If not, then datagram received must be an aircraft type name
                {
                    unitHasChanged = true;
                    string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
                    var unit = jsonRoot.units.unit.FirstOrDefault(u => u.typeName == datagram);
                    CurrentUnitType = datagram;

                    // Signal that we haven't received yet new AoA telemetry
                    LastData.AoA = -1; 
                    LastData.Speed = 0;
                    LastData.SpeedBrakes = -1;
                    LastData.Flaps = -1;
                    LastData.GForces = -1;
                    LastData.Altitude = -1;
                    LastData.Gear = -1;

                    // Reset MaxProcessingTime with each new airplane
                    Stats.Initialize();

                    // New aircraft, let's make sure, sounds are muted
                    soundManager_AoA.MuteEffects();

                    bool aircraftFound = (unit != null);
                    if (aircraftFound)  // If found, use the limits defined in the JSON file
                    {
                        soundManager_AoA.AoA1 = unit.AoA1;
                        soundManager_AoA.AoA2 = unit.AoA2;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(unit.AoA1, unit.AoA2); // ChangeAoARange() will check if this is an AoA effect-type

                        CurrentUnitType += "(" + soundManager_AoA.AoA1 + ", " + soundManager_AoA.AoA2 + ")";
                    }
                    else  // basically ignore the limits, because the unit type was not found in the JSON File
                    {
                        soundManager_AoA.AoA1 = 360;
                        soundManager_AoA.AoA2 = 360;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(360, 360);

                    }

                    soundManager_AoA.ScheduleAlarm(aircraftFound); // Sound Notification now and in 30 minutes
                }

                if (Statistics) 
                {
                    stopwatch.Stop(); // at this point the datagram has been fully processed
                    Stats.AddSample(stopwatch.Elapsed.Milliseconds);                    
                }

            } // end-while


        } // Run()

        public static string TestJSONFile(string JsonFilePath)
        {
            string result;// = string.Empty;
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

using System.Net;
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
        public int MaxProcessingTime; // time of the datagram that took longer to process
        public int LastProcessorUsed; // processor used in the last udp packet received
        public int ThreadId; // OS Thread ID for the UDP Telemetry Server
        public string CurrentUnitType; // current type of aircraft used by the player

        private AoA_SoundManager soundManager;
        private MotorController[] vibMotor;
        private Root jsonRoot;
        private bool cancelationToken;
        private bool Statistics;
        private int listeningPort;
        private UdpClient listenerUdp;
        public int MinSpeed; // km/h (below this speed, effects won't be active)

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();


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
            soundManager = SoundManager;
            vibMotor = Motor;
            jsonRoot = null;
            Statistics = CalculateStats;
            this.listeningPort = ListeningPort;
            lastErrorMsg = string.Empty;
            cancelationToken = false;
            LastData = new TelemetryData();
            listenerUdp = null;
            MinSpeed = 0;
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
            MaxProcessingTime = -1;
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
                    LastProcessorUsed = (int)GetCurrentProcessorNumber();
                }
                catch (Exception ex)
                {
                    if (cancelationToken) // The user requested to stop in another thread (UI Thread)
                        return; // Abort listenning

                    // Some error did happen
                    lastErrorMsg = ex.Message;
                    return; // Abort listening
                }


                /* Process the datagram received */



                // Update Statistics and UI status controls
                TimeStamp = Environment.TickCount64;
                long newSecond = TimeStamp / 1000;
                if (Statistics)
                {
                    if (LastSecond != newSecond)
                    {
                        //lblDatagramsPerSecond.Text = DPS.ToString();  // update datagrams per second
                        //BeginInvoke(new Action(() => { lblDatagramsPerSecond.Text = DPS.ToString();  /* update datagrams per second  */ }));
                        DPS = iDPS; // Only update when the last DPS per second has been calculated which is now
                        iDPS = 1; // reset the counter
                        LastSecond = newSecond;
                        //needs_update = true;  // Update required for Statistics, but only if the user wants to see them                        
                    }
                    else
                    {
                        //needs_update = false;
                        iDPS++;
                    }
                }

                // Always process each datagram received
                // datagram is composed of: AoA, SpeedBrakes, Flaps (each one in one byte)
                // AoA possible values: 0-255
                // SpeedBreaks possible values: 0-100
                // Flaps possible values: 0-100
                // Speed (optional): 0-255.  Units in 10th's of Km, so 10 is 100Km
                if ( receiveData.Length == 4)
                {
                    // Obtain telemetry data
                    LastData.AoA = receiveData[0];
                    LastData.SpeedBrakes = receiveData[1];
                    LastData.Flaps = receiveData[2];

                    
                    
                    /* Speed Reception and Conversion
                     * receivedData[3] is in decameters/s
                     * This unit was selected to make it fit in 8 bits (1 byte)
                     * Since it is received obviously without decimals, some resolution is lost but not needed in this program.
                     * To convert decameters/s -> km/h we use 
                     * km/h = decameters per second x 36
                     */
                     LastData.Speed = receiveData[3] * 36; // After this, Speed now is in km/h


                    // Process the Effects only if the current plane is moving above the MinSpeed required by the user
                    if (LastData.Speed >= MinSpeed) { 
                        
                        // Update the sound effects
                        soundManager.UpdateEffect(LastData.AoA);

                        // Update vibration-motors effects
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ProcessEffect(LastData);
                    }

                }
                else  // If not numeric, then datagram received must be an aircraft type name
                {
                    string datagram = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
                    var unit = jsonRoot.units.unit.FirstOrDefault(u => u.typeName == datagram);
                    CurrentUnitType = datagram;

                    // Signal that we haven't received yet new AoA telemetry
                    LastData.AoA = -1; 
                    LastData.Speed = 0;
                    LastData.SpeedBrakes = -1;
                    LastData.Flaps = -1;

                    MaxProcessingTime = -1; // Reset MaxProcessingTime with each new airplane

                    if (unit != null)  // If found, use the limits defined in the JSON file
                    {
                        soundManager.AoA1 = unit.AoA1;
                        soundManager.AoA2 = unit.AoA2;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(unit.AoA1, unit.AoA2); // ChangeAoARange() will check if this is an AoA effect-type
                    }
                    else  // basically ignore the limits, because the unit type was not found in the JSON File
                    {
                        soundManager.AoA1 = 360;
                        soundManager.AoA2 = 360;
                        for (int i = 0; i < vibMotor.Length; i++)
                            vibMotor[i].ChangeAoARange(360, 360);

                    }
                    
                }

                if (Statistics) 
                {
                    stopwatch.Stop(); // at this point the datagram has been fully processed
                    int elapsed = stopwatch.Elapsed.Milliseconds;
                    if (MaxProcessingTime < elapsed) MaxProcessingTime = elapsed;
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

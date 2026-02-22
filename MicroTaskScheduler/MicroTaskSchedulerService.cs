using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Media;
using IdealProcessorEnhanced;
using System.Runtime.InteropServices;

namespace MicroTaskScheduler
{
    
    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }


    public partial class MicroTaskSchedulerService : ServiceBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task antivirusDisableTask;
        private Task hourlyAlarmTask;
        private ProcessStartInfo startInfo;
        private IntPtr affinityMask = IntPtr.Zero;
        private ProcessorAssigner processorAssigner = null;  // Must be alive the while the program is running and is assigned only once if it is the right type of processor

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);


        public MicroTaskSchedulerService()
        {
            InitializeComponent();
        }

        private void AssignEfficiencyCoresOnly()
        {            
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // We need to know the number of processors available to determine if Hyperthreading and Efficient cores are enabled
            int cpuCount = 0;
            regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (regKey != null)
            {
                // The number of subkeys corresponds to the number of CPUs
                cpuCount = regKey.SubKeyCount;
            }
            regKey.Close();


            CpuType cpuType;
            if (processorName.Contains("12700K")) cpuType = CpuType.Intel_12700K;
            else if (processorName.Contains("14700K")) cpuType = CpuType.Intel_14700K;
            else cpuType = CpuType.Other;

            // Calculate affinity for efficient cores only
            IntPtr affinityMask = IntPtr.Zero;
            switch (cpuType)
            {
                case CpuType.Intel_12700K:
                    if (cpuCount == 20)
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);
                    }
                    break;
                case CpuType.Intel_14700K:
                    if (cpuCount == 28)
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | 1 << 23 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27);
                        if (processorAssigner == null) processorAssigner = new ProcessorAssigner(27);
                        SetNewIdealProcessor(27);
                    }
                    break;
                default:
                    //ignore
                    break;
            }

            if (affinityMask != IntPtr.Zero)
            {
                try
                {
                    // Set the CPU affinity to Efficient Cores only
                    Process currentProcess = Process.GetCurrentProcess();
                    currentProcess.ProcessorAffinity = affinityMask;
                }
                catch { } // Ignore
            }
        }

        private void SetNewIdealProcessor(uint maxProcNumber)
        {
            if (maxProcNumber <= 0)
            {
                //AddToLog(GetTickCount64(), "Invalid MaxProcessorNumber", "$SetNewIdealProcessor({maxProcNumber})", true);  // TODO ADD LOG
                return;
            }


            // My Intel 14700K has 8 performance cores and 12 efficiency cores.
            // CPU numbers 0-15 are performance
            // CPU numbers 16-27 are efficiency            
            uint newIdealProcessor = processorAssigner.GetNextProcessor();

            IntPtr currentThreadHandle = GetCurrentThread();
            int previousProcessor = (int)SetThreadIdealProcessor(currentThreadHandle, newIdealProcessor);

            if (previousProcessor < 0 || (previousProcessor > maxProcNumber))
            {
                //AddToLog(GetTickCount64(), "Failed to set Ideal Processor", "$SetNewIdealProcessor({maxProcNumber})", true); //TODO: Add Log
                return;
            }
        }

        
        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("MicroTaskScheduler service starting...", EventLogEntryType.Information);

            //provoke the file to be created so the user can edit it if they want to change the alarm interval or the sound file (first run only)
            if (Properties.Settings.Default.SoundAlarm == -1)
            {
                Properties.Settings.Default.SoundAlarm = 0; // default to the Casio Alarm sound if the user hasn't set it yet
                Properties.Settings.Default.AlarmInterval_ms = 3600000; // default to 1 hour in milliseconds if the user hasn't set it yet
                Properties.Settings.Default.Save();
            }

            PerformanceMonitor.CPU_QoS.SetHardAffinityProcess(PerformanceMonitor.CPU_QoS.CpuSetType.Efficiency);
            //AssignEfficiencyCoresOnly();

            string scriptPath = @"C:\Users\ralch\Desktop\DisableAntivirus.ps1";
            startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            cancellationTokenSource = new CancellationTokenSource();
            antivirusDisableTask = Task.Run(() => DisableAntivirus(cancellationTokenSource.Token));
            hourlyAlarmTask = Task.Run(() => HourlyAlarm(cancellationTokenSource.Token));
        }

        protected override void OnStop()
        {
            try
            {
                cancellationTokenSource.Cancel();
                antivirusDisableTask.Wait();
            }
            catch
            {
                // obviously the previous calls might generate an exception when the cancelation token is triggered by an onStop() request
                // we can ignore them
            }
            finally
            {
                EventLog.WriteEntry("MicroTaskScheduler service stopped.", EventLogEntryType.Information);
            }
        }

        private async Task DisableAntivirus(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ExecutePowerShellScript();
                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                }
            } catch
            {
                // obviously the previous calls might generate an exception when the cancelation token is triggered by an onStop() request
                // we can ignore them
            }
        }

        private async Task HourlyAlarm(CancellationToken cancellationToken)
        {

            var stream = Properties.Settings.Default.SoundAlarm == 0? Properties.Resources.Casio_Watch_Alarm: Properties.Resources.StartCredit;

            // -----------------------------------------------------------------
            // 1. STARTUP SEQUENCE
            // -----------------------------------------------------------------
            try
            {
                // Play Startup Credit
                //using (SoundPlayer spWelcome = new SoundPlayer(Properties.Resources.StartCredit))
                //{
                //    spWelcome.PlaySync();
                //}
                //await Task.Delay(2000, cancellationToken);

                // Play Startup Beep
                using (SoundPlayer sp = new SoundPlayer(stream))
                {
                    sp.PlaySync();
                    //await Task.Delay(2000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Startup Sound Error: {ex.Message}", EventLogEntryType.Warning);
            }


            // -----------------------------------------------------------------
            // 2. THE HOURLY LOOP (CASIO LOGIC)
            // -----------------------------------------------------------------
            int AlarmInterval_ms = Properties.Settings.Default.AlarmInterval_ms;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int msToWait = AlarmInterval_ms; // Default to user-defined interval

                    // Wait exactly until the next hour-on-the-clock, if the AlarmInterval_ms is set to 1 hour, otherwise just wait the specified interval
                    // There are 3,600,000 milliseconds in one hour.
                    int msTotalInHour = 60 * 60 * 1000; // relying here on compiler optimization to treat this as a constant and not recalculate every loop
                    if (AlarmInterval_ms == msTotalInHour)
                    {
                        // SNAPSHOT: Get the time exactly ONCE. 
                        // This prevents the "Double Chime" bug where the second changes while reading.
                        DateTime now = DateTime.Now;

                        // CASIO MATH: Ignore Date objects. Just look at the clock face.
                        // How many milliseconds have passed since the top of the hour?
                        // Formula: (Minutes * 60 * 1000) + (Seconds * 1000) + Milliseconds
                        int msPassed = (now.Minute * 60000) + (now.Second * 1000) + now.Millisecond;

                        // Calculate exact milliseconds remaining
                        msToWait = msTotalInHour - msPassed;
                    }

                    // SANITY CHECK: The Clamp (The fix for your crash)
                    // If the clock drifted or logic was off by 1ms, force it to 0.
                    if (msToWait < 0) msToWait = msTotalInHour;  // Fall back to a full hour from now if negative


                    // Wait for the rest of the hour
                    // We use 'int' here, which is safer than TimeSpan for small negative values
                    // Log msToWait for debugging purposes in the event log
                    EventLog.WriteEntry($"HourlyAlarm: Waiting {(msToWait/1000)/60} minutes until next chime.", EventLogEntryType.Information);
                    await Task.Delay(msToWait, cancellationToken);

                    // Log the time we are actually playing the chime for debugging purposes
                    EventLog.WriteEntry($"HourlyAlarm: Waking up to chime at {DateTime.Now:HH:mm:ss.fff}.", EventLogEntryType.Information);

                    // -----------------------------------------------------------------
                    // 3. PLAY ALARM
                    // -----------------------------------------------------------------
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // CRITICAL FIX: The 'using' block.
                        // This creates a fresh connection to the Audio Driver every hour.
                        // This solves the "Silence" issue.
                        using (SoundPlayer sp = new SoundPlayer(stream))
                        {
                                sp.PlaySync();
                                EventLog.WriteEntry($"sp.PlaySync() called and Task.Delay() follows.", EventLogEntryType.Information);
                                await Task.Delay(2000, cancellationToken); // Wait 2 seconds to push us past the "00:00" mark.
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping. Exit cleanly.
                    break;
                }
                catch (Exception ex)
                {
                    // Log line number and error, but keep the service running!
                    //EventLog.WriteEntry($"HourlyAlarm Error: {ex.Message}", EventLogEntryType.Error);
                    // EXTRACT LINE NUMBER
                    var st = new StackTrace(ex, true);
                    var frame = st.GetFrame(0);
                    int line = frame != null ? frame.GetFileLineNumber() : 0;

                    EventLog.WriteEntry($"HourlyAlarm CRASH at Line {line}. Exception: {ex.Message}\nStack: {ex.StackTrace}", EventLogEntryType.Error);


                    // If we crashed, wait 1 minute before trying again so we don't flood the logs
                    await Task.Delay(60000, cancellationToken);
                }
            }
        }
        private async Task HourlyAlarm(CancellationToken cancellationToken, double neitherwork)
        {
            try
            {
                // 1. Startup Sound (Keep existing logic)
                using (SoundPlayer spWelcome = new SoundPlayer(Properties.Resources.StartCredit))
                {
                    spWelcome.PlaySync(); // Use PlaySync to ensure it finishes
                }
                await Task.Delay(2000, cancellationToken); // Use Task.Delay instead of Thread.Sleep

                // 2. Startup Casio Beep
                using (SoundPlayer spCasioStart = new SoundPlayer(Properties.Resources.Casio_Watch_Alarm))
                {
                    spCasioStart.PlaySync();
                }
                await Task.Delay(2000, cancellationToken);

                // 3. The Hourly Loop
                while (!cancellationToken.IsCancellationRequested)
                {
                    DateTime now = DateTime.Now;
                    DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0).AddHours(1);
                    TimeSpan timeToNextHour = nextHour - now;

                    // Wait until the next hour
                    await Task.Delay(timeToNextHour, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // FIX: Create a NEW SoundPlayer instance every hour.
                        // This ensures a fresh connection to the audio driver.
                        using (SoundPlayer hourlyChime = new SoundPlayer(Properties.Resources.Casio_Watch_Alarm))
                        {
                            hourlyChime.Load(); // Ensure loaded
                            hourlyChime.PlaySync(); // PlaySync blocks this specific Task thread until sound is done
                        }

                        // Optional: Wait a bit to ensure we don't trigger twice if calculation was milliseconds off
                        await Task.Delay(2000, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown behavior, ignore
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"HourlyAlarm() Exception: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private async Task HourlyAlarm(CancellationToken cancellationToken, float doesntwork)
        {
            try
            {
                SoundPlayer spWelcome = new SoundPlayer(Properties.Resources.StartCredit);
                spWelcome.Play();
                Thread.Sleep(2000); // Wait a couple of seconds to avoid the startup beep being right on the edge of the next hour which would cause a double beep
                spWelcome = null;

                SoundPlayer spCasio = new SoundPlayer(Properties.Resources.Casio_Watch_Alarm);
                spCasio.Play();
                Thread.Sleep(2000); // Wait a couple of seconds to avoid the startup beep being right on the edge of the next hour which would cause a double beep

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Calculate the time remaining until the next hour
                    //TimeSpan timeToNextHour = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(DateTime.Now.Minute) - TimeSpan.FromSeconds(DateTime.Now.Second) - TimeSpan.FromMilliseconds(DateTime.Now.Millisecond);

                    DateTime now = DateTime.Now;
                    DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0).AddMinutes(10);
                    TimeSpan timeToNextHour = nextHour - now;

                    await Task.Delay(timeToNextHour, cancellationToken); // wait here and see ya in an hour

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        spCasio.Play();
                        Thread.Sleep(2000);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                // obviously the previous calls might generate an exception when the cancelation token is triggered by an onStop() request
                // we can ignore them
                EventLog.WriteEntry($"HourlyAlarm() Exception: {ex.Message}.  No more hourly alarms after this moment.", EventLogEntryType.Error);
            }
        }




        private void ExecutePowerShellScript()
        {
            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    if (affinityMask != IntPtr.Zero) process.ProcessorAffinity = affinityMask;
                }

                //EventLog.WriteEntry("MicroTaskScheduler: Powershell script started", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {                
                EventLog.WriteEntry($"Failed to start PowerShell script: {ex.Message}", EventLogEntryType.Error);
            }
        }
    }
}

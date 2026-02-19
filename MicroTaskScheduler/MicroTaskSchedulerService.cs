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
            AssignEfficiencyCoresOnly();

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
            EventLog.WriteEntry("MicroTaskScheduler service started.", EventLogEntryType.Information);
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
                    DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0).AddMinutes(10);
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

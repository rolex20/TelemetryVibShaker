using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Media;

namespace MicroTaskScheduler
{


    public partial class MicroTaskSchedulerService : ServiceBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task antivirusDisableTask;
        private Task hourlyAlarmTask;
        private ProcessStartInfo startInfo;
        private IntPtr affinityMask = IntPtr.Zero;
        public MicroTaskSchedulerService()
        {
            InitializeComponent();
        }

        private void AssignEfficiencyCoresOnly()
        {
            Process currentProcess = Process.GetCurrentProcess();


            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Make sure we have 20 CPUs (HyperThreading Enabled and Efficient Cores enabled in 12700K)
            // Open the registry key for the processors
            int cpuCount = 0;
            regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (regKey != null)
            {
                // The number of subkeys corresponds to the number of CPUs
                cpuCount = regKey.SubKeyCount;
            }

            // Check if the processor name contains "Intel 12700K", HyperThreading Enabled and Efficient Cores enabled
            if (processorName.Contains("12700K") && cpuCount == 20)
            {                    
                // Define the CPU affinity mask for CPUs 17 to 20                                 
                // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                try
                {
                        // Set the CPU affinity
                        currentProcess.ProcessorAffinity = affinityMask;
                    }
                    catch { } // Ignore
            }

            regKey.Close();

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
            SoundPlayer myWaveFile = new SoundPlayer(Properties.Resources.Casio_Watch_Alarm);
            myWaveFile.Play();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Calculate the time remaining until the next hour
                    TimeSpan timeToNextHour = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(DateTime.Now.Minute) - TimeSpan.FromSeconds(DateTime.Now.Second) - TimeSpan.FromMilliseconds(DateTime.Now.Millisecond);
                    await Task.Delay(timeToNextHour, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested) myWaveFile.Play();
                }
            }
            catch
            {
                // obviously the previous calls might generate an exception when the cancelation token is triggered by an onStop() request
                // we can ignore them
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

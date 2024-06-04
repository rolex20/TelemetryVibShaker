using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace MicroTaskScheduler
{


    public partial class MicroTaskSchedulerService : ServiceBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task schedulerTask;
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

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
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
            schedulerTask = Task.Run(() => SchedulerLoop(cancellationTokenSource.Token));
            EventLog.WriteEntry("MicroTaskScheduler service started.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            try
            {
                cancellationTokenSource.Cancel();
                schedulerTask.Wait();
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

        private async Task SchedulerLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                    ExecutePowerShellScript();                    
                }
            } catch
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

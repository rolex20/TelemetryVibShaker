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

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(uint hThread, uint dwIdealProcessor);


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
                //AddToLog(GetTickCount64(), "Invalid MaxProcessorNumber", "$SetNewIdealProcessor({maxProcNumber})", true);
                return;
            }


            // My Intel 14700K has 8 performance cores and 12 efficiency cores.
            // CPU numbers 0-15 are performance
            // CPU numbers 16-27 are efficiency
            ProcessorAssigner assigner = new ProcessorAssigner(maxProcNumber);
            uint newIdealProcessor = assigner.GetNextProcessor();

            uint currentThreadHandle = GetCurrentThreadId();
            int previousProcessor = (int)SetThreadIdealProcessor(currentThreadHandle, newIdealProcessor);

            if (previousProcessor < 0 || (previousProcessor > maxProcNumber))
            {
                //AddToLog(GetTickCount64(), "Failed to set Ideal Processor", "$SetNewIdealProcessor({maxProcNumber})", true);
                return;
            }
        }

        private void AssignEfficiencyCoresOnlyOld()
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

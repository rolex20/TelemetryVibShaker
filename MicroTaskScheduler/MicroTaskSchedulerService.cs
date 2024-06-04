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
        public MicroTaskSchedulerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
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
            cancellationTokenSource.Cancel();
            schedulerTask.Wait();
            EventLog.WriteEntry("MicroTaskScheduler service stopped.", EventLogEntryType.Information);
        }

        private async Task SchedulerLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ExecutePowerShellScript();
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
        }

        private void ExecutePowerShellScript()
        {
            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                }

                EventLog.WriteEntry("MicroTaskScheduler: Powershell script started", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {                
                EventLog.WriteEntry($"Failed to start PowerShell script: {ex.Message}", EventLogEntryType.Error);
            }
        }
    }
}

using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;

namespace MicroTaskScheduler
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();

            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set the properties of the service process installer
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            // Set the properties of the service installer
            serviceInstaller.ServiceName = "My MicroTaskScheduler";
            serviceInstaller.Description = "My task scheduler service that runs a PowerShell script every 10 minutes.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add installers to the collection
            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }

        private void InitializeComponent()
        {
            // Method intentionally left empty.
        }
    }
}

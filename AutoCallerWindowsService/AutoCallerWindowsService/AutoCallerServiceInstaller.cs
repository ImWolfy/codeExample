using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace AutoCallerWindowsService
{
    [RunInstaller(true)]
    public partial class AutoCallerServiceInstaller : Installer
    {
        public AutoCallerServiceInstaller()
        {
            InitializeComponent();
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "MedhelpAutoCallerService";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}

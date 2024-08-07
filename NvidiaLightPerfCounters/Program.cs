﻿// Based-on and full credits to: https://github.com/Alexey-Kamenev/GpuPerfCounters
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// I am not using this service anymore: I have programmed the nvidia calls directly in my PerformanceMonitor
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace NvidiaLightPerfCounters
{
    [RunInstaller(true)]
    public class GpuPerfCountersInstaller : Installer
    {
        private readonly ServiceInstaller _svcInst;
        private readonly ServiceProcessInstaller _procInst;

        public GpuPerfCountersInstaller()
        {
            _procInst = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem, };

            _svcInst = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                ServiceName = "NvidiaLightPerfCounters",
                DisplayName = "Nvidia-GPU Performance Counters",
                Description = "Enables monitoring NVIDIA GPU performance counters in tools like perfmon and others."
            };

            Installers.Add(_svcInst);
            Installers.Add(_procInst);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            PerformanceCounterCategory.Delete(PerfCounterService.CategoryName);

            base.OnAfterUninstall(savedState);
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 1 && (args[0] == "-install" || args[0] == "-uninstall"))
            {
                switch (args[0])
                {
                    case "-install":
                        Install(false);
                        break;
                    case "-uninstall":
                        Install(true);
                        break;
                }
            }
            else
                ServiceBase.Run(new PerfCounterService());
        }

        private static void Install(bool uninstall)
        {
            AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, null);
            IDictionary state = new Hashtable();
            try
            {
                inst.UseNewContext = true;
                if (!uninstall)
                {
                    inst.Install(state);
                    inst.Commit(state);
                }
                else
                    inst.Uninstall(state);
            }
            catch (Exception)
            {
                inst.Rollback(state);
                throw;
            }
        }
    }


}

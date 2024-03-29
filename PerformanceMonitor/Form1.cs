using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;


namespace PerformanceMonitor
{
    public partial class frmMain : Form
    {
        private  PerformanceCounter cpuCounter0, cpuCounter1, cpuCounter2, cpuCounter3, cpuCounter4, cpuCounter5, cpuCounter6, cpuCounter7, cpuCounter8, cpuCounter9, cpuCounter10, cpuCounter11, cpuCounter12, cpuCounter13, cpuCounter14, cpuCounter15, cpuCounter16, cpuCounter17, cpuCounter18,cpuCounter19;
        private PerformanceCounter diskCounterC, diskCounterN, diskCounterR;
        private  PerformanceCounter gpuCounter;
        private Stopwatch stopwatch;
        private long ExCounter;  // Exceptions Counter
        private void btnGPU_Click(object sender, EventArgs e)
        {
            txtCounterName.Visible = false;
            txtCategory.Visible = false;
            btnGPU.Visible = false;

            gpuCounter = new PerformanceCounter("GPU", txtCategory.Text, txtCounterName.Text, true);
        }

        private void pbGPU0_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            chkEnabled.Checked = false;

            btnGPU.Visible = true;
            txtCategory.Visible = true;
            txtCounterName.Visible = true;
        }



        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCurrentProcessorNumber();

        public frmMain()
        {
            InitializeComponent();
        }

        private void ResetMaxCounters()
        {
            lblDiskC.Tag = 0.0f;
            lblDiskN.Tag = 0.0f;
            lblDiskR.Tag = 0.0f;

            lblMaxDiskC.Tag = 0.0f;
            lblMaxDiskN.Tag = 0.0f;
            lblMaxDiskR.Tag = 0.0f;

            lblMaxLoopTime.Tag = 0L;

            lblExceptions.Tag = 0L;
            ExCounter = 0L;
        }
        private void frmMain_Load(object sender, EventArgs e)
        {

            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
            {

                // Get the current process
                Process process = Process.GetCurrentProcess();

                // Define the CPU affinity mask for CPUs 17 to 20
                // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                IntPtr affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                // Set the CPU affinity
                process.ProcessorAffinity = affinityMask;
            }
            regKey.Close();

            ResetMaxCounters();

            gpuCounter = new PerformanceCounter("GPU", "% GPU Time", "_total");

            diskCounterC = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "0 C:", true);
            diskCounterN = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "1 N:", true);
            diskCounterR = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "2 R:", true);

            cpuCounter0 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,0", true);
            cpuCounter1 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,1", true);
            cpuCounter2 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,2", true);
            cpuCounter3 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,3", true);
            cpuCounter4 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,4", true);
            cpuCounter5 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,5", true);
            cpuCounter6 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,6", true);
            cpuCounter7 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,7", true);
            cpuCounter8 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,8", true);
            cpuCounter9 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,9", true);
            cpuCounter10 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,10", true);
            cpuCounter11 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,11", true);
            cpuCounter12 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,12", true);
            cpuCounter13 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,13", true);
            cpuCounter14 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,14", true);
            cpuCounter15 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,15", true);
            cpuCounter16 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,16", true);
            cpuCounter17 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,17", true);
            cpuCounter18 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,18", true);
            cpuCounter19 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,19", true);

            stopwatch = new Stopwatch();
        }

        private void UpdateCounter(PerformanceCounter cpuCounter, ProgressBar pb, Label lbl)
        {
            float f;
            try
            {
                f = cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                f = 0.0f;
                ExCounter++;              
            }
           
            int v = (int)f;

            if (pb.Value != v)
            {
                pb.Value = v;
                lbl.Text = $"{f:F1}%";
            }
        }

        private void UpdateDisk(PerformanceCounter diskCounter, Label diskLabel, Label diskMaxLabel)
        {
            float result;
            try
            {
                result = diskCounter.NextValue() / (1048576.0f);
            }
            catch (Exception ex)
            {
                result = 0.0f;
                ExCounter++;
            }

            if ((float)diskLabel.Tag != result)
            {
                diskLabel.Text = $"{result:F1}";

                if (result > (float)diskMaxLabel.Tag)
                {
                    diskMaxLabel.Tag = result;
                    diskMaxLabel.Text = diskLabel.Text;
                }
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            stopwatch.Restart();

                uint processorNumber = GetCurrentProcessorNumber();
                lblCurrentProcessor.Text = processorNumber.ToString();

                UpdateCounter(gpuCounter, pbGPU0, lblGPU0);
            
                UpdateCounter(cpuCounter0, pbCPU0, lblCPU0);
                UpdateCounter(cpuCounter1, pbCPU1, lblCPU1);
                UpdateCounter(cpuCounter2, pbCPU2, lblCPU2);
                UpdateCounter(cpuCounter3, pbCPU3, lblCPU3);
                UpdateCounter(cpuCounter4, pbCPU4, lblCPU4);
                UpdateCounter(cpuCounter5, pbCPU5, lblCPU5);
                UpdateCounter(cpuCounter6, pbCPU6, lblCPU6);
                UpdateCounter(cpuCounter7, pbCPU7, lblCPU7);
                UpdateCounter(cpuCounter8, pbCPU8, lblCPU8);
                UpdateCounter(cpuCounter9, pbCPU9, lblCPU9);
                UpdateCounter(cpuCounter10, pbCPU10, lblCPU10);
                UpdateCounter(cpuCounter11, pbCPU11, lblCPU11);
                UpdateCounter(cpuCounter12, pbCPU12, lblCPU12);
                UpdateCounter(cpuCounter13, pbCPU13, lblCPU13);
                UpdateCounter(cpuCounter14, pbCPU14, lblCPU14);
                UpdateCounter(cpuCounter15, pbCPU15, lblCPU15);
                UpdateCounter(cpuCounter16, pbCPU16, lblCPU16);
                UpdateCounter(cpuCounter17, pbCPU17, lblCPU17);
                UpdateCounter(cpuCounter18, pbCPU18, lblCPU18);
                UpdateCounter(cpuCounter19, pbCPU18, lblCPU19);

                UpdateDisk(diskCounterC, lblDiskC, lblMaxDiskC);
                UpdateDisk(diskCounterN, lblDiskN, lblMaxDiskN);
                UpdateDisk(diskCounterR, lblDiskR, lblMaxDiskR);

                if (ExCounter != (long)lblExceptions.Tag)
                {
                    lblExceptions.Tag = ExCounter;
                    lblExceptions.Text = ExCounter.ToString();
                }

            stopwatch.Stop();

            long elapsed_ms = stopwatch.ElapsedMilliseconds;
            lblLoopTime.Text = elapsed_ms.ToString();
            if (elapsed_ms > (long)lblMaxLoopTime.Tag)
            {
                lblMaxLoopTime.Tag = elapsed_ms;
                lblMaxLoopTime.Text = lblLoopTime.Text;
            }

        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = chkEnabled.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetMaxCounters();

        }
    }
}

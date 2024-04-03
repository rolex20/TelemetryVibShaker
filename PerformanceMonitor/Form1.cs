using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.ServiceProcess;
using System.Linq;
using System.CodeDom.Compiler;
using System.Linq.Expressions;


namespace PerformanceMonitor
{
    public partial class frmMain : Form
    {
        private PerformanceCounter cpuCounter0, cpuCounter1, cpuCounter2, cpuCounter3, cpuCounter4, cpuCounter5, cpuCounter6, cpuCounter7, cpuCounter8, cpuCounter9, cpuCounter10, cpuCounter11, cpuCounter12, cpuCounter13, cpuCounter14, cpuCounter15, cpuCounter16, cpuCounter17, cpuCounter18, cpuCounter19;

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopService("GpuPerfCounters");
        }

        private PerformanceCounter diskCounterC, diskCounterN, diskCounterR;

        private void picCPUDetails_Click(object sender, EventArgs e)
        {
            if (lblCurrentProcessor.Visible) // make sure we turn off ShowLastThread just in case
            {
                lblShowLastThread.Visible = false;
                lblLastThread.Visible = false;
            }

            lblCurrentProcessor.Visible = !lblCurrentProcessor.Visible;
            lblLP.Visible = lblCurrentProcessor.Visible;

         }

        private void lblLP_Click(object sender, EventArgs e)
        {
            lblShowLastThread.Visible = !lblShowLastThread.Visible;
            lblLastThread.Visible = lblShowLastThread.Visible;
        }

        private void tschkEnabled_Click(object sender, EventArgs e)
        {
            //timer1.Enabled = chkEnabled.Checked;
            timer1.Enabled = tschkEnabled.Checked;
        }

        private void pbGPU0_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            //chkEnabled.Checked = false;
            tschkEnabled.Checked = false;

            btnGPU.Visible = true;
            txtCategory.Visible = true;
            txtCounterName.Visible = true;
        }

        private void chkAutoMoveTop_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoMoveTop.Checked)
            {
                chkAutoMoveTop.Tag = lblTop.Tag;
                chkAutoMoveTop.Text = "Auto Move Top: " + lblTop.Text;
            }
            else
                chkAutoMoveTop.Text = "Auto Move Top";
        }

        private PerformanceCounter gpuUtilizationCounter, gpuFanCounter;
        private Stopwatch stopwatch;
        private long ExCounter;  // Exceptions Counter

        // Import the GetCurrentThread API
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        // Import the SetThreadIdealProcessor API
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();


        private void btnGPU_Click(object sender, EventArgs e)
        {
            txtCounterName.Visible = false;
            txtCategory.Visible = false;
            btnGPU.Visible = false;

            gpuUtilizationCounter = new PerformanceCounter("GPU", txtCategory.Text, txtCounterName.Text, true);
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
            timer1.Enabled = false;
            timer1.Tag = false; // flag for one-time control in timer1_Tick()

            lblTop.Tag = 0;  // used to store frmMain.Top
            chkAutoMoveTop.Tag = 10; // give some 10 pixels of top margin
            lblCurrentProcessor.Tag = 255; // unrealistic processor assigment to force update in timer1

            lblLT.Tag = false; // used to ignore the first LoopTime Max calculation in timer1
            lblLoopTime.Tag = 0L; // used to keep track of the last one to avoid update in timer1

            lblLastThread.Tag = 0; // used to keep track of the last thread used to avoid update in timer1

            StartService("GpuPerfCounters");
            InitializeCounterTags();
            ResetMaxCounters();



            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
            {
                // Define the CPU affinity mask for CPUs 17 to 20
                // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                IntPtr affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                // Set the CPU affinity
                currentProcess.ProcessorAffinity = affinityMask;

                gpuUtilizationCounter = new PerformanceCounter("GPU", "% GPU Time", "nvidia geforce rtx 4090(01:00)");
                gpuFanCounter = new PerformanceCounter("GPU", "% GPU Fan Speed", "nvidia geforce rtx 4090(01:00)");

                lblCPU.Tag = true;  // special tag to indicate that this is a 12700K
            }
            else
            {
                gpuUtilizationCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", true);  //Generic for debugging
                lblCPU.Tag = false; // special tag to indicate that this is a 12700K
            }

            regKey.Close();

            // Change the priority class to BelowNormal
            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

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
            //timer1.Enabled = chkEnabled.Checked;
            timer1.Enabled = tschkEnabled.Checked;
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

            if ((int)lbl.Tag != v)
            {
                lbl.Tag = v;

                System.Drawing.Color color = f <= 100.0f ? (System.Drawing.Color)pb.Tag : System.Drawing.Color.Red;
                if (pb.ForeColor != color) pb.ForeColor = color;
                pb.Value = v <= 100 ? v : 100;


                color = f <= 100.0f ? System.Drawing.Color.Black : System.Drawing.Color.Red;
                if (lbl.ForeColor != color) lbl.ForeColor = color;
                lbl.Text = $"{f:F1}%";
            }
        }

        private void UpdateDisk(PerformanceCounter diskCounter, Label diskLabel, Label diskMaxLabel)
        {
            float result;
            try
            {
                result = diskCounter.NextValue() / (1048576.0f); // show value in MB/s
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

            // update frmMain.Top position in the label only when required
            int t = this.Top;
            if ((int)lblTop.Tag != t)
            {
                lblTop.Tag = t;
                lblTop.Text = t.ToString();
            }

            // move frmMain.Top position to the stored position if required by the user
            if (chkAutoMoveTop.Checked)
            {
                int a = (int)chkAutoMoveTop.Tag;
                if (this.Top != a) this.Top =a;                
            }

            // make the form to be always on top if required by the user
            if (chkAlwaysOnTop.Checked != this.TopMost) 
                this.TopMost = chkAlwaysOnTop.Checked;


            if (!(bool)timer1.Tag) // only do this once
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                if ((bool)lblCPU.Tag) // special flag equals True when this processor is a 12700K
                {
                    // Get the pseudo handle for the current thread
                    IntPtr currentThreadHandle = GetCurrentThread();

                    // Set the ideal processor for the current thread to 19.   GpuPerfCounters is assigned 18
                    uint previousIdealProcessor = SetThreadIdealProcessor(currentThreadHandle, 19);
                }
                timer1.Tag = true;  // flag for one-time control in timer1_Tick()
            }

            if (lblShowLastThread.Visible)
            {
                int th = (int)GetCurrentThreadId(); 
                if ((int)lblLastThread.Tag != th)
                {                    
                    lblLastThread.Tag = th;
                    lblLastThread.Text = th.ToString();
                }
                
            }

            if (lblCurrentProcessor.Visible)
            {
                int processorNumber = (int)GetCurrentProcessorNumber();
                if ((int)lblCurrentProcessor.Tag != processorNumber)
                {
                    lblCurrentProcessor.Tag = processorNumber;
                    lblCurrentProcessor.Text = processorNumber.ToString();
                }
            }

            UpdateCounter(gpuUtilizationCounter, pbGPU0, lblGPU0);
            UpdateCounter(gpuFanCounter, pbGPUFanSpeed, lblGPUFanSpeed);

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
            if ((long)lblLoopTime.Tag != elapsed_ms)
            {
                lblLoopTime.Tag = elapsed_ms;
                lblLoopTime.Text = elapsed_ms.ToString();
            }

            if (elapsed_ms > (long)lblMaxLoopTime.Tag)
            {
                if ((bool)lblLT.Tag) // ignore the first Max
                    lblMaxLoopTime.Tag = elapsed_ms; 
                else
                    lblLT.Tag = true;

                lblMaxLoopTime.Text = lblLoopTime.Text;  // update regardless of the first-time-ignore for information purposes
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetMaxCounters();

        }

        private void InitializeCounterTags()
        {
            var labels = this.Controls.OfType<Label>()
                            .Where(c => (c.Name.StartsWith("lblCPU")) || (c.Name.StartsWith("lblGPU")))
                            .ToList();
            foreach (var label in labels)
            {
                label.Tag = 0; // Needs initial assignment in UpdateCounters()
            }



            var progressbars = this.Controls.OfType<ProgressBar>()
                            .Where(c => (c.Name.StartsWith("pbCPU")) || (c.Name.StartsWith("pbGPU")))
                            .ToList();
            foreach (var progressbar in progressbars)
            {
                progressbar.Tag = progressbar.ForeColor; // Save default bar color for use in UpdateCounters()
            }
        }

        private void StartService(string serviceName)
        {
            try
            {
                // Create an instance of ServiceController
                ServiceController serviceController = new ServiceController(serviceName);

                // Check if the service is already running or in the process of starting
                if (serviceController.Status != ServiceControllerStatus.Running &&
                    serviceController.Status != ServiceControllerStatus.StartPending)
                    // Start the service
                    serviceController.Start();
            }
            catch 
            {
                // continue
            }
        }

        private void StopService(string serviceName)
        {
            try
            {
                // Create an instance of ServiceController
                ServiceController serviceController = new ServiceController(serviceName);

                // Check if the service is already running or in the process of starting
                if (serviceController.Status == ServiceControllerStatus.Running)
                    // Stop the service
                    serviceController.Stop();
            }
            catch
            {
                // continue
            }
        }

    }
}

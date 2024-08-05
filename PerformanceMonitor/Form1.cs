using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Drawing;
using System.Threading;



namespace PerformanceMonitor
{
    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }

    public partial class frmMain : Form
    {
        private PerformanceCounter cpuCounter0, cpuCounter1, cpuCounter2, cpuCounter3, cpuCounter4, cpuCounter5, cpuCounter6, cpuCounter7, cpuCounter8, cpuCounter9, cpuCounter10, cpuCounter11, cpuCounter12, cpuCounter13, cpuCounter14, cpuCounter15, cpuCounter16, cpuCounter17, cpuCounter18, cpuCounter19, cpuCounter20, cpuCounter21, cpuCounter22, cpuCounter23, cpuCounter24, cpuCounter25, cpuCounter26, cpuCounter27;
        //private PerformanceCounter gpuUtilizationCounter, gpuFanCounter;
        private Stopwatch stopwatch;
        private long ExCounter;  // Exceptions Counter
        private PerformanceCounter diskCounterC, diskCounterN, diskCounterR;
        private NvidiaGpu myRTX4090;

        private HttpListener listener; // Web Server for remote control location and focus commands
        private int webServerThreadId, dispatcherUIThread;
        private Process currentProcess;
        private CpuType cpuType;

        private int maxCpuUtil;
        string maxCpuName;

        private MediaPlayer mpCpu, mpGpu;

        private float TotalTicks, TotalCpuTicksAboveThreshold, TotalGpuTicksAboveThreshold;

        private void ResetTicksCounters()
        {
            TotalTicks = 0.0f;
            TotalCpuTicksAboveThreshold = 0.0f;
            TotalGpuTicksAboveThreshold = 0.0f;
            lblCpuAbovePct.Tag = -1.0f;
            lblGpuAbovePct.Tag = -1.0f;
        }


        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.TimerInterval = (int)nudPollingInterval.Value;
            Properties.Settings.Default.PriorityClassSelectedIndex = cmbPriorityClass.SelectedIndex;

            Properties.Settings.Default.chkCpuAlarm = chkCpuAlarm.Checked;
            Properties.Settings.Default.chkGpuAlarm = chkGpuAlarm.Checked;
            Properties.Settings.Default.trkCpuThreshold = trkCpuThreshold.Value;
            Properties.Settings.Default.trkGpuThreshold = trkGpuThreshold.Value;
            Properties.Settings.Default.txtCpuAlarm = txtCpuAlarm.Text;
            Properties.Settings.Default.txtGpuAlarm = txtGpuAlarm.Text;
            Properties.Settings.Default.trkCpuVolume = trkCpuVolume.Value;
            Properties.Settings.Default.trkGpuVolume = trkGpuVolume.Value;
            Properties.Settings.Default.tcTabControl = tcTabControl.SelectedIndex;

            if (this.WindowState != FormWindowState.Minimized)
            {
                Properties.Settings.Default.XCoordinate = this.Location.X;
                Properties.Settings.Default.YCoordinate = this.Location.Y;
            }

            Properties.Settings.Default.Save();
        }



        private void tsbtnResetMaxCounters_Click(object sender, EventArgs e)
        {
            ResetMaxCounters();
        }

        private void tschkShowLastThread_Click(object sender, EventArgs e)
        {
            if (!tschkShowLastThread.Checked)
                tslblLastThread.Text = "---";
            else
                tslblLastThread.Text = String.Empty;
        }

        private void tsbtnRecenter_Click(object sender, EventArgs e)
        {
            tschkAutoMoveTop.Checked = false;
            this.CenterToScreen();
        }


        private void tschkShowLastProcessor_Click(object sender, EventArgs e)
        {
            if (!tschkShowLastProcessor.Checked)
                tslblCurrentProcessor.Text = "----";
            else 
                tslblCurrentProcessor.Text = String.Empty;
        }

        private void cmbPriorityClass_SelectedIndexChanged(object sender, EventArgs e)
        {
                switch (cmbPriorityClass.SelectedIndex)
                {
                    case 0: //NORMAL
                        currentProcess.PriorityClass = ProcessPriorityClass.Normal;
                        break;
                    case 1: // BELOW NORMAL
                        currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                        break;
                    case 2: //IDLE
                        currentProcess.PriorityClass = ProcessPriorityClass.Idle;
                        break;
                }

            cmbPriorityClass.Tag = true;
        }

        private void nudPollingInterval_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)nudPollingInterval.Value;
        }

        private void tstxtAutoMoveY_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Prevent non-numeric characters
            }
        }


        private void tschkEnabled_Click(object sender, EventArgs e)
        {
            //timer1.Enabled = chkEnabled.Checked;
            timer1.Enabled = tschkEnabled.Checked;

            if (timer1.Enabled) { ResetTicksCounters(); }
        }


        // Import the GetCurrentThread API
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        private void trkCpuVolume_Scroll(object sender, EventArgs e)
        {
            lblCpuVolume.Text = trkCpuVolume.Value.ToString();
        }

        private void trkGpuVolume_Scroll(object sender, EventArgs e)
        {
            lblGpuVolume.Text = trkGpuVolume.Value.ToString();
        }

        private void trkCpuThreshold_Scroll(object sender, EventArgs e)
        {
            lblCpuThreshold.Text = trkCpuThreshold.Value.ToString();
        }

        private void trkGpuThreshold_Scroll(object sender, EventArgs e)
        {
            lblGpuThreshold.Text = trkGpuThreshold.Value.ToString();
        }

        private void cmbAudioDevice1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeAudioDevice(mpCpu, cmbAudioDevice1.SelectedIndex);
            ChangeAudioDevice(mpGpu, cmbAudioDevice1.SelectedIndex);
        }
        
        private void ChangeAudioDevice(MediaPlayer mp, int audioDeviceIndex)
        {
            if (mp!=null) mp.ChangeAudioDevice(audioDeviceIndex);
        }

        private void chkCpuAlarm_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCpuAlarm.Checked)
            {
                mpCpu = new MediaPlayer(cmbAudioDevice1.SelectedIndex);
                mpCpu.Open(txtCpuAlarm.Text);
                mpCpu.Volume = 0.0f;
                mpCpu.PlayLooping();
            } else
            {
                mpCpu.Dispose();
                mpCpu = null;
            }
        }

        private void chkGpuAlarm_CheckedChanged(object sender, EventArgs e)
        {
            if (chkGpuAlarm.Checked)
            {
                mpGpu = new MediaPlayer(cmbAudioDevice1.SelectedIndex);
                mpGpu.Open(txtGpuAlarm.Text);
                mpGpu.Volume = 0.0f;
                mpGpu.PlayLooping();
            }
            else
            {
                mpGpu.Dispose();
                mpGpu = null;
            }
        }



        // Import the SetThreadIdealProcessor API
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);


        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();




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

            tslblMaxLoopTime.Tag = 0L;

            tslblExceptions.Tag = 0L;
            ExCounter = 0L;

            ResetTicksCounters();
        }

        private string GetMyIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return "localhost";
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private void lblCPU3_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void pbCPU17_Click(object sender, EventArgs e)
        {

        }

        private void lblCPU17_Click(object sender, EventArgs e)
        {

        }

        private void lblCPU5_Click(object sender, EventArgs e)
        {

        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void pbCPU18_Click(object sender, EventArgs e)
        {

        }

        private void lblCPU18_Click(object sender, EventArgs e)
        {

        }

        private void lblCPU7_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void pbCPU19_Click(object sender, EventArgs e)
        {

        }

        private void lblCPU19_Click(object sender, EventArgs e)
        {

        }

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        private Mutex SingleInstanceMutex;
        private void SingleInstanceChecker()
        {
            SingleInstanceMutex = new Mutex(true, this.Text + "_mutex", out bool createdNew);
            if (!createdNew)
            {
                BringExistingInstanceToFront();
                ShowNotification("Another instance of this application is already running.");
                Application.Exit();
            }
        }

        private void BringExistingInstanceToFront()
        {
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (IsIconic(handle))
                    {
                        ShowWindowAsync(handle, SW_RESTORE);
                    }
                    SetForegroundWindow(handle);
                    break;
                }
            }
        }

        private void ShowNotification(string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = "Application Notification",
                BalloonTipText = message
            };

            notifyIcon.ShowBalloonTip(10000);
        }


        private void AssignEfficiencyCoresOnly()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
            string processorName = regKey.GetValue("ProcessorNameString").ToString();
            currentProcess = Process.GetCurrentProcess();

            // We need to know the number of processors available to determine if Hyperthreading and Efficient cores are enabled
            int cpuCount = 0;
            regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (regKey != null)
            {
                // The number of subkeys corresponds to the number of CPUs
                cpuCount = regKey.SubKeyCount;
            }

            if (processorName.Contains("12700K")) cpuType = CpuType.Intel_12700K;
            else if (processorName.Contains("14700K")) cpuType = CpuType.Intel_14700K;
            else cpuType = CpuType.Other;



            // Define the CPU affinity mask for CPUs 17 to 20
            // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
            IntPtr affinityMask = IntPtr.Zero;
            switch (cpuType)
            {
                case CpuType.Intel_12700K:                    
                    tslblCpuType.Text = "i7-12700K";
                    if (cpuCount == 20) // Make sure HyperThreading and Efficient cores are enabled
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);
                        lblEfficientCoresNote.Text = "12700K" + lblEfficientCoresNote.Text;
                        lblEfficientCoresNote.Visible = true;
                    }

                    // Highlight my best cores according to my BIOS
                    label8.BorderStyle = BorderStyle.FixedSingle;
                    label12.BorderStyle = BorderStyle.FixedSingle;
                    label14.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case CpuType.Intel_14700K:                    
                    tslblCpuType.Text = "i7-14700K";
                    if (cpuCount == 28) // Make sure HyperThreading and Efficient cores are enabled
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | 1 << 23 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27);
                        lblEfficientCoresNote.Text = "14700K" + lblEfficientCoresNote.Text;
                        lblEfficientCoresNote.Visible = true;
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
                    currentProcess.ProcessorAffinity = affinityMask;
                }
                catch { } // Ignore
            }

            regKey.Close();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();

            ResetTicksCounters();

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);


            chkCpuAlarm.Checked = Properties.Settings.Default.chkCpuAlarm;
            chkGpuAlarm.Checked = Properties.Settings.Default.chkGpuAlarm;
            trkCpuThreshold.Value = Properties.Settings.Default.trkCpuThreshold;
            trkGpuThreshold.Value = Properties.Settings.Default.trkGpuThreshold;
            txtCpuAlarm.Text = Properties.Settings.Default.txtCpuAlarm;
            txtGpuAlarm.Text = Properties.Settings.Default.txtGpuAlarm;
            trkCpuVolume.Value = Properties.Settings.Default.trkCpuVolume;
            trkGpuVolume.Value = Properties.Settings.Default.trkGpuVolume;

            tcTabControl.SelectedIndex = Properties.Settings.Default.tcTabControl;

            FillAudioDevices();

            maxCpuUtil = 0;
            maxCpuName = String.Empty;

            webServerThreadId = -1;
            dispatcherUIThread = -1;

            timer1.Enabled = false;
            timer1.Tag = false; // flag for one-time control in timer1_Tick()

            tscmbCategory.Tag = false;  // flag to avoid changing the GpuPerfCounter the first time
            tscmbCategory.SelectedIndex = 0;  // otherwise, the combo will appear empty

            tslblTop.Tag = 0;  // used to store frmMain.Top
            tslblCurrentProcessor.Tag = 255; // unrealistic processor assigment to force update in timer1

            tslblLT.Tag = false; // used to ignore the first LoopTime Max calculation in timer1
            tslblLoopTime.Tag = 0L; // used to keep track of the last one to avoid update in timer1

            tslblLastThread.Tag = 0; // used to keep track of the last thread used to avoid update in timer1

            myRTX4090 = new NvidiaGpu(0);


            InitializeCounterTags();
            ResetMaxCounters();
            AssignEfficiencyCoresOnly();



            // Change the priority class to the previous setting selected (NORMAL, BELOW_NORMAL or IDLE)
            cmbPriorityClass.SelectedIndex = Properties.Settings.Default.PriorityClassSelectedIndex;


            // Obtain current IP Address
            txtIPAddress.Text = GetMyIPAddress();

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
            cpuCounter20 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,20", true);
            cpuCounter21 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,21", true);
            cpuCounter22 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,22", true);
            cpuCounter23 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,23", true);
            cpuCounter24 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,24", true);
            cpuCounter25 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,25", true);
            cpuCounter26 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,26", true);
            cpuCounter27 = new PerformanceCounter("Processor Information", "% Processor Utility", "0,27", true);

            StartWebServer();

            stopwatch = new Stopwatch();

            nudPollingInterval.Value = Properties.Settings.Default.TimerInterval;
            timer1.Interval = (int)nudPollingInterval.Value;
            timer1.Enabled = tschkEnabled.Checked;
        }

        // Function Inlining: Only in this particular case, I prefer to repeat code in this case instead of passing all parameters to the similar function
        private void UpdateCounter(int counter, ProgressBar pb, Label lbl, string dimensional = "%")
        {

            if (this.WindowState != FormWindowState.Minimized && (int)lbl.Tag != counter)
            {
                lbl.Tag = counter;

                System.Drawing.Color color = counter <= 100.0f ? (System.Drawing.Color)pb.Tag : System.Drawing.Color.Red;
                if (pb.ForeColor != color) pb.ForeColor = color;
                pb.Value = counter <= 100 ? counter : 100;

                // Let's notify with red when counter >= 80 to denote possible bottleneck
                color = counter <= 80.0f ? System.Drawing.Color.Black : System.Drawing.Color.Red;
                if (lbl.ForeColor != color) lbl.ForeColor = color;
                lbl.Text = $"{counter:F1}{dimensional}";
            }

        }

        // Function Inlining: Only in this particular case, I prefer to repeat code in this case instead of passing all parameters to the similar function
        private void UpdateCounter(PerformanceCounter counter, ProgressBar pb, Label lbl, string dimensional = "%")
        {
            float f;

            try
            {
                f = counter.NextValue();
                if (f>maxCpuUtil)
                {
                    maxCpuUtil = (int)f;
                    maxCpuName = counter.CounterName;
                }
            }
            catch (Exception ex)
            {
                f = 0.0f;
                ExCounter++;
                LogError(ex.Message, $"UpdateCounter({pb.Name})");
            }

            int v = (int)f;

            
            // Only update the label if it is visible in the selected tab and the value has changed
            if (lbl.Parent == tcTabControl.SelectedTab && this.WindowState != FormWindowState.Minimized && (int)lbl.Tag != v)
            {
                lbl.Tag = v;

                // Let's notify with red when counter >= 80 to denote possible bottleneck
                System.Drawing.Color color = f <= 80.0f ? (System.Drawing.Color)pb.Tag : System.Drawing.Color.Red;
                if (pb.ForeColor != color) pb.ForeColor = color;
                pb.Value = v <= 100 ? v : 100;


                color = f <= 100.0f ? System.Drawing.Color.Black : System.Drawing.Color.Red;
                if (lbl.ForeColor != color) lbl.ForeColor = color;
                lbl.Text = $"{f:F1}{dimensional}";
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
                LogError(ex.Message, $"UpdateDisk({diskLabel.Name})");
            }

            if (diskLabel.Parent == tcTabControl.SelectedTab && this.WindowState != FormWindowState.Minimized && (float)diskLabel.Tag != result)
            {
                diskLabel.Text = $"{result:F1}";

                if (result > (float)diskMaxLabel.Tag)
                {
                    diskMaxLabel.Tag = result;
                    diskMaxLabel.Text = diskLabel.Text;
                }
            }

        }

        private void UpdateGPUInfo()
        {
            // Need to read temp too to get the fan speed??, so all updated here
            int temp = myRTX4090.TemperatureC;
            int fanspeed = myRTX4090.FanSpeed;
            int util = myRTX4090.Utilization; // this also reads memory utilization

            // Track max Gpu util
            if (mpGpu != null )
            {
                if (util >= trkGpuThreshold.Value)
                {
                    mpGpu.Volume = trkGpuVolume.Value / 100.0f;
                    TotalGpuTicksAboveThreshold += timer1.Interval;                    
                }
                else
                    mpGpu.Volume = 0.0f;
            }
            UpdateCaption(lblGpuAbovePct, TotalGpuTicksAboveThreshold / TotalTicks, "%");

            ExCounter += myRTX4090.ReadResetExceptionsCounter;

            // update the GPU-Utilization or the GPU-Temperature
            switch (tscmbCategory.SelectedIndex)
            {
                case 0: // %GPU Time
                    UpdateCounter(util, pbGPU0, lblGPU0, "%");
                    UpdateCaption(lblGpuAlarm, util, "%");
                    break;
                case 1: // GPU Temperature (in degrees C)
                    UpdateCounter(temp, pbGPU0, lblGPU0, "°C");
                    break;
                case 2: // Memory Utilization
                    UpdateCounter(myRTX4090.MemoryUtilization, pbGPU0, lblGPU0, "?");
                    break;
            }

            // update Fan-Speed
            UpdateCounter(fanspeed, pbGPUFanSpeed, lblGPUFanSpeed);
        }

        private void UpdateMonitorLabels()
        {
            float incrementalTicks = timer1.Interval;
            TotalTicks += incrementalTicks;

            if (tschkShowLastThread.Checked)
            {
                UpdateCaption(tslblLastThread, (int)GetCurrentThreadId());
            }

            if (tschkShowLastProcessor.Checked)
            {
                UpdateCaption(tslblCurrentProcessor, (int)GetCurrentProcessorNumber());
            }


            UpdateGPUInfo();

            maxCpuUtil = 0;
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
            UpdateCounter(cpuCounter19, pbCPU19, lblCPU19);
            UpdateCounter(cpuCounter20, pbCPU20, lblCPU20);
            UpdateCounter(cpuCounter21, pbCPU21, lblCPU21);
            UpdateCounter(cpuCounter22, pbCPU22, lblCPU22);
            UpdateCounter(cpuCounter23, pbCPU23, lblCPU23);
            UpdateCounter(cpuCounter24, pbCPU24, lblCPU24);
            UpdateCounter(cpuCounter25, pbCPU25, lblCPU25);
            UpdateCounter(cpuCounter26, pbCPU26, lblCPU26);
            UpdateCounter(cpuCounter27, pbCPU27, lblCPU27);

            if (mpCpu != null) {
                if (maxCpuUtil >= trkCpuThreshold.Value)
                {
                    mpCpu.Volume = trkCpuVolume.Value / 100.0f;
                    TotalCpuTicksAboveThreshold += incrementalTicks;                    
                } 
                else
                    mpCpu.Volume = 0.0f;
            }
            UpdateCaption(lblCpuAbovePct, TotalCpuTicksAboveThreshold / TotalTicks, "%");

            UpdateCaption(lblCpuAlarm, maxCpuUtil, "%");

            UpdateDisk(diskCounterC, lblDiskC, lblMaxDiskC);
            UpdateDisk(diskCounterN, lblDiskN, lblMaxDiskN);
            UpdateDisk(diskCounterR, lblDiskR, lblMaxDiskR);

            UpdateCaption(tslblExceptions, ExCounter);
         }

            private void timer1_Tick(object sender, EventArgs e)
            {
            stopwatch.Restart();
            timer1.Enabled = false;

            // update frmMain.Top position in the label only when required
            int t = this.Top;
            if ((int)tslblTop.Tag != t)
            {
                tslblTop.Tag = t;
                tslblTop.Text = t.ToString();
            }


            if (tschkAutoReadY.Checked)
                tstxtAutoMoveY.Text = tslblTop.Text;
            
            if (tschkAutoMoveTop.Checked && (int.TryParse(tstxtAutoMoveY.Text, out t)) && (this.Top!=t))          
                this.Top = t;

            // make the form to be always on top if required by the user
            if (tschkAlwaysOnTop.Checked != this.TopMost) 
                this.TopMost = tschkAlwaysOnTop.Checked;


            UpdateMonitorLabels();
            timer1.Enabled = true;
            stopwatch.Stop();

            long elapsed_ms = stopwatch.ElapsedMilliseconds;
            UpdateCaption(tslblLoopTime, elapsed_ms);

            
            if (elapsed_ms > (long)tslblMaxLoopTime.Tag)
            {
                if (!(bool)tslblLT.Tag) // ignore the first Max
                    UpdateCaption(tslblMaxLoopTime, elapsed_ms); 
                tslblLT.Tag = true;                
            }

        }


        private void InitializeCounterTags()
        {

            var labels = tbMonitor.Controls.OfType<Label>()
                            .Where(c => (c.Name.StartsWith("lblCPU")) || (c.Name.StartsWith("lblGPU")))
                            .ToList();
            foreach (var label in labels)
            {
                label.Tag = 0; // Needs initial assignment in UpdateCounters()
            }



            var progressbars = tbMonitor.Controls.OfType<ProgressBar>()
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
            catch (Exception ex)
            {
                ExCounter++;
                LogError(ex.Message, $"StartService({serviceName})");
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
            catch (Exception ex)
            {
                ExCounter++;
                LogError(ex.Message, $"StopService({serviceName})");
                
            }
        }

        private void StartWebServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://{txtIPAddress.Text}:8080/");
            //listener.Prefixes.Add("http://perfmon:8080/");
            //listener.Prefixes.Add("http://192.168.1.5:8080/"); // remove this line when debugging
            //listener.Prefixes.Add("http://localhost:8080/");
            try
            {
                listener.Start();
            }
            catch (Exception ex) {
                ExCounter++;
                LogError(ex.Message, "StartWebServer{ listener.Start(); }");
            }
            Task.Run(() => ProcessRequest());
        }

        private async Task ProcessRequest()
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                
                if (request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string formData = await reader.ReadToEndAsync();
                        webServerThreadId = (int)GetCurrentThreadId();

                        var parameters = formData.Split('&')
                                                .Select(param => param.Split('='))
                                                .ToDictionary(param => param[0], param => WebUtility.UrlDecode(param[1]));

                        string sx, sy;
                        int x = -1;
                        int y = -1;
                        if (parameters.TryGetValue("x", out sx) && parameters.TryGetValue("y", out sy))
                        {
                            x = int.Parse(sx);
                            y = int.Parse(sy);
                        }

                        string topMost, focus, reset, exit;

                        parameters.TryGetValue("topmost", out topMost);
                        parameters.TryGetValue("focus", out focus);
                        parameters.TryGetValue("reset", out reset);
                        parameters.TryGetValue("exit", out exit);


                        MakeFormChanges(x, y, topMost, focus, reset);

                        SendResponse(response, $"{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} Successfully requested coordinates change to X: {x}, Y: {y}");

                        if (exit != null) Application.Exit();
                    }
                }
                else
                {
                    string timestamp = DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]");
                    SendResponse(response, "Welcome! Please submit new X and Y coordinates.");
                }
            }
        }

        private void MakeFormChanges(int x, int y, string topMost, string focus, string reset)
        {
            this.Invoke(new Action(() => {
                dispatcherUIThread = (int)GetCurrentThreadId();


                //txtErrors.AppendText($"Moving form to x={x}, y={y}" + Environment.NewLine);

                //if (x >= 0 && y >= 0)
                this.Location = new System.Drawing.Point(x, y);

                // If the user submitted the form with topMost empty, this means the user might want to set this property to false
                this.TopMost = topMost != null;

                if (focus != null) this.Activate();

                if (reset != null) ResetMaxCounters();

            }));
        }

        private void SendResponse(HttpListenerResponse response, string message)
        {

            string marked = (this.TopMost ? "checked" : String.Empty);
            string responseString = $@"
                <html>
                <head><title>Performance Monitor Light - Web Server</title>
                <link rel=""stylesheet"" href=""https://learn.microsoft.com/_themes/docs.theme/master/en-us/_themes/styles/24b6bbbc.site-ltr.css "">
                </head>
                <body leftmargin='10' topmargin='10'>
                    <h1>{message}</h1>
                    <form method='post' border='1'>
                        <br><br><label for='x'>X:</label>
                        <input type='number' name='x' value='{this.Location.X}' required placeholder='type new X coordinate'/><br><br>
                        <label for='y'>Y:</label>
                        <input type='number' name='y' value='{this.Location.Y}' required placeholder='type new Y coordinate' autofocus/><br><br>

                        <input type='checkbox' id='topmost' name='topmost' value='always_on_top' {marked}>
                        <label for='topmost'> Always on top</label><br>

                        <input type='checkbox' id='focus' name='focus' value='focus'>
                        <label for='focus'>Focus</label><br>

                        
                        <input type='checkbox' id='reset' name='reset' value='Reset'>
                        <label for='reset'>Reset All Max Values</label><br>
                        
                        <input type='checkbox' id='exit' name='exit' value='Exit'>
                        <label for='exit'>Exit program</label><br>

                        <input type='submit' value='Submit' />
                    </form>
                    <hr>{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} Web Server ThreadID: {webServerThreadId.ToString()} Dispatcher ThreadID: {dispatcherUIThread}
                </body>
                </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
            response.Close();
        }

        private void LogError(string message, string function)
        {
            if (ExCounter <= 100) // Only log the first 100 errors
            {
                // Get the current timestamp
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                // Format the error message with the timestamp
                string errorMessage = $"[{timestamp}] [{function}] {message}{Environment.NewLine}";

                // Check if the action is being called from a thread other than the UI thread
                if (txtErrors.InvokeRequired)
                {
                    txtErrors.Invoke(new Action(() => txtErrors.AppendText(errorMessage)));
                }
                else
                {
                    txtErrors.AppendText(errorMessage);
                }
            }
        }

        private void UpdateCaption<TControl, TValue>(TControl L, TValue value, string dimensional = "")
            where TControl : Control
        {

            if (this.WindowState != FormWindowState.Minimized && tcTabControl.SelectedTab==L.Parent && !Equals(L.Tag, value))
            {
                L.Tag = value;
                L.Text = (value is float f ? $"{f:F1}" : value.ToString()) + dimensional;
            } 
        }

        private void UpdateCaption<TValue>(ToolStripLabel L, TValue value, string dimensional = "")
        {
            if (this.WindowState != FormWindowState.Minimized && !Equals(L.Tag, value))
            {
                L.Tag = value;
                L.Text = (value is float f ? $"{f:F1}" : value.ToString()) + dimensional;
            }
        }

        private void FillAudioDevices()
        {


            //This version has a problem is likely due to a limitation within the NAudio library. Specifically, the WaveOut.GetCapabilities method returns a ProductName that is truncated to a maximum of 31 characters
            var enumerator = new MMDeviceEnumerator();
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var capabilities = WaveOut.GetCapabilities(n);
                cmbAudioDevice1.Items.Add(capabilities.ProductName);

                var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[n];
                Debug.Print(device.FriendlyName);
            }
            /*
                        // Create enumerator
                        var enumerator = new MMDeviceEnumerator();
                        // Skip the -1 Microsoft Audio Mapper
                        for (int n = 0; n < WaveOut.DeviceCount; n++)
                        {
                            var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[n];
                            cmbAudioDevice1.Items.Add($"{device.ID} - {device.InstanceId} - {device.FriendlyName}");
                        }
            */
        }


    }
}

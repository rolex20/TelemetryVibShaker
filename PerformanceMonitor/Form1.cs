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
using System.IO.Pipes;
using System.Windows.Forms.VisualStyles;
using IdealProcessorEnhanced;
using System.Reflection;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;



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

        private HttpListener listener; // Web IPC_PipeServer for remote control location and focus commands
        private int webServerThreadId, dispatcherUIThread;

        private Thread pipeServerThread;  // IPC_PipeServer Thread for pipes interprocess communications
        private CancellationTokenSource pipeCancellationTokenSource;

        private Process currentProcess;
        private CpuType CPUType;
        private int CPUCount = -1;
        private bool needToCallSetNewIdealProcessor = true;
        private ProcessorAssigner processorAssigner = null;  // Must be alive the while the program is running and is assigned only once if it is the right type of processor

        private int maxCpuUtil; // Maximum recorded CPU utilization
        string maxCpuName;

        private MediaPlayer mpCpu, mpGpu;

        private float TotalTicks, TotalCpuTicksAboveThreshold, TotalGpuTicksAboveThreshold;

        private bool topMost = false; // to fix .TopMost bug

        private ProcessorAssignmentStats cpuStats;


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
            cpuStats?.WriteReportSnapshot(true, "Final Snapshot on FormClosing()");

            timer1.Enabled = false;
            pipeCancellationTokenSource.Cancel();
            
            
            // Save relevant user settings

            Properties.Settings.Default.TimerInterval = (int)nudPollingInterval.Value;
            Properties.Settings.Default.PriorityClassSelectedIndex = cmbPriorityClass.SelectedIndex;

            Properties.Settings.Default.chkCpuAlarm = chkCpuAlarm.Checked;
            Properties.Settings.Default.chkGpuAlarm = chkGpuAlarm.Checked;
            Properties.Settings.Default.trkCpuThreshold = trkCpuThreshold.Value;
            Properties.Settings.Default.trkGpuThreshold = trkGpuThreshold.Value;
            Properties.Settings.Default.trkMonitorBottleneckThreshold = trkMonitorBottleneckThreshold.Value;
            Properties.Settings.Default.txtCpuAlarm = txtCpuAlarm.Text;
            Properties.Settings.Default.txtGpuAlarm = txtGpuAlarm.Text;
            Properties.Settings.Default.trkCpuVolume = trkCpuVolume.Value;
            Properties.Settings.Default.trkGpuVolume = trkGpuVolume.Value;
            Properties.Settings.Default.tcTabControl = tcTabControl.SelectedIndex;
            Properties.Settings.Default.cmbProcessorCounter = cmbProcessorCounter.SelectedIndex;
            Properties.Settings.Default.chkReassignIdealProcessor = chkReassignIdealProcessor.Checked;

            Properties.Settings.Default.rbNoAffinity = rbNoAffinity.Checked ;
            Properties.Settings.Default.rbHardAffinity = rbHardAffinity.Checked;
            Properties.Settings.Default.rbEcoQosAffinity = rbEcoQosAffinity.Checked ;
            Properties.Settings.Default.rbCpuSetsAffinity = rbCpuSetsAffinity.Checked ;




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
            if (tschkShowLastProcessor.Checked)
                tslblCurrentProcessor.Text = String.Empty; 
            else
                tslblCurrentProcessor.Text = "----";
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
            UpdateGpuInterval(timer1.Interval);
        }

        private string GetDateTimeStamp()
        {
            return $"{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} ";
        }


        // Deprecated, not called anymore: NvidiaLightPerfCounters-SERVICE not used anymore
        // Send IPC pipe command to NvidiaLightPerfCounters-SERVICE to update the refresh interval
        private void UpdateGpuInterval(int newInterval)
        {

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", "NvidiaLightPerfCountersPipeCommands", PipeDirection.Out))
                {
                    pipeClient.Connect(1000);
                    using (var writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine(newInterval.ToString());
                        writer.Flush();
                        writer.Close();
                    }
                    pipeClient.Close();

                }
            }
            catch (IOException ex)
            {
                LogError($"{GetDateTimeStamp()}An I/O error occurred: " + ex.Message, $"UpdateGpuInterval({newInterval})");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"{GetDateTimeStamp()}Access is denied: " + ex.Message, $"UpdateGpuInterval({newInterval})");
            }
            catch (Exception ex)
            {
                LogError($"{GetDateTimeStamp()}An unexpected error occurred: " + ex.Message, $"UpdateGpuInterval({newInterval})");
            }

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

        private void UpdatePercentageTrackers(TrackBar trackBar, Label label, int ? newValue )
        {
            if (newValue.HasValue)
            {
                trackBar.Value = newValue.Value;
            }

            label.Text = trackBar.Value.ToString() + "%";
            trackBar.Tag = (float)trackBar.Value;

        }

        private void trkCpuThreshold_Scroll(object sender, EventArgs e)
        {
            UpdatePercentageTrackers(trkCpuThreshold, lblCpuThreshold, null);
        }

        private void trkGpuThreshold_Scroll(object sender, EventArgs e)
        {
            UpdatePercentageTrackers(trkGpuThreshold, lblGpuThreshold, null);
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





        private void frmMain_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            tstxtAutoMoveY.Text = Location.X.ToString();
            //LogError("Occurs when form is moved to a monitor with a different resolution and scaling level, or when form's monitor scaling level is changed in the Windows settings.", "DpiChanged()", true);
        }

        private void frmMain_LocationChanged(object sender, EventArgs e)
        {
            tstxtAutoMoveY.Text = Location.X.ToString();
            //LogError("Event raised when the value of the Location property is changed on Control.", "LocationChanged()", true);
        }

        private void cmbProcessorCounter_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool prevValue = timer1.Enabled;
            timer1.Enabled = false;
            Thread.Sleep(500);

            const string CategoryName = "Processor Information";
            string CounterName = cmbProcessorCounter.SelectedItem.ToString();
            cpuCounter0 = new PerformanceCounter(CategoryName, CounterName, "0,0", true);
            cpuCounter1 = new PerformanceCounter(CategoryName, CounterName, "0,1", true);
            cpuCounter2 = new PerformanceCounter(CategoryName, CounterName, "0,2", true);
            cpuCounter3 = new PerformanceCounter(CategoryName, CounterName, "0,3", true);
            cpuCounter4 = new PerformanceCounter(CategoryName, CounterName, "0,4", true);
            cpuCounter5 = new PerformanceCounter(CategoryName, CounterName, "0,5", true);
            cpuCounter6 = new PerformanceCounter(CategoryName, CounterName, "0,6", true);
            cpuCounter7 = new PerformanceCounter(CategoryName, CounterName, "0,7", true);
            cpuCounter8 = new PerformanceCounter(CategoryName, CounterName, "0,8", true);
            cpuCounter9 = new PerformanceCounter(CategoryName, CounterName, "0,9", true);
            cpuCounter10 = new PerformanceCounter(CategoryName, CounterName, "0,10", true);
            cpuCounter11 = new PerformanceCounter(CategoryName, CounterName, "0,11", true);
            cpuCounter12 = new PerformanceCounter(CategoryName, CounterName, "0,12", true);
            cpuCounter13 = new PerformanceCounter(CategoryName, CounterName, "0,13", true);
            cpuCounter14 = new PerformanceCounter(CategoryName, CounterName, "0,14", true);
            cpuCounter15 = new PerformanceCounter(CategoryName, CounterName, "0,15", true);
            cpuCounter16 = new PerformanceCounter(CategoryName, CounterName, "0,16", true);
            cpuCounter17 = new PerformanceCounter(CategoryName, CounterName, "0,17", true);
            cpuCounter18 = new PerformanceCounter(CategoryName, CounterName, "0,18", true);
            cpuCounter19 = new PerformanceCounter(CategoryName, CounterName, "0,19", true);
            cpuCounter20 = new PerformanceCounter(CategoryName, CounterName, "0,20", true);
            cpuCounter21 = new PerformanceCounter(CategoryName, CounterName, "0,21", true);
            cpuCounter22 = new PerformanceCounter(CategoryName, CounterName, "0,22", true);
            cpuCounter23 = new PerformanceCounter(CategoryName, CounterName, "0,23", true);
            cpuCounter24 = new PerformanceCounter(CategoryName, CounterName, "0,24", true);
            cpuCounter25 = new PerformanceCounter(CategoryName, CounterName, "0,25", true);
            cpuCounter26 = new PerformanceCounter(CategoryName, CounterName, "0,26", true);
            cpuCounter27 = new PerformanceCounter(CategoryName, CounterName, "0,27", true);

            timer1.Enabled = prevValue;
        }

        private void trkMonitorBottleneckThreshold_Scroll(object sender, EventArgs e)
        {
            UpdatePercentageTrackers(trkMonitorBottleneckThreshold, lblMonitorBottleneckThreshold, null);
        }



        private void chkReassignIdealProcessor_CheckedChanged(object sender, EventArgs e)
        {
            // processor should not be assigned from the current thread
            // let's signal the need for that operation here
            needToCallSetNewIdealProcessor = chkReassignIdealProcessor.Visible && chkReassignIdealProcessor.Checked;
        }



        private void ReassignAffinity(object sender, EventArgs e)
        {
            if (rbNoAffinity.Checked)
            {
                CPU_QoS.SetHardAffinityProcess(CPU_QoS.CpuSetType.None); // whole process                
                CPU_QoS.SetEcoQoS(false); // current thread
                CPU_QoS.SetCpuSets(CPU_QoS.CpuSetType.None); // current thread
                                                             // 
                string msg = "Affinity changed/reset to No Specified Affinity.";
                cpuStats?.Reset(msg, true);
                lblEfficientCoresNote.Text = msg;                

            }
            else if (rbHardAffinity.Checked)
            {
                CPU_QoS.SetEcoQoS(false);
                CPU_QoS.SetCpuSets(CPU_QoS.CpuSetType.None);
                CPU_QoS.SetHardAffinityProcess(CPU_QoS.CpuSetType.Efficiency); // whole process

                string msg = "Affinity changed to Hard Affinity (whole process).";
                cpuStats?.Reset(msg, true);
                lblEfficientCoresNote.Text = msg;
            }
            else if (rbEcoQosAffinity.Checked)
            {
                CPU_QoS.SetHardAffinityProcess(CPU_QoS.CpuSetType.None); // whole process                
                CPU_QoS.SetEcoQoS(true);
                CPU_QoS.SetCpuSets(CPU_QoS.CpuSetType.None);

                string msg = "Affinity changed to EcoQoS (current thread only).";
                cpuStats?.Reset(msg, true);
                lblEfficientCoresNote.Text = msg;
            }
            else if (rbCpuSetsAffinity.Checked)
            {
                CPU_QoS.SetHardAffinityProcess(CPU_QoS.CpuSetType.None); // whole process
                CPU_QoS.SetEcoQoS(false);
                CPU_QoS.SetCpuSets(CPU_QoS.CpuSetType.Efficiency);

                string msg = "Affinity changed to Efficient CpuSets (current thread only).";
                cpuStats?.Reset(msg, true);
                lblEfficientCoresNote.Text = msg;
            }
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

        private void SetNewIdealProcessor(uint maxProcNumber)
        {
            if (maxProcNumber <= 0)
            {
                LogError("Invalid MaxProcessorNumber", $"SetNewIdealProcessor({maxProcNumber})");
                return;
            }


            // My Intel 14700K has 8 performance cores and 12 efficiency cores.
            // CPU numbers 0-15 are performance
            // CPU numbers 16-27 are efficiency            
            uint newIdealProcessor = processorAssigner.GetNextProcessor();

            IntPtr currentThreadHandle = GetCurrentThread();
            int previousProcessor = (int)SetThreadIdealProcessor(currentThreadHandle, newIdealProcessor);

            if (previousProcessor <0 || (previousProcessor > maxProcNumber))
            {
                LogError("Failed to set Ideal Processor", $"SetNewIdealProcessor({newIdealProcessor})={previousProcessor}");
                tslblIdealProcessor.Text = "ERR";
                return;
            }

            UpdateCaption(tslblIdealProcessor, (int)newIdealProcessor);
        }

        private void DetermineCpuTypeAndCount()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
            string processorName = regKey.GetValue("ProcessorNameString").ToString();
            currentProcess = Process.GetCurrentProcess();

            // We need to know the number of processors available to determine if Hyperthreading and Efficient cores are enabled
            regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (regKey != null)
            {
                // The number of subkeys corresponds to the number of CPUs
                CPUCount = regKey.SubKeyCount;
            }
            regKey.Close();


            if (processorName.Contains("12700K")) CPUType = CpuType.Intel_12700K;
            else if (processorName.Contains("14700K")) CPUType = CpuType.Intel_14700K;
            else CPUType = CpuType.Other;

            switch (CPUType)
            {
                case CpuType.Intel_12700K:
                    tslblCpuType.Text = "i7-12700K";
                    lblEfficientCoresNote.Text = "12700K  detected.";

                    if (CPUCount == 20) // Make sure HyperThreading and Efficient cores are enabled
                    {
                        if (processorAssigner == null) processorAssigner = new ProcessorAssigner((uint)CPUCount-1);
                        needToCallSetNewIdealProcessor = true; // Force flag because chkReassignIdealProcesso() onclick will miss it since the control wasn't enabled yet

                        chkReassignIdealProcessor.Visible = true;
                        lblReassignIdealProcessor.Visible = true;
                        chkReassignIdealProcessor.Enabled = true;

                        // Highlight my best cores according to my BIOS
                        label8.BorderStyle = BorderStyle.FixedSingle;
                        label12.BorderStyle = BorderStyle.FixedSingle;
                        label14.BorderStyle = BorderStyle.FixedSingle;
                    }

                    break;
                case CpuType.Intel_14700K:
                    tslblCpuType.Text = "i7-14700K";
                    lblEfficientCoresNote.Text = "14700K  detected.";

                    if (CPUCount == 28) // Make sure HyperThreading and Efficient cores are enabled
                    {
                        if (processorAssigner == null) processorAssigner = new ProcessorAssigner((uint)CPUCount - 1);
                        needToCallSetNewIdealProcessor = true; // Force flag because chkReassignIdealProcesso() onclick will miss it since the control wasn't enabled yet

                        chkReassignIdealProcessor.Visible = true;
                        lblReassignIdealProcessor.Visible = true;
                        chkReassignIdealProcessor.Enabled = true;


                        // Highlight my best cores according to my BIOS
                        label8.BorderStyle = BorderStyle.FixedSingle;
                        label12.BorderStyle = BorderStyle.FixedSingle;
                        label14.BorderStyle = BorderStyle.FixedSingle;
                    }
                    break;
                default:
                    //ignore
                    break;
            }


        }
      

        private void frmMain_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();
            DetermineCpuTypeAndCount();

            ResetTicksCounters();

            



            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);


            // Restore user preferences

            chkCpuAlarm.Checked = Properties.Settings.Default.chkCpuAlarm;
            chkGpuAlarm.Checked = Properties.Settings.Default.chkGpuAlarm;
            
            trkCpuThreshold.Value = Properties.Settings.Default.trkCpuThreshold;
            UpdatePercentageTrackers(trkCpuThreshold, lblCpuThreshold, null);
            
            trkGpuThreshold.Value = Properties.Settings.Default.trkGpuThreshold;
            UpdatePercentageTrackers(trkGpuThreshold, lblGpuThreshold, null);

            trkMonitorBottleneckThreshold.Value = Properties.Settings.Default.trkMonitorBottleneckThreshold;
            UpdatePercentageTrackers(trkMonitorBottleneckThreshold, lblMonitorBottleneckThreshold, null);

            txtCpuAlarm.Text = Properties.Settings.Default.txtCpuAlarm;
            txtGpuAlarm.Text = Properties.Settings.Default.txtGpuAlarm;

            trkCpuVolume.Value = Properties.Settings.Default.trkCpuVolume;
            lblCpuVolume.Text = trkCpuVolume.Value.ToString();

            trkGpuVolume.Value = Properties.Settings.Default.trkGpuVolume;
            lblGpuVolume.Text = trkGpuVolume.Value.ToString();

            chkReassignIdealProcessor.Checked = Properties.Settings.Default.chkReassignIdealProcessor; //this calls chkReassignIdealProcessor_OnClick()

            tcTabControl.SelectedIndex = Properties.Settings.Default.tcTabControl;

            rbNoAffinity.Checked = Properties.Settings.Default.rbNoAffinity;
            rbHardAffinity.Checked = Properties.Settings.Default.rbHardAffinity;    
            rbEcoQosAffinity.Checked = Properties.Settings.Default.rbEcoQosAffinity;
            rbCpuSetsAffinity.Checked = Properties.Settings.Default.rbCpuSetsAffinity;

            if (CPU_QoS.IsHybridCpu())
            {
                rbNoAffinity.Enabled = true;
                rbHardAffinity.Enabled = true;
                rbEcoQosAffinity.Enabled = true;
                rbCpuSetsAffinity.Enabled = true;
                ReassignAffinity(null, null);
                tschkShowLastProcessor.Checked = true;
                cpuStats = new ProcessorAssignmentStats(CPUCount);
            } else
                lblEfficientCoresNote.Text = "Not an Alder/Raptor Lake gen CPU, (not hybrid) efficient cores affinity options are disabled.";



            FillAudioDevices();

            maxCpuUtil = 0;
            maxCpuName = String.Empty;

            webServerThreadId = -1;
            dispatcherUIThread = -1;

            timer1.Enabled = false;
            timer1.Tag = false; // flag for one-time control in timer1_Tick(), only needed once


            tslblTop.Tag = 0;  // used to store frmMain.Top
            tslblCurrentProcessor.Tag = 255; // unrealistic processor assigment to force update in timer1

            tslblLT.Tag = false; // used to ignore the first LoopTime Max calculation in timer1
            tslblLoopTime.Tag = 0L; // used to keep track of the last one to avoid update in timer1

            tslblLastThread.Tag = 0; // used to keep track of the last thread used to avoid update in timer1

            myRTX4090 = new NvidiaGpu(0);

            InitializeCounterTags();
            ResetMaxCounters();            


            // Obtain current IP Address
            txtIPAddress.Text = GetMyIPAddress();

            diskCounterC = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "0 C:", true);
            diskCounterN = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "1 N:", true);
            diskCounterR = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "2 R:", true);

            // Load the previous selected Processor Counter
            // The following call will provoke a call to cmbProcessorCounter_SelectedIndexChanged
            // which in turn initializes all cpuCounters
            cmbProcessorCounter.SelectedIndex = Properties.Settings.Default.cmbProcessorCounter;


            // No longer using web server, not its pipes
            // StartWebServer(); 

            // Start Interprocess communication server using named pipes using a different thread
            pipeCancellationTokenSource = new CancellationTokenSource();
            pipeServerThread = new Thread(() => IPC_PipeServer(pipeCancellationTokenSource.Token));
            pipeServerThread.IsBackground = true;
            pipeServerThread.Start();


            stopwatch = new Stopwatch();

            nudPollingInterval.Value = Properties.Settings.Default.TimerInterval;
            timer1.Interval = (int)nudPollingInterval.Value;

            

            // Change the priority class to the previous setting selected (NORMAL, BELOW_NORMAL or IDLE)
            // This call must come after AssignEfficiencyCoresOnly
            cmbPriorityClass.SelectedIndex = Properties.Settings.Default.PriorityClassSelectedIndex;

            timer1.Enabled = tschkEnabled.Checked;
        }

        // When playing in VR I can't conveniently switch to performance monitor to change settings
        // Instead I user RemoteWindowControl to send IPC pipe messages to PerformanceMonitor
        // This way I can change tabs, restart the counters or monitor a different CPU counter
        private async void IPC_PipeServer(CancellationToken cancellationToken)
        {
            bool need_to_close = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("PerformanceMonitorCommandsPipe", PipeDirection.In))
                {
                    // Wait for a client to connect
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    int lines = 0;
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string command;
                        string result = "OK";
                        while ((command = await reader.ReadLineAsync()) != null)
                        {
                            // We need to execute the following code on a thread that can modify form objects/properties
                            this.Invoke(new Action(() =>
                            {

                                switch (command)
                                {
                                    case "MONITOR":
                                        tcTabControl.SelectTab("tbMonitor");
                                        break;
                                    case "ALERTS":
                                        tcTabControl.SelectTab("tbAlarm");
                                        break;
                                    case "SETTINGS":
                                        tcTabControl.SelectTab("tbSettings");
                                        break;
                                    case "ERRORS":
                                        tcTabControl.SelectTab("tbErrors");
                                        break;
                                    case "RESTART_COUNTERS":
                                        tsbtnResetMaxCounters_Click(null, null);
                                        break;
                                    case "GPU_UTILIZATION": //not-needed anymore, but could still come in a command
                                        //tscmbCategory.SelectedIndex = 0;
                                        break;
                                    case "GPU_TEMPERATURE": //not-needed anymore, but could still come in a command
                                        //tscmbCategory.SelectedIndex = 1;
                                        break;
                                    case "GPU_MEMORY_UTILIZATION": //not-needed anymore, but could still come in a command
                                        //tscmbCategory.SelectedIndex = 2;
                                        break;
                                    case "CYCLE_CPU_ALARM":
                                        chkCpuAlarm.Checked = !chkCpuAlarm.Checked;
                                        break;
                                    case "CYCLE_GPU_ALARM":
                                        chkGpuAlarm.Checked = !chkCpuAlarm.Checked;
                                        break;
                                    case "PROCESSOR_UTILITY":
                                        cmbProcessorCounter.SelectedIndex = 0;
                                        break;
                                    case "PROCESSOR_TIME":
                                        cmbProcessorCounter.SelectedIndex = 1;
                                        break;
                                    case "INTERRUPT_TIME":
                                        cmbProcessorCounter.SelectedIndex = 2;
                                        break;
                                    case "DPC_TIME":
                                        cmbProcessorCounter.SelectedIndex = 3;
                                        break;
                                    case "MINIMIZE":
                                        this.WindowState = FormWindowState.Minimized;
                                        break;
                                    case "RESTORE":
                                        this.WindowState = FormWindowState.Normal;
                                        break;
                                    case "FOREGROUND":
                                        this.BringToFront();
                                        this.Focus();
                                        break;
                                    case string s when s.StartsWith("ALIGN_LEFT"):
                                        if (s.Length > 10)
                                        {
                                            string new_position = s.Substring(10);
                                            int x;
                                            if (int.TryParse(new_position, out x))
                                                this.Location = new Point(x, this.Location.Y);
                                        }
                                        break;
                                    case "TOPMOST":
                                        // The following doesn't work, had to fix with local copy
                                        // this.TopMost = !(this.TopMost);
                                        topMost = !topMost;
                                        this.TopMost = topMost;
                                        result = "Now .TopMost=" + this.TopMost.ToString();
                                        break;
                                    case "CLOSE":
                                        need_to_close = true;
                                        pipeCancellationTokenSource.Cancel();
                                        break;
                                    default:
                                        result = "Unknown command";
                                        break;
                                }
                                string info = $"Received command #${++lines}):[{command}][{result}]";
                                LogError(info, "PipeServer", true);

                            }));
                        }
                    }
                }
            }


            // We need to execute the following code on a thread that can modify form objects/properties
            if (need_to_close) this.Invoke(new Action(() =>
            {
                this.Close();
            }));

        }

        // Function Inlining: Only in this particular case, I prefer to repeat code in this case instead of passing all parameters to the similar function
        // default threshold of 10000 is larger than cpu/memory utilization percentage and fan-rpm, so it won't trigger the red color for those counters, but it will trigger for disk throughput in MB/s which can be a bottleneck
        private void UpdateCounter(int counter, ProgressBar pb, Label lbl, string dimensional = "%", int threshold = 10000)
        {

            if (this.WindowState != FormWindowState.Minimized && (int)lbl.Tag != counter)
            {
                lbl.Tag = counter;

                System.Drawing.Color color = counter < threshold ? (System.Drawing.Color)pb.Tag : System.Drawing.Color.Red;
                if (pb.ForeColor != color) pb.ForeColor = color;
                pb.Value = counter <= 100 ? counter : 100;

                // Let's notify with red when counter >= threshold to denote possible bottleneck
                color = counter < threshold ? System.Drawing.Color.Black : System.Drawing.Color.Red;
                if (lbl.ForeColor != color) lbl.ForeColor = color;
                lbl.Text = $"{counter:F1}{dimensional}";
            }

        }

        // Function Inlining: Only in this particular case, I prefer to repeat code in this case instead of passing all parameters to the similar function
        private void UpdateCounter(float counterValue, ProgressBar pb, Label lbl, ref int maxCounterValue, string dimensional = "%")
        {
            int v = (int)counterValue;
            if (v > maxCounterValue) maxCounterValue = v;
           

            
            // Only update the label if it is visible in the selected tab and the value has changed
            if (lbl.Parent == tcTabControl.SelectedTab && this.WindowState != FormWindowState.Minimized && (int)lbl.Tag != v)
            {
                lbl.Tag = v;
                float threshold= (float) trkMonitorBottleneckThreshold.Tag; // this is is the same as trkMonitorBottleneckThreshold.Value but float cached

                // Let's notify with red when counter >= trkMonitorBottleneckThreshold.Value to denote possible bottleneck
                Color color = counterValue < threshold ? (Color)pb.Tag : Color.Red;

                if (pb.ForeColor != color) pb.ForeColor = color;
                pb.Value = v <= 100 ? v : 100;


                color = counterValue < threshold ? Color.Black : Color.Red;
                if (lbl.ForeColor != color) lbl.ForeColor = color;
                lbl.Text = $"{counterValue:F1}{dimensional}";

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

        private void SmartUpdateColor(Label labelDest, Color OriginalColor, Color NewColor, float Percentage, float Threshold)
        {
            // Changing colors is more expensive than checking if color change is needed
            if (Percentage>=Threshold && labelDest.ForeColor != NewColor)
            {
                labelDest.ForeColor = NewColor;
            } else if (Percentage < Threshold && labelDest.ForeColor != OriginalColor)
            {
                labelDest.ForeColor= OriginalColor;
            }
        }
        
        private void UpdateGPUInfo()
        {
            // Need to read temp too to get the fan speed??, so all updated here
            int temp = myRTX4090.TemperatureC;
            int fanspeed = myRTX4090.FanSpeed;
            int util = myRTX4090.Utilization; // this also reads memory utilization

            // Always Track max Gpu util (alarms)
            float newVolume = 0.0f; // mute the alarm if not above threshold
            if (util >= trkGpuThreshold.Value)
            {
                newVolume = trkGpuVolume.Value / 100.0f;
                TotalGpuTicksAboveThreshold += timer1.Interval;
            }
            if (mpGpu != null) mpGpu.Volume = newVolume;

            float abovePct = (TotalGpuTicksAboveThreshold / TotalTicks) * 100.0f;
            SmartUpdateColor(lblGpuAbovePct, lblRTX4090GPU.ForeColor, Color.Red, abovePct, (float)trkGpuThreshold.Tag);
            UpdateCaption(lblGpuAbovePct, abovePct, "%");

            SmartUpdateColor(lblGpuAlarm, lblRTX4090GPU.ForeColor, Color.Red, util, (float)trkGpuThreshold.Tag);
            UpdateCaption(lblGpuAlarm, util, "%");


            ExCounter += myRTX4090.ReadResetExceptionsCounter;

            UpdateCounter(util, pbGPU0, lblGPU0, "%", trkMonitorBottleneckThreshold.Value);
            UpdateCounter(temp, pbGPUTemp, lblGPUTemp, "°C", 80);
            UpdateCounter(myRTX4090.MemoryUtilization, pbGPUMem, lblGPUMem, "?", trkMonitorBottleneckThreshold.Value);

            // update Fan-Speed
            UpdateCounter(fanspeed, pbGPUFanSpeed, lblGPUFanSpeed, "%", trkMonitorBottleneckThreshold.Value);
        }

        private void UpdateMonitorLabels()
        {
            if (needToCallSetNewIdealProcessor && chkReassignIdealProcessor.Checked && chkReassignIdealProcessor.Enabled)
            {
                needToCallSetNewIdealProcessor = false;
                SetNewIdealProcessor((uint)CPUCount-1); // This one also displays the new ideal processor
            }

            float incrementalTicks = timer1.Interval; 
            TotalTicks += incrementalTicks;

            if (tschkShowLastThread.Checked)
            {
                
                UpdateCaption(tslblLastThread, (int)GetCurrentThreadId());
            }

            if (tschkShowLastProcessor.Checked)
            {
                uint cpu = GetCurrentProcessorNumber();
                UpdateCaption(tslblCurrentProcessor, (int)cpu);
                cpuStats?.Tick(cpu);
            }

            UpdateGPUInfo();



            float[] counterValues = new float[28];
            try
            {
                counterValues[0] = cpuCounter0.NextValue();
                counterValues[1] = cpuCounter1.NextValue();
                counterValues[2] = cpuCounter2.NextValue();
                counterValues[3] = cpuCounter3.NextValue();
                counterValues[4] = cpuCounter4.NextValue();
                counterValues[5] = cpuCounter5.NextValue();
                counterValues[6] = cpuCounter6.NextValue();
                counterValues[7] = cpuCounter7.NextValue();
                counterValues[8] = cpuCounter8.NextValue();
                counterValues[9] = cpuCounter9.NextValue();
                counterValues[10] = cpuCounter10.NextValue();
                counterValues[11] = cpuCounter11.NextValue();
                counterValues[12] = cpuCounter12.NextValue();
                counterValues[13] = cpuCounter13.NextValue();
                counterValues[14] = cpuCounter14.NextValue();
                counterValues[15] = cpuCounter15.NextValue();
                counterValues[16] = cpuCounter16.NextValue();
                counterValues[17] = cpuCounter17.NextValue();
                counterValues[18] = cpuCounter18.NextValue();
                counterValues[19] = cpuCounter19.NextValue();
                counterValues[20] = cpuCounter20.NextValue();
                counterValues[21] = cpuCounter21.NextValue();
                counterValues[22] = cpuCounter22.NextValue();
                counterValues[23] = cpuCounter23.NextValue();
                counterValues[24] = cpuCounter24.NextValue();
                counterValues[25] = cpuCounter25.NextValue();
                counterValues[26] = cpuCounter26.NextValue();
                counterValues[27] = cpuCounter27.NextValue();
            } catch(Exception ex)
            {
                ExCounter++;
                LogError(ex.Message, $"cpuCounterXX.NextValue()");
            }


            maxCpuUtil = 0;
            //Unrolled for now, change everything to arrays later
            UpdateCounter(counterValues[0], pbCPU0, lblCPU0, ref maxCpuUtil);

            UpdateCounter(counterValues[1], pbCPU1, lblCPU1, ref maxCpuUtil);
            UpdateCounter(counterValues[2], pbCPU2, lblCPU2, ref maxCpuUtil);
            UpdateCounter(counterValues[3], pbCPU3, lblCPU3, ref maxCpuUtil);
            UpdateCounter(counterValues[4], pbCPU4, lblCPU4, ref maxCpuUtil);
            UpdateCounter(counterValues[5], pbCPU5, lblCPU5, ref maxCpuUtil);
            UpdateCounter(counterValues[6], pbCPU6, lblCPU6, ref maxCpuUtil);
            UpdateCounter(counterValues[7], pbCPU7, lblCPU7, ref maxCpuUtil);
            UpdateCounter(counterValues[8], pbCPU8, lblCPU8, ref maxCpuUtil);
            UpdateCounter(counterValues[9], pbCPU9, lblCPU9, ref maxCpuUtil);
            UpdateCounter(counterValues[10], pbCPU10, lblCPU10, ref maxCpuUtil);
            UpdateCounter(counterValues[11], pbCPU11, lblCPU11, ref maxCpuUtil);
            UpdateCounter(counterValues[12], pbCPU12, lblCPU12, ref maxCpuUtil);
            UpdateCounter(counterValues[13], pbCPU13, lblCPU13, ref maxCpuUtil);
            UpdateCounter(counterValues[14], pbCPU14, lblCPU14, ref maxCpuUtil);
            UpdateCounter(counterValues[15], pbCPU15, lblCPU15, ref maxCpuUtil);
            UpdateCounter(counterValues[16], pbCPU16, lblCPU16, ref maxCpuUtil);
            UpdateCounter(counterValues[17], pbCPU17, lblCPU17, ref maxCpuUtil);
            UpdateCounter(counterValues[18], pbCPU18, lblCPU18, ref maxCpuUtil);
            UpdateCounter(counterValues[19], pbCPU19, lblCPU19, ref maxCpuUtil);
            UpdateCounter(counterValues[20], pbCPU20, lblCPU20, ref maxCpuUtil);
            UpdateCounter(counterValues[21], pbCPU21, lblCPU21, ref maxCpuUtil);
            UpdateCounter(counterValues[22], pbCPU22, lblCPU22, ref maxCpuUtil);
            UpdateCounter(counterValues[23], pbCPU23, lblCPU23, ref maxCpuUtil);
            UpdateCounter(counterValues[24], pbCPU24, lblCPU24, ref maxCpuUtil);
            UpdateCounter(counterValues[25], pbCPU25, lblCPU25, ref maxCpuUtil);
            UpdateCounter(counterValues[26], pbCPU26, lblCPU26, ref maxCpuUtil);
            UpdateCounter(counterValues[27], pbCPU27, lblCPU27, ref maxCpuUtil);


            // Always Track max Cpu util (alarms)
            float newVolume = 0.0f; // mute the alarm if not above threshold
            if (maxCpuUtil >= trkCpuThreshold.Value)
            {
                newVolume = trkCpuVolume.Value / 100.0f;
                TotalCpuTicksAboveThreshold += incrementalTicks;
            }
            if (mpCpu != null) mpCpu.Volume = newVolume;


            float aboveThPct = (TotalCpuTicksAboveThreshold / TotalTicks) * 100.0f;
            SmartUpdateColor(lblCpuAbovePct, lblEfficientCoresNote.ForeColor, Color.Red, aboveThPct, (float)trkCpuThreshold.Tag);
            UpdateCaption(lblCpuAbovePct, aboveThPct, "%");

            SmartUpdateColor(lblCpuAlarm, lblEfficientCoresNote.ForeColor, Color.Red, maxCpuUtil, (float)trkCpuThreshold.Tag);
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

            // Main call to update counters
            UpdateMonitorLabels();


            timer1.Enabled = true;
            stopwatch.Stop();

            long elapsed_ms = stopwatch.ElapsedMilliseconds;
            UpdateCaption(tslblLoopTime, elapsed_ms);

            
            if (elapsed_ms > (long)tslblMaxLoopTime.Tag)
            {
                if ((bool)tslblLT.Tag) // ignore the first Max
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
                <head><title>Performance Monitor Light - Web IPC_PipeServer</title>
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
                    <hr>{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} Web IPC_PipeServer ThreadID: {webServerThreadId.ToString()} Dispatcher ThreadID: {dispatcherUIThread}
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

        private void LogError(string message, string function, bool forced = false)
        {
            if (forced || ExCounter <= 100) // Only log the first 100 errors
            {
                // Get the current timestamp
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                // Format the error newInterval with the timestamp
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

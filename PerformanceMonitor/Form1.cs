using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.ServiceProcess;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;




namespace PerformanceMonitor
{
    public partial class frmMain : Form
    {
        private PerformanceCounter cpuCounter0, cpuCounter1, cpuCounter2, cpuCounter3, cpuCounter4, cpuCounter5, cpuCounter6, cpuCounter7, cpuCounter8, cpuCounter9, cpuCounter10, cpuCounter11, cpuCounter12, cpuCounter13, cpuCounter14, cpuCounter15, cpuCounter16, cpuCounter17, cpuCounter18, cpuCounter19;
        //private PerformanceCounter gpuUtilizationCounter, gpuFanCounter;
        private Stopwatch stopwatch;
        private long ExCounter;  // Exceptions Counter
        private PerformanceCounter diskCounterC, diskCounterN, diskCounterR;
        private NvidiaGpu myRTX4090;

        private HttpListener listener; // Web Server for remote control location and focus commands
        private int webServerThreadId, dispatcherUIThread;
        private Process currentProcess;


        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.TimerInterval = (int)nudPollingInterval.Value;
            Properties.Settings.Default.PriorityClassSelectedIndex = cmbPriorityClass.SelectedIndex;
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
        }


        // Import the GetCurrentThread API
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

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
        private void frmMain_Load(object sender, EventArgs e)
        {
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



            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Get the current process
            currentProcess = Process.GetCurrentProcess();

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
            {
                // Define the CPU affinity mask for CPUs 17 to 20
                // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                IntPtr affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                // Set the CPU affinity
                currentProcess.ProcessorAffinity = affinityMask;

                //gpuUtilizationCounter = new PerformanceCounter("GPU", "% GPU Time", "nvidia geforce rtx 4090(01:00)");
                //gpuFanCounter = new PerformanceCounter("GPU", "% GPU Fan Speed", "nvidia geforce rtx 4090(01:00)");

                tslbl12700K.Tag = true;  // special tag to indicate that this is a 12700K
                lbl12700KNote.Visible = true;
            }
            else
            {
                //gpuUtilizationCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", true);  //Generic for debugging
                tslbl12700K.Tag = false; // special tag to indicate that this is not a 12700K
            }

            regKey.Close();

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

            StartWebServer();

            stopwatch = new Stopwatch();

            nudPollingInterval.Value = Properties.Settings.Default.TimerInterval;
            timer1.Interval = (int)nudPollingInterval.Value;
            timer1.Enabled = tschkEnabled.Checked;
        }

        // Function Inlining: I prefer to repeat code in this case instead of passing all parameters to the similar function
        private void UpdateCounter(int counter, ProgressBar pb, Label lbl, string dimensional = "%")
        {
            if ((int)lbl.Tag != counter)
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

        // Function Inlining: I prefer to repeat code in this case instead of passing all parameters to the similar function
        private void UpdateCounter(PerformanceCounter counter, ProgressBar pb, Label lbl, string dimensional = "%")
        {
            float f;
            try
            {
                f = counter.NextValue();
            }
            catch (Exception ex)
            {
                f = 0.0f;
                ExCounter++;
                LogError(ex.Message, $"UpdateCounter({pb.Name})");
            }

            int v = (int)f;

            if ((int)lbl.Tag != v)
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

        private void UpdateGPUInfo()
        {
            // Need to read temp too to get the fan speed
            int util = myRTX4090.Utilization;
            int temp = myRTX4090.TemperatureC;
            int fanSpeed = myRTX4090.FanSpeed;
            ExCounter += myRTX4090.ReadResetExceptionsCounter;

            // update the GPU-Utilization or the GPU-Temperature
            switch (tscmbCategory.SelectedIndex)
            {
                case 0: // %GPU Time
                    UpdateCounter(util, pbGPU0, lblGPU0, "%");
                    break;
                default: // GPU Temperature (in degrees C)
                    UpdateCounter(temp, pbGPU0, lblGPU0, "°C");
                    break;
            }

            // update Fan-Speed
            UpdateCounter(fanSpeed, pbGPUFanSpeed, lblGPUFanSpeed);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            stopwatch.Restart();

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


/*
            if (!(bool)timer1.Tag) // only do this once
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                if ((bool)tslbl12700K.Tag) // special flag equals True when this processor is a 12700K
                {
                    // Get the pseudo handle for the current thread
                    IntPtr currentThreadHandle = GetCurrentThread();

                    // Set the ideal processor for the current thread to 19.   GpuPerfCounters is assigned 18
                    uint previousIdealProcessor = SetThreadIdealProcessor(currentThreadHandle, 19);
                }
                timer1.Tag = true;  // flag for one-time control in timer1_Tick()
            }
*/
            if (tschkShowLastThread.Checked)
            {
                int th = (int)GetCurrentThreadId(); 
                if ((int)tslblLastThread.Tag != th)
                {                    
                    tslblLastThread.Tag = th;
                    tslblLastThread.Text = th.ToString();
                }
                
            }

            if (tschkShowLastProcessor.Checked)
            {
                int processorNumber = (int)GetCurrentProcessorNumber();
                if ((int)tslblCurrentProcessor.Tag != processorNumber)
                {
                    tslblCurrentProcessor.Tag = processorNumber;
                    tslblCurrentProcessor.Text = processorNumber.ToString();
                }
            }


            UpdateGPUInfo();

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

            UpdateDisk(diskCounterC, lblDiskC, lblMaxDiskC);
            UpdateDisk(diskCounterN, lblDiskN, lblMaxDiskN);
            UpdateDisk(diskCounterR, lblDiskR, lblMaxDiskR);

            if (ExCounter != (long)tslblExceptions.Tag)
            {
                tslblExceptions.Tag = ExCounter;
                tslblExceptions.Text = ExCounter.ToString();
            }

            stopwatch.Stop();

            long elapsed_ms = stopwatch.ElapsedMilliseconds;

            if ((long)tslblLoopTime.Tag != elapsed_ms)
            {
                tslblLoopTime.Tag = elapsed_ms;
                tslblLoopTime.Text = elapsed_ms.ToString();
            }

            if (elapsed_ms > (long)tslblMaxLoopTime.Tag)
            {
                if ((bool)tslblLT.Tag) // ignore the first Max
                    tslblMaxLoopTime.Tag = elapsed_ms; 
                else
                    tslblLT.Tag = true;

                tslblMaxLoopTime.Text = tslblLoopTime.Text;  // update regardless of the first-time-ignore for information purposes
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetMaxCounters();

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


    }
}

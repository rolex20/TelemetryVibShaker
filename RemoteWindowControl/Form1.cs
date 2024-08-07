using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Media;



namespace RemoteWindowControl
{
    public enum CpuType
    {
        Intel_12700K,
        Intel_14700K,
        Other
    }

    public partial class frmMain : Form
    {
        private Process currentProcess;
        private HttpListener listener; // Web Server for remote control location and focus commands
        private int webServerThreadId;
        private int ExCounter = 0;  // Exceptions Counter
        private string HTML_Template;
        private Mutex SingleInstanceMutex;
        private ProgramList programsList;

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();


        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_RESTORE = 9;

        public static void MinimizeWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_SHOWMINIMIZED);
        }

        public static void MaximizeWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_SHOWMAXIMIZED);
        }

        public static void RestoreWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        // Define the RECT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        // Define the POINT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // Define the WINDOWPLACEMENT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
            // The following field is not used in the .NET definition
            // public RECT rcDevice;
        }

        // Declare the GetWindowRect function
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);



        // Declare the GetWindowPlacement function
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        public static void MoveWindowToCoordinates(IntPtr hWnd, int x, int y)
        {
            SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private IntPtr GetHandleByProcessName(string ProcessName, int Instance)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(ProcessName);
                IntPtr windowHandle = processes[Instance].MainWindowHandle;
                return windowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr MoveResizeWindow(string ProcessName, int InstanceNumber, int X, int Y, int NewState)
        {
            IntPtr windowHandle = GetHandleByProcessName(ProcessName, InstanceNumber);
            if (windowHandle == IntPtr.Zero) return IntPtr.Zero;

            MoveWindowToCoordinates(windowHandle, X, Y);

            switch (NewState)
            {
                case -1:
                    MinimizeWindow(windowHandle);
                    break;
                case 1:
                    RestoreWindow(windowHandle);
                    break;
                case 2:
                    MaximizeWindow(windowHandle);
                    break;
            }

            return windowHandle;
        }




        private RECT GetWindowPosition(string ProcessName, int InstanceNumber)
        {
            IntPtr windowHandle = GetHandleByProcessName(ProcessName, InstanceNumber);
            return GetWindowPosition(windowHandle);
        }

        private RECT GetWindowPosition(IntPtr hWnd)
        {
            RECT rect = new RECT();
            if (hWnd != IntPtr.Zero)
                GetWindowRect(hWnd, out rect);
            else
            {
                rect.Left = 0;
                rect.Top = 0;
            }

            return rect;
        }



        private void StartWebServer()
        {
            listener = new HttpListener();
            string prefix = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                ExCounter++;
                SystemSounds.Beep.Play();
                tabControls.SelectTab("tabDebug");
                string functionName = "StartWebServer{ listener.Start(); }";
                LogError(ex.Message, functionName);
                LogError("For permission issues, run the command: [netsh http add urlacl url=http://+:8080/ user=YourUserName]", functionName);
            }
            Task.Run(() => ProcessRequest());
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const UInt32 WM_CLOSE = 0x0010;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);


        private void LaunchProgram(string exePath)
        {
            try
            {
                // Extract the directory from the full file path
                string directory = Path.GetDirectoryName(exePath);

                // Prepare the process to run
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath, // Set the executable file path
                    WorkingDirectory = directory, // Set the start directory
                    UseShellExecute = true // Use the system shell to start the process
                };

                // Start the external process
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                LogError($"Error launching program: {ex.Message}", "LaunchProgram()");
            }
        }
        private string GetLaunchPath(string ProcessName)
        {
            foreach (ProgramDetails program in programsList.Programs)
            {
                if (program.ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return program.Path;
                }
            }
            return String.Empty;
        }

        private async Task ProcessRequest()
        {
            while (true)
            {
                var context = await listener.GetContextAsync(); // After this call, I have a new web request
                var request = context.Request;
                var response = context.Response;

                webServerThreadId = (int)GetCurrentThreadId();

                lblWebServerThreadId.BeginInvoke(new Action(() => { lblWebServerThreadId.Text = webServerThreadId.ToString(); }));


                string footer = $"{GetDateTimeStamp()}Ready";
                int newX = 0;
                int newY = 0;
                string processName = String.Empty;
                IntPtr procHandle = IntPtr.Zero;
                if (request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string formData = await reader.ReadToEndAsync();

                        var parameters = formData.Split('&')
                                                .Select(param => param.Split('='))
                                                .ToDictionary(param => param[0], param => WebUtility.UrlDecode(param[1]));


                        parameters.TryGetValue("Process", out processName);
                        if (processName.Length > 0)
                        {
                            LogError($"ProcessName=[{processName}]", "ProcessRequest()");

                            string temp;
                            int x = 0;
                            int y = 0;

                            if (parameters.TryGetValue("Command", out temp))
                            {
                                LogError("Clicked Command for " + processName, "ProcessRequest()");
                                parameters.TryGetValue("PipeCommand", out temp);
                                footer = SendCommand(temp);
                            }
                            else if (parameters.TryGetValue("Exit", out temp))
                            {
                                LogError("Clicked Terminate for " + processName, "ProcessRequest()");
                                Process[] processes = Process.GetProcessesByName(processName);
                                if (processes.Length > 0)
                                {
                                    IntPtr hWnd = processes[0].MainWindowHandle;
                                    SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                    footer = $"{GetDateTimeStamp()} Successfully requested graceful-exit/kill for [{processName}]";
                                    if (IsWindow(hWnd) && !processes[0].CloseMainWindow()) processes[0].Kill();
                                }
                                else
                                    footer = $"{GetDateTimeStamp()} Process Not Found [{processName}]";
                            }
                            else if (parameters.TryGetValue("MakeForeground", out temp))
                            {
                                LogError("Clicked Make Foreground for " + processName, "ProcessRequest()");
                                Process[] processes = Process.GetProcessesByName(processName);
                                if (processes.Length > 0)
                                {
                                    IntPtr hWnd = processes[0].MainWindowHandle;
                                    if (hWnd != IntPtr.Zero)
                                    {
                                        // Restore the window if it is minimized
                                        if (IsIconic(hWnd))
                                        {
                                            ShowWindowAsync(hWnd, SW_RESTORE);
                                        }

                                        // Bring the window to the foreground
                                        SetForegroundWindow(hWnd);
                                        footer = $"{GetDateTimeStamp()} Successfully requested SetForegroundWindow [{processName}]";
                                    }
                                    else
                                    {
                                        string msg = $"{GetDateTimeStamp()}{processName} does not have a MainWindowHandle";
                                        LogError(msg, "MakeForeground");
                                        footer = msg;
                                    }
                                }
                                else
                                    footer = $"{GetDateTimeStamp()} Process Not Found [{processName}]";
                            }
                            else if (parameters.TryGetValue("GetValues", out temp))
                            {
                                LogError("Clicked GetValues for " + processName, "ProcessRequest()");
                                RECT rect = GetWindowPosition(processName, 0);
                                newX = rect.Left;
                                newY = rect.Top;
                                footer = $"{GetDateTimeStamp()} Successfully requested coordinates read for [{processName}]";
                            }
                            else if (parameters.TryGetValue("Launch", out temp))
                            {
                                LogError($"Clicked Launch for {processName}", "ProcessRequest()");
                                string exePath = GetLaunchPath(processName);
                                if (exePath.Length > 0)
                                {
                                    LaunchProgram(exePath);
                                }
                            }
                            else if (parameters.TryGetValue("X", out string sx) && parameters.TryGetValue("Y", out string sy)) // At this point the only left option is Submit new coordinates for move operation
                            {
                                x = int.Parse(sx);
                                y = int.Parse(sy);
                                LogError($"X={x}, Y={y}", "ProcessRequest()");


                                if (parameters.TryGetValue("Move", out temp))
                                {
                                    LogError("Clicked Move", "ProcessRequest()");

                                    // Check required new state: Minimized, Restore, NoChange
                                    parameters.TryGetValue("State", out temp);
                                    int newState = int.TryParse(temp, out int parsedValue) ? parsedValue : 0;

                                    parameters.TryGetValue("Instance", out temp);
                                    int.TryParse(temp, out int instance);

                                    procHandle = MoveResizeWindow(processName, instance, x, y, newState);
                                    footer = $"{GetDateTimeStamp()} Successfully requested coordinates change to X: {x}, Y: {y} for [{processName}]";

                                    RECT rect = GetWindowPosition(procHandle);
                                    newX = rect.Left;
                                    newY = rect.Top;
                                }
                            }

                        }
                        else
                            footer = "Please select a Process and try again.";
                    }
                }

                if (!chkUseCachedHTML.Checked) HTML_Template = ReadFileContents(txtHTML.Text);
                string HTML = HTML_Template.Replace("{X}", newX.ToString());
                HTML = HTML.Replace("{Y}", newY.ToString());
                HTML = HTML.Replace("{cmbProcess}", GetHtmlProcessCombo(processName));
                HTML = HTML.Replace("{footer}", footer);

                SendResponse(response, HTML);

            }
        }

        private string GetDateTimeStamp()
        {
            return $"{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} ";
        }

        private string SendCommand(string command)
        {
            if (string.IsNullOrEmpty(command) || !command.Contains('|'))
            {
                return $"{GetDateTimeStamp()}Invalid pipe command [{command}]";
            }

            string[] values = command.Split('|');
            if (values.Length != 2 || string.IsNullOrEmpty(values[0]) || string.IsNullOrEmpty(values[1]))
            {
                return $"{GetDateTimeStamp()}Missing part for pipe command [{command}]";
            }

            string pipeName = values[0];
            string message = values[1];

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000);
                    using (var writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                        writer.Close();
                    }
                    pipeClient.Close();

                }
            }
            catch (IOException ex)
            {
                return ($"{GetDateTimeStamp()}An I/O error occurred: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ($"{GetDateTimeStamp()}Access is denied: " + ex.Message);
            }
            catch (Exception ex)
            {
                return ($"{GetDateTimeStamp()}An unexpected error occurred: " + ex.Message);
            }


            return $"{GetDateTimeStamp()}Pipe Command Sent [{command}]";
        }

        private string GetHtmlProcessCombo(string ProcessName)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<select name='Process' required>");

            foreach (ProgramDetails program in programsList.Programs)
            {
                string selected = program.ProcessName.Equals(ProcessName) ? " selected " : String.Empty;
                html.Append($"<option {selected} value='{program.ProcessName}'>{program.FriendlyName}</option>");
            }
            html.Append("</select>");

            return html.ToString();
        }

        public string ReadFileContents(string filename)
        {
            try
            {
                // Open the text file using a stream reader.
                using (StreamReader reader = new StreamReader(filename))
                {
                    // Read the stream as a string.
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
            catch (IOException e)
            {
                LogError($"The file could not be read: {e.Message}", "ReadFileContents()");
                return string.Empty;
            }
        }

        private void SendResponse(HttpListenerResponse response, string HTML)
        {

            byte[] buffer = Encoding.UTF8.GetBytes(HTML);
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
                if (txtDebug.InvokeRequired)
                {
                    txtDebug.Invoke(new Action(() => txtDebug.AppendText(errorMessage)));
                }
                else
                {
                    txtDebug.AppendText(errorMessage);
                }
            }
        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            SingleInstanceChecker();

            txtDebug.Tag = 0; // Used to track the number of errors detected

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);
            ProcessorCheck();

            if (chkAutostart.Checked)
            {
                Task.Run(() => AutoStartWebServer());
            }
        }

        private async Task AutoStartWebServer()
        {
            for (int i = 10; i >= 0; i--)
            {
                lblCountDownTimer.BeginInvoke(new Action(() => { lblCountDownTimer.Text = i.ToString(); }));
                if (!chkAutostart.Checked || !btnStartWebServer.Enabled) break;
                System.Threading.Thread.Sleep(1000);
            }
            lblCountDownTimer.BeginInvoke(new Action(() => { lblCountDownTimer.Text = String.Empty; }));

            // Verify that the user hasn't aborted the autostart or clicked manually on btnStartWebServer
            if (chkAutostart.Checked && btnStartWebServer.Enabled)
                btnStartWebServer_Click(null, null);
        }

        private void txtIPAddress_TextChanged(object sender, EventArgs e)
        {
            UpdateLink();
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            UpdateLink();
        }

        private void UpdateLink()
        {
            // Check if this function is being called from a thread other than the UI thread
            //lblLink.Invoke(new Action(() => lblLink.Text = $"http://{txtIPAddress.Text}:{txtPort.Text}/"));

            lblLink.Text = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
        }

        private void LoadProgramsConfigurationFile()
        {
            string json = File.ReadAllText(txtProgramsConfigurationFile.Text);
            // Deserialize the JSON into a list of ProgramInfo objects

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            programsList = JsonSerializer.Deserialize<ProgramList>(json, options);
        }

        private void btnStartWebServer_Click(object sender, EventArgs e)
        {
            LoadProgramsConfigurationFile();

            // Remember this could be called from another thread
            this.BeginInvoke(new Action(() =>
            {
                btnStartWebServer.Enabled = false;
                txtHTML.Enabled = false;

                // Minimize if required
                if (chkAutostart.Checked) this.WindowState = FormWindowState.Minimized;

                UpdateLink();

                // Get all processes running on the local computer.
                Process[] processList = Process.GetProcesses();

                // Iterate through all the processes and print their names
                foreach (Process proc in processList)
                {
                    Debug.WriteLine("Process: {0} ID: {1}", proc.ProcessName, proc.Id);
                }
            }));


            //Prepare the cached copy of the HTML Template
            HTML_Template = ReadFileContents(txtHTML.Text);



            StartWebServer();
        }

        private void ProcessorCheck()
        {
            // LoadSettings must be called before ProcessorCheck()

            // Get the current process
            currentProcess = Process.GetCurrentProcess();

            // Assign only Efficiency cores if requested and CPU==12700K
            chkUseEfficiencyCoresOnly_CheckedChanged(null, null);

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            txtDebug.Text = String.Empty; // We don't want to save this to Properties.Settings

            if (this.WindowState != FormWindowState.Minimized)
            {
                Properties.Settings.Default.XCoordinate = this.Location.X;
                Properties.Settings.Default.YCoordinate = this.Location.Y;
            }

            // Save the settings for all controls in the form
            SaveSettings(this);

            // SaveSettings() is recursive so calling Save() below
            Properties.Settings.Default.Save();

        }

        // This method loads the setting Value for a given control
        private void LoadSetting(Control control)
        {

            // Get the name of the control
            string controlName = control.Name;

            // If the control name is not null or empty
            if (string.IsNullOrEmpty(controlName)) return;


            // Get the control's property Value according to its type
            MyControlInfo controlInfo = new MyControlInfo(control);
            if (controlInfo.Value != null) // Load Value only if I am interested in this control
            {
                // If the next line fails, remember to add that control to the Properties.Settings:
                // Right click on the Project, select Properties, click on General and then click on
                // the link "Create or open application settings"
                object savedSettingValue = Properties.Settings.Default[controlName];
                controlInfo.AssignValue(savedSettingValue);// Get the setting Value from the Properties.Settings.Default
            }




        }


        // This method saves the setting Value for a given control
        private void SaveSetting(Control control)
        {
            // Get the name of the control
            string controlName = control.Name;

            if (string.IsNullOrEmpty(controlName)) return;


            MyControlInfo controlInfo = new MyControlInfo(control);

            // Get the control's property Value according to its type
            //object Value = RestorableSetting(control);

            // Save setting only if I am interested in this control
            if (controlInfo.Value != null)
            {
                // If the next line fails, remember to add that control to the Properties.Settings:
                // Right click on the Project, select Properties, click on General and then click on
                // the link "Create or open application settings"
                Properties.Settings.Default[controlName] = controlInfo.Value;   // Set the setting Value in the Properties.Settings.Default
            }
        }

        // This method loads the settings for all controls in a container
        private void LoadSettings(Control container)
        {
            // Loop through all the controls in the container
            foreach (Control control in container.Controls)
            {
                // Load the setting for the current control
                LoadSetting(control);
                // If the control is a container itself, load the settings for its children recursively
                if (control.HasChildren)
                {
                    LoadSettings(control);
                }
            }
        }

        // This method saves the settings for all controls in a container
        private void SaveSettings(Control container)
        {
            // Loop through all the controls in the container
            foreach (Control control in container.Controls)
            {
                // Save the setting for the current control
                SaveSetting(control);
                // If the control is a container itself, save the settings for its children recursively
                if (control.HasChildren)
                {
                    SaveSettings(control);
                }
            }
        }

        private void chkUseEfficiencyCoresOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (currentProcess == null) return;

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
                        chkUseEfficiencyCoresOnly.Text = "14700K" + chkUseEfficiencyCoresOnly.Text;
                        chkUseEfficiencyCoresOnly.Visible = true;
                    }
                    break;
                case CpuType.Intel_14700K:
                    if (cpuCount == 28)
                    {
                        affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | 1 << 23 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27);
                        chkUseEfficiencyCoresOnly.Text = "14700K" + chkUseEfficiencyCoresOnly.Text;
                        chkUseEfficiencyCoresOnly.Visible = true;
                    }
                    break;
                default:
                    //ignore
                    break;
            }

            if (chkUseEfficiencyCoresOnly.Checked && (affinityMask != IntPtr.Zero))
            {
                try
                {
                    // Set the CPU affinity to Efficient Cores only
                    currentProcess.ProcessorAffinity = affinityMask;
                }
                catch { } // Ignore
            }
        }




        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

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

        private void lblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var processInfo = new ProcessStartInfo($"microsoft-edge:{lblLink.Text}");
            processInfo.UseShellExecute = true;
            Process.Start(processInfo);
        }

        // Importing necessary Win32 API functions
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetPriorityClass(IntPtr hProcess);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        // Constants for process access and priority class
        const uint PROCESS_SET_INFORMATION = 0x0200;
        const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;



        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the process ID of notepad
                Process[] processes = Process.GetProcessesByName("notepad");
                if (processes.Length == 0)
                {
                    Console.WriteLine("Notepad is not running.");
                    return;
                }

                int processId = processes[0].Id;

                // Open the process with the required access
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION|PROCESS_SET_INFORMATION, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to open process. Error: " + GetLastError());
                    return;
                }
                // Set the process priority class
                uint error = 0;
                if (!SetPriorityClass(hProcess, PROCESS_MODE_BACKGROUND_BEGIN))
                {
                    error = GetLastError();
                    Console.WriteLine("Failed to set process priority class. Error: " + error);
                    Console.WriteLine("Error description: " + new System.ComponentModel.Win32Exception((int)error).Message);
                }
                else
                {
                    Console.WriteLine("Process priority class set to background mode.");
                }
                LogError($"Notepad.Id={processId}.  Handle used for SetPriorityClass: {hProcess}", $"GetLastError={error}");

                // Close the process handle
                CloseHandle(hProcess);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

        }
    }
    public class ProgramDetails
    {
        public string FriendlyName { get; set; }
        public string ProcessName { get; set; }
        public string Path { get; set; }
    }

    public class ProgramList
    {
        public List<ProgramDetails> Programs { get; set; }
    }
}

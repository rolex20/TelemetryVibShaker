using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;


namespace RemoteWindowControl
{
    public partial class frmMain : Form
    {
        private Process currentProcess;
        private HttpListener listener; // Web Server for remote control location and focus commands
        private int webServerThreadId;
        private int ExCounter = 0;  // Exceptions Counter
        private string HTML_Template;

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
            } catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr MoveResizeWindow(string ProcessName, int InstanceNumber, int X, int Y, int NewState)
        {
            IntPtr windowHandle = GetHandleByProcessName(ProcessName, InstanceNumber);
            if (windowHandle == IntPtr.Zero) return IntPtr.Zero;

            MoveWindowToCoordinates(windowHandle, X, Y);

            if (NewState == -1)
                MinimizeWindow(windowHandle);
            else if (NewState == 1)
                RestoreWindow(windowHandle);

            return windowHandle;
        }


        private void btnSendChanges_Click(object sender, EventArgs e)
        {
            int newstate = 0;
            if (rbMinimize.Checked)
                newstate = -1;
            else if (rbRestore.Checked)
                newstate = 1;

            MoveResizeWindow(cmbProcesses.Items[cmbProcesses.SelectedIndex].ToString(), (int)nudInstance.Value, Convert.ToInt32(txtXCoord.Text), Convert.ToInt32(txtYCoord.Text), newstate);
        }

        private RECT GetWindowPosition(string ProcessName, int InstanceNumber)
        {
            IntPtr windowHandle = GetHandleByProcessName(ProcessName, InstanceNumber);
            return GetWindowPosition(windowHandle);
        }

        private RECT GetWindowPosition(IntPtr  hWnd)
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

        private void btnReadPosition_Click(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(cmbProcesses.Items[cmbProcesses.SelectedIndex].ToString());
            IntPtr windowHandle = processes[0].MainWindowHandle;

            RECT windowRect = new RECT();
            GetWindowRect(windowHandle, out windowRect);
            int x = windowRect.Left;
            int y = windowRect.Top;

            txtXCoord.Text = x.ToString();
            txtYCoord.Text = y.ToString();
        }

        private void StartWebServer()
        {
            listener = new HttpListener();
            string prefix = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
            listener.Prefixes.Add(prefix);
            //listener.Prefixes.Add("http://perfmon:8080/");
            //listener.Prefixes.Add("http://192.168.1.5:8080/"); // remove this line when debugging
            //listener.Prefixes.Add("http://localhost:8080/");
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                ExCounter++;
                LogError(ex.Message, "StartWebServer{ listener.Start(); }");
            }
            Task.Run(() => ProcessRequest());
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


                string footer = "Ready";
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
                            LogError($"ProcessName={processName}", "ProcessRequest()");

                            string temp;
                            int x = 0;
                            int y = 0;


                            if (parameters.TryGetValue("GetValues", out temp))
                            {
                                LogError("Clicked GetValues for " + processName, "ProcessRequest()");
                                RECT rect = GetWindowPosition(processName, 0);
                                newX = rect.Left;
                                newY = rect.Top;
                                footer = $"{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} Successfully requested coordinates read for {processName}";
                            }
                            else if (parameters.TryGetValue("X", out string sx) && parameters.TryGetValue("Y", out string sy))
                            {
                                x = int.Parse(sx);
                                y = int.Parse(sy);
                                LogError($"X={x}, Y={y}", "ProcessRequest()");


                                if (parameters.TryGetValue("Submit", out temp))
                                {
                                    LogError("Clicked Submit", "ProcessRequest()");
                                    
                                    // Check required new state: Minimized, Restore, NoChange
                                    parameters.TryGetValue("State", out temp);
                                    int newState = int.TryParse(temp, out int parsedValue) ? parsedValue : 0;

                                    procHandle = MoveResizeWindow(processName, 0, x, y, newState);
                                    footer = $"{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} Successfully requested coordinates change to X: {x}, Y: {y}";

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

                string HTML = HTML_Template.Replace("{X}", newX.ToString());
                HTML = HTML.Replace("{Y}", newY.ToString());
                HTML = HTML.Replace("{cmbProcess}", GetHtmlProcessCombo(processName));
                HTML = HTML.Replace("{footer}", footer);

                SendResponse(response, HTML);

            }
        }

        private string GetHtmlProcessCombo(string ProcessName)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<select name='Process' required>");
            for(int i=0; i<cmbProcesses.Items.Count; i++)
            {
                string pname = cmbProcesses.Items[i].ToString();
                string selected = pname.Equals(ProcessName) ? " selected " : String.Empty;
                html.Append($"<option {selected} value='{pname}'>{pname}</option>");
            }
            html.Append("</select>");

            return html.ToString();
        }

        private void MakeFormChanges(int x, int y, string topMost, string focus, string reset)
        {
            this.Invoke(new Action(() =>
            {
                //dispatcherUIThread = (int)GetCurrentThreadId();


                //txtErrors.AppendText($"Moving form to x={x}, y={y}" + Environment.NewLine);

                //if (x >= 0 && y >= 0)
                this.Location = new System.Drawing.Point(x, y);

                // If the user submitted the form with topMost empty, this means the user might want to set this property to false
                this.TopMost = topMost != null;

                if (focus != null) this.Activate();

                if (reset != null) LogError("reset was called", "MakeFormChanges()");

            }));
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
            tabControls.SelectedIndex = 1;

            txtDebug.Tag = 0; // Used to track the number of errors detected

            // Restore previous location
            this.Location = new Point(Properties.Settings.Default.XCoordinate, Properties.Settings.Default.YCoordinate);
            // Load the settings for all controls in the form
            LoadSettings(this);
            ProcessorCheck();

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
            lblLink.Text = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LogError(textBox1.Text, "button1_Click()");
        }

        private void btnStartWebServer_Click(object sender, EventArgs e)
        {
            UpdateLink();

            txtHTML.Enabled = false;
            btnStartWebServer.Enabled = false;
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

            Properties.Settings.Default.XCoordinate = this.Location.X;
            Properties.Settings.Default.YCoordinate = this.Location.Y;

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


            // Open the registry key for the processor
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

            // Read the processor name from the registry
            string processorName = regKey.GetValue("ProcessorNameString").ToString();

            // Check if the processor name contains "Intel 12700K"
            if (processorName.Contains("12700K"))
            {
                chkUseEfficiencyCoresOnly.Visible = true;
                if (chkUseEfficiencyCoresOnly.Checked)
                {

                    // Define the CPU affinity mask for CPUs 17 to 20
                    // CPUs are zero-indexed, so CPU 17 is represented by bit 16, and so on.
                    IntPtr affinityMask = (IntPtr)(1 << 16 | 1 << 17 | 1 << 18 | 1 << 19);

                    // Set the CPU affinity
                    currentProcess.ProcessorAffinity = affinityMask;
                }
            }

            regKey.Close();


        }
    }
}

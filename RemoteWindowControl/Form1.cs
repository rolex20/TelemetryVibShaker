using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace RemoteWindowControl
{
    public partial class frmMain : Form
    {
        private HttpListener listener; // Web Server for remote control location and focus commands
        private int webServerThreadId, dispatcherUIThread;
        private int ExCounter = 0;  // Exceptions Counter

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
            Process[] processes = Process.GetProcessesByName(ProcessName);
            IntPtr windowHandle = processes[Instance].MainWindowHandle;
            return windowHandle;
        }

        private void btnSendChanges_Click(object sender, EventArgs e)
        {

            IntPtr windowHandle = GetHandleByProcessName(cmbProcesses.Items[cmbProcesses.SelectedIndex].ToString(), (int)nudInstance.Value);
            MoveWindowToCoordinates(windowHandle, Convert.ToInt32(txtXCoord.Text), Convert.ToInt32(txtYCoord.Text));

            if (rbMinimize.Checked)
                MinimizeWindow(windowHandle);
            else if (rbRestore.Checked)
                RestoreWindow(windowHandle);

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
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                webServerThreadId = (int)GetCurrentThreadId();
                lblWebServerThreadId.Text = webServerThreadId.ToString();


                if (request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string formData = await reader.ReadToEndAsync();

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

        private void SendResponse(HttpListenerResponse response, string message)
        {

            string marked = (this.TopMost ? "checked" : String.Empty);
            string responseString = $@"
                <html>
                <head><title>Performance Monitor Light - Web Server</title>
                <link rel=""stylesheet"" href=""https://learn.microsoft.com/_themes/docs.theme/master/en-us/_themes/styles/24b6bbbc.site-ltr.css "">
                </head>
                <body leftmargin='10' topmargin='10'>
                    <h1 align='center'>{message}</h1>
                    <table border='1' align='center'><tr><td>
                    <form method='post' align='center'>
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
                    <hr>{DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss]")} WebServerThreadID: {webServerThreadId.ToString()}
                </td</tr><table>
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


        private void frmMain_Load(object sender, EventArgs e)
        {
            StartWebServer();
        }

        private void txtIPAddress_TextChanged(object sender, EventArgs e)
        {
            lblLink.Text = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            lblLink.Text = $"http://{txtIPAddress.Text}:{txtPort.Text}/";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LogError(textBox1.Text, "button1_Click()");
        }
    }
}

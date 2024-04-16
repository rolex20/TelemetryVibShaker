using System.Net.Sockets;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using TelemetryVibShaker;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text;
using System.Windows.Forms;


namespace SimConnectExporter
{
    public partial class frmMain : Form
    {
        private UdpClient udpSender;
        private byte[] datagram;

        private SimConnect simconnect = null;
        private const int WM_USER_SIMCONNECT = 0x0402;

        private string CurrentAircraftName;

        enum DEFINITIONS
        {
            Struct1,
        }

        enum REQUESTS
        {
            Request1,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title; // Aircraft aircraftName
            public double trueAirspeed; // True airspeed in knots
            public double flaps; // Flaps position
            public double spoilers; // Airbrakes / Spoilers position
            public double angleOfAttack; // Angle of Attack in degrees
        };

        private void ConnectToSimConnect()
        {
            try
            {
                simconnect = new SimConnect("MSFS2020 SimConnect WinForms", this.Handle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(Simconnect_OnRecvSimobjectData);

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                //simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Airspeed True", "meter per second", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Trailing Edge Flaps Left Percent", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Spoilers Handle Position", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Angle of Attack", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);
                simconnect.RequestDataOnSimObject(REQUESTS.Request1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch (COMException ex)
            {
                MessageBox.Show("Unable to connect to MSFS2020: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (simconnect != null)
                {
                    simconnect.ReceiveMessage();
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect connection established");
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("SimConnect connection closed");
            DisconnectFromSimConnect();
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUESTS.Request1)
            {
                Struct1 sd = (Struct1)data.dwData[0];

                /*** Send Datagram ***/
                string aircraftName = sd.title;
                if (CurrentAircraftName.Equals(aircraftName))// send datagram
                {
                    datagram[0] = (byte)sd.angleOfAttack;
                    datagram[1] = (byte)sd.spoilers;
                    datagram[2] = (byte)sd.flaps;
                    datagram[3] = (byte)(sd.trueAirspeed / 10.0d); // Speed must be sent in decameters per second
                    udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
                }
                else
                {// send aircraft name
                    CurrentAircraftName = aircraftName;
                    byte[] sendBytes = Encoding.ASCII.GetBytes(CurrentAircraftName);
                    udpSender.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSender);
                }


                /*** Update UI ***/
                if (chkShowStatistics.Checked && tabs.SelectedIndex == 1)
                {
                    UpdateValue(lblTimestamp, Environment.TickCount64);
                    UpdateValue(lblCurrentUnitType, sd.title);
                    UpdateValue(lblSpeed, (int)sd.trueAirspeed);
                    UpdateValue(lblLastFlaps, (int)sd.flaps);
                    UpdateValue(lblLastSpeedBrakes, (int)sd.spoilers);
                }

            }
        }

        private void UpdateValue(Label L, int value)
        {
            if (Convert.ToInt32(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateValue(Label L, long value)
        {
            if (Convert.ToInt64(L.Tag) != value)
            {
                L.Tag = value;
                L.Text = (value >= 0) ? value.ToString() : "---"; // if value<0 then it is not valid or applicable yet
            }
        }

        private void UpdateValue(Label L, string value)
        {
            if (!L.Tag.Equals(value))
            {
                L.Tag = value;
                L.Text = value;
            }
        }




        private void DisconnectFromSimConnect()
        {
            if (simconnect != null)
            {
                simconnect.Dispose();
                simconnect = null;
            }
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }



        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectUDP();
            DisconnectFromSimConnect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectUDP();
            DisconnectFromSimConnect();
        }

        private void DisconnectUDP()
        {
            if (udpSender != null)
            {
                udpSender.Dispose();
                udpSender = null;
            }

        }

        private void ConnectUDP()
        {
            udpSender = new UdpClient();
            udpSender.Connect(txtDestinationHostname.Text, Convert.ToInt32(txtDestinationPort.Text));
        }

        // send the message
        // the destination is defined by the call to .Connect()
        // for now, lets ignore the connected field.  anyways this fails if not connected
        public void SendTelemtryDatagram()
        {

            udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
        }

        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            tsStatusBar1.Text = "Trying to connect";
            CurrentAircraftName = "";
            ConnectUDP();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            datagram = new byte[4];
            // Load the settings for all controls in the form
            LoadSettings(this);
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

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.XCoordinate = this.Location.X;
            Properties.Settings.Default.YCoordinate = this.Location.Y;

            // Save the settings for all controls in the form
            SaveSettings(this);

            // SaveSettings() is recursive so calling Save() below
            Properties.Settings.Default.Save();

        }
    }
}

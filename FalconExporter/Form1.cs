//using F4TexSharedMem;
using F4SharedMem.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FalconExporter
{
    public partial class frmMain : Form
    {
        private Reader _sharedMemReader = new Reader();
        private FlightData _lastFlightData;

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();


        public frmMain()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tsTimestamp.Text = GetTickCount64().ToString();

            if (ReadSharedMem() != null)
            {
                BindSharedMemoryDataToFormElements();
                tsStatus.Text = "Connected";
                
            }
            else
            {
                tsStatus.Text = "Not Connected";
            }
        }

        private FlightData ReadSharedMem()
        {
            return _lastFlightData = _sharedMemReader.GetCurrentData();
        }

        private void BindSharedMemoryDataToFormElements()
        {
                BindFDVarsToFormElements();
        }

        private void BindFDVarsToFormElements()
        {
            this.SuspendLayout();
            lblAoA.Text = _lastFlightData.alpha.ToString();
            lblSpeed.Text = _lastFlightData.kias.ToString();
            lblAltitude.Text = _lastFlightData.aauz.ToString();
            lblRALT.Text = _lastFlightData.RALT.ToString();
            lblSpeedBrakes.Text = _lastFlightData.speedBrake.ToString();
            lblTrueAirspeed.Text = (_lastFlightData.vt * 0.592484f).ToString();
            lblGForces.Text = _lastFlightData.gs.ToString();
            lblTotalStates.Text = _lastFlightData.totalStates.ToString();
            lblVehicleType.Text = _lastFlightData.vehicleACD.ToString();
            lblFuel.Text = (_lastFlightData.internalFuel + _lastFlightData.externalFuel).ToString();
            this.ResumeLayout();
        }

    }
}

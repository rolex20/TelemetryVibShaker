﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    internal class TelemetryData
    {
        public int AoA; // 0 to 255
        public int SpeedBrakes; // 0 to 100 percent
        public int Flaps; // 0 to 100 percent
        public int Speed; // true AirSpeed, km/h
        public int GForces; // G forces
        public int Altitude; // Altitude above the ground in meters
        public int Gear; // Gear deployed, 0 to 100 percent

        //public int DatagramLength; 

        public TelemetryData()
        {
            // Flag that no values have been received yet
            AoA = -1;
            SpeedBrakes = -1;
            Flaps = -1;
            Speed = 0;
            GForces = 0;
            Altitude = 0;
            Gear = 0;
            //DatagramLength = 0;
        }
    }
}

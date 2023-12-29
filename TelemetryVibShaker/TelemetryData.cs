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

        public TelemetryData()
        {
            // Flag that no values have been received yet
            AoA = -1;
            SpeedBrakes = -1;
            Flaps = -1;

        }
    }
}

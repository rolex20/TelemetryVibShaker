using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    /// <summary>
    // Manages effects for small vibration motors
    // conected to arduino microcontrollers
    // Communication with arduino-like controllers is via UDP
    // My scenario:
    // The first microcontroller one is an arduino connected to two vibration motors
    // The vibration motors are attached to my Throttle and Joystick in a location where the vibration is felt but not touched with my hand
    // The second is a LILIGO T-Watch 2020-V3 watch with an integrated vibration motor
    /// </summary>
    internal class MotorManager
    {
        public MotorManager(string Controller1IP, string Controller2IP) {
        
        }

        public void UpdateEffect(int AoA, int SpeedBrakes, int Flaps)
        {

        }
    }
}

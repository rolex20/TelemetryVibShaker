﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    internal class MotorStrengthPoints
    {
        private const int TFT_BLACK = 0;
        private const int TFT_YELLOW = 1;
        private const int TFT_DARKGREEN = 2;
        private const int TFT_GREEN = 3;
        private const int TFT_RED = 4;

        //See diagram in: https://github.com/rolex20/TelemetryVibShaker/blob/master/TelemetryVibShaker/Points%20for%20Effects%20-%20Diagram.png
        //If effect is AoA then x2=AoA1, x3=AoA2
        public float x2, x3, x4, x5;
        public float y2, y3, y4, y5;
        private float m1; // cached slope of the equation of the straight line between the two points: x2,y2 and x3,y3
        private float m2; // cached slope of the equation of the straight line between the two points: x4, y4 and x5, y5

        public MotorStrengthPoints(float X2, float Y2, float X3, float Y3, float X4, float Y4, float X5, float Y5)
        {
            x2 = X2;
            x3 = X3;
            x4 = X4;
            x5 = X5;

            y2 = Y2;
            y3 = Y3;
            y4 = Y4;
            y5 = Y5;

            // calculate cached slopes
            m1 = SolveSlope(x2, y2, x3, y3);
            m2 = SolveSlope(x4, y4, x5, y5);

        }

        /// <summary>
        /// Calculates output to send to a vibration motor
        /// If telemetry is between x2 and x3 it will use linear interpolation from the two points x2,y2 and x3,y3
        /// If telemetry is less than x2, the return value will be 0
        /// If telemetry is larger than x3 it will use linear interpolation from the two points x4,y4 and x5,y5
        /// </summary>
        /// <param name="telemetry"></param>
        /// <returns></returns>
        public int CalculateOutput(int telemetry)
        {
            if (telemetry < x2)
                return 0;
            else if (telemetry >= x2 && telemetry <= x3)
                return Seg1SolveY(telemetry);
            else
                return Seg2SolveY(telemetry);
        }

        /// <summary>
        /// Return a color code for the TWatch based on telemetry
        /// For now the color is fixed to Yellow (<x2), Green (x2-x3), Red (>x3)
        /// </summary>
        /// <param name="telemetry">telemetry data</param>
        /// <returns></returns>
        public int GetBackgroundColor(int telemetry)
        {
            // For now the effectType is fixed to BLACK, YELLOW, GREEN, RED
            if (telemetry == 0)
                return TFT_BLACK;
            else if (telemetry < x2)
                return TFT_YELLOW;
            else if (telemetry <= x3)
                return TFT_DARKGREEN;
            else
                return TFT_RED;
        }



        /// <summary>
        /// Solves Y for the straight line in Segment 1: AoA
        /// It uses the equation of the line that passes between two points
        /// It uses a cached pre calculated value for the slope m
        /// And Segment 1 is defined between points x2,y2 and x3, y3
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>y</returns>
        private int Seg1SolveY(float x)
        {
            // y = mx - mx1 + y1
            if (x2 == 360.0f) return 0; // Disable effects by sending 0 to the motors

            float y = m1 * x - m1 * x2 + y2;
            return (int) (y);
        }

        /// <summary>
        /// Solves Y for the straight line in Segment 1
        /// It uses the equation of the line that passes between two points
        /// It uses a cached pre calculated value for the slope m
        /// And Segment 1 is defined between points x2,y2 and x3, y3
        /// </summary>
        /// <param name="X">x</param>
        /// <returns>y</returns>
        private int Seg2SolveY(float X)
        {
            // y = mx - mx1 + y1

            float y = m2 * X - m2 * x4 + y4;
            return (int)(y);
        }

        private float SolveSlope(float X1, float Y1, float X2, float Y2)
        {
            // m = (y1 - y2) / (x1 - x2)
            return (Y1-Y2)/(X1-X2);
        } 

        public void ChangeAoARange(float AoA1, float AoA2)
        {
            x2 = AoA1;
            x3 = AoA2;
            x4 = x3 + 1; // reassign this value based on the new x3

            // recalculate cached slopes
            m1 = SolveSlope(x2, y2, x3, y3);
            m2 = SolveSlope(x4, y4, x5, y5);
        }
    }
}


namespace TelemetryVibShaker
{
    internal class EffectDefinition
    {
        public bool Enabled;
        private MotorStrengthPoints points;
        private VibrationEffectType effectType;
        public VibrationEffectType EffectType { get { return effectType; } }

        public EffectDefinition(VibrationEffectType Effect, MotorStrengthPoints Points, bool Enabled)
        {
            this.Enabled = Enabled;
            effectType = Effect;
            points = Points;
        }

        public void UpdatePoints(MotorStrengthPoints Points)
        {
            points = Points;
        }


        public void ChangeAoARange(int AoA1, int AoA2)
        {
            if (effectType == VibrationEffectType.AoA || effectType == VibrationEffectType.BackgroundAoA) points.ChangeAoARange(AoA1, AoA2);
        }


        // Calculates the output (0..255) to send to a motor or a color (0..4) depending on the effectType
        public int CalculateOutput(TelemetryData Telemetry)
        {
            if (!Enabled) return 0;

            // Using a strategy pattern here only obscured the code
            // The code below is simpler to understand
            switch (effectType)
            {
                case VibrationEffectType.AoA:
                    return points.CalculateOutput(Telemetry.AoA);
                case VibrationEffectType.SpeedBrakes:
                    return points.CalculateOutput(Telemetry.SpeedBrakes);
                case VibrationEffectType.Flaps:
                    return points.CalculateOutput(Telemetry.Flaps);
                case VibrationEffectType.BackgroundAoA:                   
                    return points.GetBackgroundColor(Telemetry.AoA);
                case VibrationEffectType.BackgroundSpeedBrakes:
                    return points.GetBackgroundColor(Telemetry.SpeedBrakes);
                case VibrationEffectType.BackgroundFlaps:
                    return points.GetBackgroundColor(Telemetry.Flaps);
                case VibrationEffectType.Gear:
                    return points.GetBackgroundColor(Telemetry.Gear);
                case VibrationEffectType.Nothing:
                    return 0;
                default:
                    throw new Exception("VibrationEffectType type not implemented in EffectDefinition.CalculateOutput().");
                    //return 0;  // Just in case somebody else caughts/ignores this exception
            } //switch


        } // CalculateOutput




    }
}

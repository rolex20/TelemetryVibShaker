
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

            switch (effectType)
            {
                case VibrationEffectType.AoA:
                    return points.CalculateOutput(Telemetry.AoA);
                    break;// just in case I add something else later
                case VibrationEffectType.SpeedBrakes:
                    return points.CalculateOutput(Telemetry.SpeedBrakes);
                    break;
                case VibrationEffectType.Flaps:
                    return points.CalculateOutput(Telemetry.Flaps);
                    break;
                case VibrationEffectType.BackgroundAoA:                   
                    return points.GetBackgroundColor(Telemetry.AoA);

                    break;
                case VibrationEffectType.BackgroundSpeedBrakes:
                    return points.GetBackgroundColor(Telemetry.SpeedBrakes);
                    break;

                case VibrationEffectType.BackgroundFlaps:
                    return points.GetBackgroundColor(Telemetry.Flaps);
                    break;
                case VibrationEffectType.Nothing:
                    return 0;
                    break;
                case VibrationEffectType.Gear:
                    return points.CalculateOutput(Telemetry.Gear);
                default:
                    throw new Exception("VibrationEffectType type not implemented in EffectDefinition.CalculateOutput().");
                    return 0;
                    break;
            } //switch


        } // CalculateOutput




    }
}

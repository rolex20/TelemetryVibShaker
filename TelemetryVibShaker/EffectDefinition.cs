
namespace TelemetryVibShaker
{
    internal class EffectDefinition
    {
        private const int TFT_BLACK = 0;
        private const int TFT_YELLOW = 1;
        private const int TFT_DARKGREEN = 2;
        private const int TFT_GREEN = 3;
        private const int TFT_RED = 4;

        MotorStrengthPoints points;
        VibrationEffectType effectType;
        public VibrationEffectType EffectType { get { return effectType; } }

        public EffectDefinition(VibrationEffectType Effect, MotorStrengthPoints Points)
        {
            effectType = Effect;
            points = Points;
        }       


        public void ChangeAoARange(int AoA1, int AoA2)
        {           
            points.ChangeAoARange(AoA1, AoA2);
        }


        // provides the output to send to a motor or a color
        public int CalculateOutput(TelemetryData Telemetry)
        {

            switch (effectType)
            {
                case VibrationEffectType.AoA:
                    return points.Seg1SolveY(Telemetry.AoA);
                    break;// just in case I add something else later
                case VibrationEffectType.SpeedBrakes:
                    return points.Seg2SolveY(Telemetry.SpeedBrakes);
                    break;
                case VibrationEffectType.Flaps:
                    return points.Seg2SolveY(Telemetry.Flaps);
                    break;
                case VibrationEffectType.BackgroundAoA:
                    // For now the effectType is fixed
                    return GetBackgroundColor(Telemetry.AoA);

                    break;
                case VibrationEffectType.BackgroundSpeedBrakes:
                    // For now the effectType is fixed
                    return GetBackgroundColor(Telemetry.SpeedBrakes);
                    break;

                case VibrationEffectType.BackgroundFlaps:
                    // For now the effectType is fixed
                    return GetBackgroundColor(Telemetry.Flaps);
                    break;
                case VibrationEffectType.Nothing:
                    return 0;
                    break;
                default:
                    return 0;
                    break;
            } //switch


        } // CalculateOutput

        private int GetBackgroundColor(int telemetry)
        {
            // For now the effectType is fixed to BLACK, YELLOW, GREEN, RED
            if (telemetry == 0) return TFT_BLACK;
            else if (telemetry <= points.x2) return TFT_YELLOW;
            else if (telemetry <= points.x3) return TFT_DARKGREEN;
            else return TFT_RED;
        }


    }
}

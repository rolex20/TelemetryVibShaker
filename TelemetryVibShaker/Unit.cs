namespace TelemetryVibShaker
{
    internal class Unit
    {
        public string typeName { get; set; }  // Name of the aircraft
        public float AoA1 { get; set; } // Initial AoA optimal range
        public float AoA2 { get; set; } // Upper limit for AoA optimal range
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    public enum SoundEffectStatus
    {
        Invalid,
        NotPlaying,
        Ready,
        Playing, // 1 or 2
        Canceled, // 1 and 2
    }

}

using System;

namespace SkyRacers
{
    public static class HudTape
    {
        public const int Rows = 13;
        public static float TickValue(float value, float step, int row)
            => ((float)Math.Floor(value / step) + Rows / 2 - row) * step;
        // Increasing readings move the scale down past the fixed centre index.
        public static float TickY(float value, float tick, float step)
            => 180 + (value - tick) * 40 / step;
    }
}

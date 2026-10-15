#nullable enable
using System;

namespace StartupLife.Presentation
{
    // Pure, engine-free dynamic Vietnamese typography policy.
    public static class CharacterCreatorTextScalePolicy
    {
        public static float Multiplier(int step)
        {
            switch (step)
            {
                case 0: return 1f;
                case 1: return 1.5f;
                case 2: return 2f;
                default: throw new ArgumentOutOfRangeException(nameof(step));
            }
        }

        public static int ClampStep(int value) => value < 0 ? 0 : (value > 2 ? 2 : value);

        public static float ControlHeight(float touchFloor, float baseBodyFont, float multiplier)
        {
            if (float.IsNaN(touchFloor) || float.IsInfinity(touchFloor) ||
                touchFloor <= 0 || float.IsNaN(baseBodyFont) ||
                float.IsInfinity(baseBodyFont) || baseBodyFont <= 0 ||
                float.IsNaN(multiplier) || multiplier < 1f || multiplier > 2f)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            return (float)Math.Ceiling(Math.Max(touchFloor,
                baseBodyFont * multiplier * 2f + 24f));
        }
    }
}

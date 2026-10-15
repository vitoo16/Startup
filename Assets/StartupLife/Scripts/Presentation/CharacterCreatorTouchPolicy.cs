#nullable enable
using System;

namespace StartupLife.Presentation
{
    /// <summary>Canvas units for a minimum mobile touch target.
    /// Screen.dpi is not universally reliable: when unavailable, estimate from
    /// a conservative 360-pixel phone reference, then require real-device QA.
    /// This calculation does not guarantee a physical millimetre measurement.</summary>
    public static class CharacterCreatorTouchPolicy
    {
        public const float MinimumTouchDp = 48f;
        public const float ReferenceDpi = 160f;
        public const float ReferencePhonePixelWidth = 360f;

        public static float MinimumCanvasUnits(float canvasScale, float screenDpi,
            int screenPixelWidth, float authoredMinimumUnits)
        {
            if (float.IsNaN(canvasScale) || float.IsInfinity(canvasScale) ||
                canvasScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(canvasScale));
            if (screenPixelWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenPixelWidth));
            if (float.IsNaN(authoredMinimumUnits) || float.IsInfinity(authoredMinimumUnits) ||
                authoredMinimumUnits <= 0f)
                throw new ArgumentOutOfRangeException(nameof(authoredMinimumUnits));

            var reliableDpi = !float.IsNaN(screenDpi) && !float.IsInfinity(screenDpi) &&
                screenDpi >= 96f && screenDpi <= 1000f;
            var density = reliableDpi
                ? screenDpi / ReferenceDpi
                : (float)Math.Min(3.5, Math.Max(1.0,
                    screenPixelWidth / (double)ReferencePhonePixelWidth));
            var physicalPixels = MinimumTouchDp * density;
            return (float)Math.Ceiling(Math.Max(authoredMinimumUnits,
                physicalPixels / canvasScale));
        }
    }
}

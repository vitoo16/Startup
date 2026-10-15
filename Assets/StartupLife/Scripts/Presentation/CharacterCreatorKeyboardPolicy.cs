#nullable enable
using System;

namespace StartupLife.Presentation
{
    // Pure geometry: subtract only the keyboard overlap with the safe area.
    // A screen-pixel keyboard inset becomes a CanvasScaler-local offset.
    public static class CharacterCreatorKeyboardPolicy
    {
        public static float BottomInsetCanvasUnits(int screenHeightPixels, float safeBottomPixels,
            float safeHeightPixels, float keyboardTopPixels, float canvasScale,
            float reservedVisibleCanvasHeight)
        {
            if (screenHeightPixels <= 0 || float.IsNaN(canvasScale) ||
                float.IsInfinity(canvasScale) || canvasScale <= 0f ||
                float.IsNaN(safeBottomPixels) || safeBottomPixels < 0f ||
                float.IsNaN(safeHeightPixels) || safeHeightPixels <= 0f ||
                float.IsNaN(keyboardTopPixels) || float.IsInfinity(keyboardTopPixels) ||
                float.IsNaN(reservedVisibleCanvasHeight) ||
                reservedVisibleCanvasHeight < 0f)
                throw new ArgumentOutOfRangeException(nameof(canvasScale));

            var top = (float)Math.Max(0, Math.Min(screenHeightPixels, keyboardTopPixels));
            var coverage = Math.Max(0f, top - safeBottomPixels);
            var available = Math.Max(0f, safeHeightPixels - reservedVisibleCanvasHeight * canvasScale);
            return (float)(Math.Min(coverage, available) / canvasScale);
        }
    }
}

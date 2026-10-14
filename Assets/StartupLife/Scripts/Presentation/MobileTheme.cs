using UnityEngine;

namespace StartupLife.Presentation
{
    // Builder-only fallback palette, kept in sync with authoritative MobileTheme.asset.
    // Engine-free Golden art validator checks byte-exact Color32 parity.
    public sealed class MobileTheme : ScriptableObject
    {
        public Color background = new Color32(246, 235, 212, 255);
        public Color panel = new Color32(255, 246, 228, 255);
        public Color ink = new Color32(39, 72, 60, 255);
        public Color accent = new Color32(184, 92, 56, 255);
        public float title = 64, heading = 44, body = 36, caption = 28;
        public int gap = 24, padding = 48, touchTarget = 96;
    }
}

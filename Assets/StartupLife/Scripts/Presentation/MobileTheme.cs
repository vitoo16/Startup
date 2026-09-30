using UnityEngine;

namespace StartupLife.Presentation
{
    // Exploratory baseline palette. Golden art approval will lock the production palette.
    public sealed class MobileTheme : ScriptableObject
    {
        public Color background = new Color32(242, 235, 222, 255);
        public Color panel = new Color32(255, 251, 242, 255);
        public Color ink = new Color32(45, 61, 57, 255);
        public Color accent = new Color32(58, 108, 88, 255);
        public float title = 64, heading = 44, body = 36, caption = 28;
        public int gap = 24, padding = 48, touchTarget = 96;
    }
}

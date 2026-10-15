#nullable disable
using System;
using TMPro;
using UnityEngine;

namespace StartupLife.Presentation
{
    // Scoped to Character Creator; player choices are UI-only, never save rules.
    public sealed class CharacterCreatorTextScaler : MonoBehaviour
    {
        [SerializeField] private CharacterCreatorTouchSizer touchSizer;
        [SerializeField] private TMP_Text scaleValue;
        private TMP_Text[] labels = Array.Empty<TMP_Text>();
        private float[] originalSizes = Array.Empty<float>();
        private bool initialized;
        private int step;

        private void OnEnable() { Cache(); ApplyScale(); }
        public void IncreaseTextScale() => SetStep(step + 1);
        public void DecreaseTextScale() => SetStep(step - 1);

        public void SetStep(int requested)
        {
            var clamped = CharacterCreatorTextScalePolicy.ClampStep(requested);
            if (initialized && clamped == step) return;
            step = clamped;
            Cache();
            ApplyScale();
        }

        private void Cache()
        {
            if (initialized) return;
            labels = GetComponentsInChildren<TMP_Text>(true);
            originalSizes = new float[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (!label) continue;
                originalSizes[i] = label.fontSize;
                // Respect TMP intrinsic preferred heights for wrapped Vietnamese
                // labels. Button/input text remains stretched in its own control.
                if (label.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                    label.GetComponentInParent<TMP_InputField>() != null) continue;
                var element = label.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element) element.preferredHeight = -1f;
            }
            initialized = true;
        }

        private void ApplyScale()
        {
            var factor = CharacterCreatorTextScalePolicy.Multiplier(step);
            for (var i = 0; i < labels.Length; i++)
            {
                if (!labels[i]) continue;
                var size = originalSizes[i] * factor;
                if (Math.Abs(labels[i].fontSize - size) > .01f)
                    labels[i].fontSize = size;
            }
            if (scaleValue)
            {
                var value = (int)(factor * 100f);
                var label = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
                if (scaleValue.text != label) scaleValue.text = label;
            }
            if (touchSizer) touchSizer.SetTypographyMultiplier(factor);
        }
    }
}

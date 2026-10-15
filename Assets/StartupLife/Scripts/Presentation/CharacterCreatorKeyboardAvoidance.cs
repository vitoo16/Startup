#nullable disable
using System;
using TMPro;
using UnityEngine;

namespace StartupLife.Presentation
{
    // No gameplay/input mutation; panel is moved only while the NAME keyboard
    // is visible. The original safe-area RectTransform offset is restored.
    public sealed class CharacterCreatorKeyboardAvoidance : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Canvas rootCanvas;
        private RectTransform panel;
        private float originalBottom;
        private float previousInset = -1f;

        private void OnEnable()
        {
            panel = GetComponent<RectTransform>();
            if (panel != null) originalBottom = panel.offsetMin.y;
            ApplyInset(0f);
        }

        private void LateUpdate()
        {
            if (!panel || !rootCanvas || !nameInput || !nameInput.isFocused)
            {
                ApplyInset(0f);
                return;
            }

            if (!TouchScreenKeyboard.visible)
            {
                ApplyInset(0f);
                return;
            }
            var keyboard = TouchScreenKeyboard.area;
            // Some Android IMEs return an empty area while visible. Fall back
            // to an estimated overlay, flagged for physical-device QA.
            var top = keyboard.height > 0f ? keyboard.yMax : Screen.height * .38f;
            var inset = CharacterCreatorKeyboardPolicy.BottomInsetCanvasUnits(
                Screen.height, Screen.safeArea.yMin, Screen.safeArea.height, top,
                rootCanvas.scaleFactor, 560f);
            ApplyInset(inset);
        }

        private void OnDisable() { ApplyInset(0f); }

        private void ApplyInset(float inset)
        {
            if (!panel || Math.Abs(previousInset - inset) < .01f) return;
            var original = panel.offsetMin;
            panel.offsetMin = new Vector2(original.x, originalBottom + inset);
            previousInset = inset;
        }
    }
}

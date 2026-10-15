#nullable disable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StartupLife.Presentation
{
    /// <summary>
    /// Enforces a conservative tap-height floor on the creator's *authored*
    /// LayoutElements. Changes on canvas resize, not every Update frame.
    /// Does not change fonts or pretend to validate 200% text reflow.
    /// </summary>
    public sealed class CharacterCreatorTouchSizer : MonoBehaviour
    {
        [SerializeField] private MobileTheme uiTheme;
        private Canvas hostCanvas;
        private UnityEngine.UI.LayoutElement[] targets =
            Array.Empty<UnityEngine.UI.LayoutElement>();
        private float lastAppliedHeight = -1f;
        private float typographyMultiplier = 1f;

        public void SetTypographyMultiplier(float multiplier)
        {
            // Reject invalid input before dirtying any Canvas/layout state.
            CharacterCreatorTextScalePolicy.Multiplier(
                multiplier == 1f ? 0 : multiplier == 1.5f ? 1 :
                multiplier == 2f ? 2 : -1);
            if (Math.Abs(typographyMultiplier - multiplier) < .01f) return;
            typographyMultiplier = multiplier;
            Refresh();
        }

        private void OnEnable() => RefreshAuthoredBindings();

        // Used once by the Editor migration *after* new accessibility
        // buttons have been appended to a previously authored scene.
        public void RefreshAuthoredBindings()
        {
            CacheTargets();
            Refresh();
        }

        private void OnCanvasHierarchyChanged()
        {
            hostCanvas = null;
            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Refresh();
        }

        private void CacheTargets()
        {
            var collected = new HashSet<UnityEngine.UI.LayoutElement>();
            foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var layout = button.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout) collected.Add(layout);
            }
            foreach (var input in GetComponentsInChildren<TMP_InputField>(true))
            {
                var layout = input.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout) collected.Add(layout);
            }
            foreach (var row in GetComponentsInChildren<UnityEngine.UI.HorizontalLayoutGroup>(true))
            {
                var layout = row.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout) collected.Add(layout);
            }
            targets = new UnityEngine.UI.LayoutElement[collected.Count];
            collected.CopyTo(targets);
            lastAppliedHeight = -1f;
        }

        private void Refresh()
        {
            if (!uiTheme || targets.Length == 0) return;
            if (!hostCanvas) hostCanvas = GetComponentInParent<Canvas>();
            if (!hostCanvas || hostCanvas.scaleFactor <= 0f || Screen.width <= 0) return;

            var densityMinimum = CharacterCreatorTouchPolicy.MinimumCanvasUnits(
                hostCanvas.scaleFactor, Screen.dpi, Screen.width, uiTheme.touchTarget);
            var target = CharacterCreatorTextScalePolicy.ControlHeight(densityMinimum,
                uiTheme.body, typographyMultiplier);
            if (Math.Abs(target - lastAppliedHeight) < 0.01f) return;
            // Store before modifying geometry: Unity may call this method
            // recursively while layout dimensions propagate.
            lastAppliedHeight = target;
            foreach (var element in targets)
            {
                if (!element) continue;
                if (element.minHeight != target) element.minHeight = target;
                if (element.preferredHeight != target) element.preferredHeight = target;
            }
        }
    }
}

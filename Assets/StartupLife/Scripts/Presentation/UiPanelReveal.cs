using System.Collections;
using UnityEngine;

namespace StartupLife.Presentation
{
    /// <summary>
    /// Reusable unscaled-time fade for uGUI panels. No layout transforms or
    /// fractional sprite movement: native pixel art remains pixel-snapped.
    /// Presentation only; never alters gameplay time, saves or UI navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiPanelReveal : MonoBehaviour
    {
        private const float DurationSeconds = 0.18f;
        // Shared with the eventual accessibility Settings surface.
        public const string ReducedMotionPreference = "startup-life.ui.reduce-motion.v1";
        private CanvasGroup canvasGroup;
        private Coroutine running;

        public static void Show(GameObject target)
        {
            if (!target) return;
            if (!target.activeSelf) target.SetActive(true);
            if (!target.activeInHierarchy) return;

            var reveal = target.GetComponent<UiPanelReveal>();
            if (!reveal) reveal = target.AddComponent<UiPanelReveal>();
            reveal.Play();
        }

        private void Play()
        {
            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            if (PlayerPrefs.GetInt(ReducedMotionPreference, 0) != 0 ||
                !Application.isPlaying)
            {
                canvasGroup.alpha = 1f;
                return;
            }

            canvasGroup.alpha = 0f;
            running = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            var elapsed = 0f;
            while (elapsed < DurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / DurationSeconds);
                // Ease-out without transform scaling or sub-pixel translation.
                canvasGroup.alpha = 1f - (1f - t) * (1f - t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            running = null;
        }

        private void OnDisable()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }
            if (canvasGroup) canvasGroup.alpha = 1f;
        }
    }
}

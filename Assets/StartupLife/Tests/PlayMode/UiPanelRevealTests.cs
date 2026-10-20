using System.Collections;
using NUnit.Framework;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    public sealed class UiPanelRevealTests
    {
        [UnityTest]
        public IEnumerator NewPanelBecomesVisibleAndSettlesWithoutMovingLayout()
        {
            var panel = new GameObject("TestUiReveal", typeof(RectTransform));
            panel.SetActive(false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(4f, 12f);
            var originalPosition = rect.anchoredPosition;
            var hadReducedMotion = PlayerPrefs.HasKey(UiPanelReveal.ReducedMotionPreference);
            var previous = PlayerPrefs.GetInt(UiPanelReveal.ReducedMotionPreference, 0);
            try
            {
                PlayerPrefs.SetInt(UiPanelReveal.ReducedMotionPreference, 0);
                UiPanelReveal.Show(panel);
                Assert.That(panel.activeSelf, Is.True);
                var group = panel.GetComponent<CanvasGroup>();
                Assert.That(group, Is.Not.Null);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(group.alpha, Is.EqualTo(1f).Within(0.001f));
                Assert.That(rect.anchoredPosition, Is.EqualTo(originalPosition),
                    "uGUI reveal must not slide pixel-snapped layouts.");
                panel.SetActive(false);
                Assert.That(group.alpha, Is.EqualTo(1f));
            }
            finally
            {
                if (hadReducedMotion)
                    PlayerPrefs.SetInt(UiPanelReveal.ReducedMotionPreference, previous);
                else
                    PlayerPrefs.DeleteKey(UiPanelReveal.ReducedMotionPreference);
                Object.Destroy(panel);
            }
        }

        [Test]
        public void ReducedMotionShowsWithoutTween()
        {
            var panel = new GameObject("TestReducedUiReveal", typeof(RectTransform));
            var hadSetting = PlayerPrefs.HasKey(UiPanelReveal.ReducedMotionPreference);
            var previous = PlayerPrefs.GetInt(UiPanelReveal.ReducedMotionPreference, 0);
            try
            {
                PlayerPrefs.SetInt(UiPanelReveal.ReducedMotionPreference, 1);
                UiPanelReveal.Show(panel);
                Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            }
            finally
            {
                if (hadSetting)
                    PlayerPrefs.SetInt(UiPanelReveal.ReducedMotionPreference, previous);
                else
                    PlayerPrefs.DeleteKey(UiPanelReveal.ReducedMotionPreference);
                Object.DestroyImmediate(panel);
            }
        }
    }
}

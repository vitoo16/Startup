using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using StartupLife.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StartupLife.Tests.PlayMode
{
    /// <summary>
    /// Real Editor/Player resize matrix. Refuses to certify unsupported
    /// resolutions; never deletes or reinitializes the player's save.
    /// </summary>
    public sealed class FirstPlayableMainMenuResponsiveTests
    {
        private static readonly Vector2Int[] PortraitSizes =
        {
            new Vector2Int(360, 640),
            new Vector2Int(390, 844),
            new Vector2Int(430, 932)
        };

        private static readonly string[] Pages =
        {
            "StartupMainMenuHome",
            "StartupMainMenuSettings",
            "StartupMainMenuNewGameConfirmation"
        };

        [UnityTest]
        public IEnumerator MenuHasUnclippedScrollablePagesAtRealPortraitResolutions()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            var originalFullscreen = Screen.fullScreen;
            var evidence = Environment.GetEnvironmentVariable(
                "STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE");

            try
            {
                var loading = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
                while (!loading.isDone) yield return null;
                yield return LocalizationSettings.InitializationOperation;
                yield return null;

                var bootstrap = UnityEngine.Object.FindAnyObjectByType<StartupLifeBootstrapper>();
                var safeArea = UnityEngine.Object.FindAnyObjectByType<MobileSafeArea>();
                Assert.That(bootstrap && bootstrap.IsReady, Is.True);
                Assert.That(safeArea, Is.Not.Null);
                var startRevision = bootstrap.Snapshot.Revision;
                var startRunId = bootstrap.Snapshot.RunId;
                var entry = GameObject.Find("StartupMainMenuOpenButton")?.GetComponent<Button>();
                Assert.That(entry, Is.Not.Null);
                entry.onClick.Invoke();
                var overlay = GameObject.Find("StartupMainMenuOverlay");
                Assert.That(overlay, Is.Not.Null);

                foreach (var size in PortraitSizes)
                {
                    Screen.SetResolution(size.x, size.y, false);
                    yield return null;
                    yield return null;
                    Assert.That(Screen.width, Is.EqualTo(size.x),
                        "Runner cannot certify portrait layout: requested width not applied.");
                    Assert.That(Screen.height, Is.EqualTo(size.y),
                        "Runner cannot certify portrait layout: requested height not applied.");

                    foreach (var pageName in Pages)
                    {
                        foreach (var candidate in Pages)
                            overlay.transform.Find(candidate).gameObject.SetActive(candidate == pageName);
                        yield return null;
                        Canvas.ForceUpdateCanvases();

                        var page = overlay.transform.Find(pageName).GetComponent<RectTransform>();
                        var scroll = page.Find(pageName + "Scroll").GetComponent<ScrollRect>();
                        Assert.That(scroll, Is.Not.Null);
                        var safe = safeArea.GetComponent<RectTransform>();
                        var panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(safe, page);
                        Assert.That(panelBounds.min.x, Is.GreaterThanOrEqualTo(safe.rect.xMin - 1f));
                        Assert.That(panelBounds.max.x, Is.LessThanOrEqualTo(safe.rect.xMax + 1f));
                        Assert.That(panelBounds.min.y, Is.GreaterThanOrEqualTo(safe.rect.yMin - 1f));
                        Assert.That(panelBounds.max.y, Is.LessThanOrEqualTo(safe.rect.yMax + 1f));
                        Assert.That(scroll.viewport.rect.height, Is.GreaterThan(0f));

                        var content = scroll.content;
                        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                        Canvas.ForceUpdateCanvases();
                        foreach (var control in content.GetComponentsInChildren<Button>(true))
                        {
                            Assert.That(control.GetComponent<RectTransform>().rect.height,
                                Is.GreaterThanOrEqualTo(95.9f),
                                "Button touch target too small: " + control.name);
                        }
                        foreach (var label in content.GetComponentsInChildren<TMP_Text>(true))
                        {
                            if (!label.gameObject.activeInHierarchy) continue;
                            label.ForceMeshUpdate();
                            Assert.That(label.text, Does.Not.Contain("No translation found"));
                            Assert.That(label.isTextOverflowing, Is.False,
                                "Vietnamese menu copy is clipped at " + size + ": " + label.name);
                        }

                        if (!string.IsNullOrWhiteSpace(evidence))
                        {
                            yield return new WaitForEndOfFrame();
                            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                            try
                            {
                                Assert.That(screenshot.width, Is.EqualTo(size.x));
                                Assert.That(screenshot.height, Is.EqualTo(size.y));
                                Directory.CreateDirectory(evidence);
                                File.WriteAllBytes(Path.Combine(evidence,
                                    "main-menu-" + size.x + "x" + size.y + "-" + pageName + ".png"),
                                    screenshot.EncodeToPNG());
                            }
                            finally { UnityEngine.Object.Destroy(screenshot); }
                        }
                    }
                }
                Assert.That(bootstrap.Snapshot.Revision, Is.EqualTo(startRevision));
                Assert.That(bootstrap.Snapshot.RunId, Is.EqualTo(startRunId));
            }
            finally
            {
                Screen.SetResolution(originalWidth, originalHeight, originalFullscreen);
            }
        }
    }
}

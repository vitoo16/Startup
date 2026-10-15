using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StartupLife.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    /// <summary>
    /// Real CanvasScaler tests. Unlike FirstPlayableVisualEvidence's legacy
    /// 1080x1920 capture, these tests actually resize the Player/Editor view.
    /// They fail if the test platform refuses to set the requested resolution;
    /// a green engine-free static test is not equivalent to this PlayMode pass.
    /// </summary>
    public sealed class CharacterCreatorResponsiveMatrixTests
    {
        private static readonly Vector2Int[] PortraitSizes =
        {
            new Vector2Int(360, 640),
            new Vector2Int(390, 844),
            new Vector2Int(430, 932)
        };
        private static readonly string[] Pages =
        {
            "CreatorIdentityPage", "CreatorLookPage",
            "CreatorWardrobePage", "CreatorTraitsPage"
        };

        [UnityTest]
        public IEnumerator CreatorKeepsActionsReachableAtThreeResolutionsAnd200PercentText()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            var originalFullscreen = Screen.fullScreen;
            // The test must not consume a real save or replace backups. Mirror
            // FirstPlayableFlowTests' save isolation for standalone PlayMode.
            var saveDirectory = UnityEngine.Application.persistentDataPath;
            var originals = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            Directory.CreateDirectory(saveDirectory);
            if (Directory.Exists(saveDirectory))
                foreach (var save in Directory.GetFiles(saveDirectory, "startup-life.json*"))
                {
                    originals.Add(save, File.ReadAllBytes(save));
                    File.Delete(save);
                }
            var output = Environment.GetEnvironmentVariable("STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE");
            var saveRequested = !string.IsNullOrWhiteSpace(output);

            try
            {
                foreach (var dimensions in PortraitSizes)
                {
                    Screen.SetResolution(dimensions.x, dimensions.y, false);
                    yield return null;
                    yield return null;
                    Assert.That(Screen.width, Is.EqualTo(dimensions.x),
                        "Unity did not apply requested width; this runner cannot certify responsive geometry.");
                    Assert.That(Screen.height, Is.EqualTo(dimensions.y),
                        "Unity did not apply requested height; this runner cannot certify responsive geometry.");

                    var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
                    while (!load.isDone) yield return null;
                    yield return LocalizationSettings.InitializationOperation;
                    yield return null;

                    var bootstrap = UnityEngine.Object.FindAnyObjectByType<StartupLifeBootstrapper>();
                    Assert.That(bootstrap && bootstrap.IsReady, Is.True,
                        "FirstPlayable must initialize before responsive verification.");
                    var creator = UnityEngine.Object.FindAnyObjectByType<CharacterCreationViewController>();
                    var textScaler = UnityEngine.Object.FindAnyObjectByType<CharacterCreatorTextScaler>();
                    var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    var scaler = canvas ? canvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
                    var safeArea = UnityEngine.Object.FindAnyObjectByType<MobileSafeArea>();
                    Assert.That(creator, Is.Not.Null, "The migrated Character Creator is missing.");
                    Assert.That(textScaler, Is.Not.Null, "The migrated text-scaling controls are missing.");
                    Assert.That(canvas, Is.Not.Null);
                    Assert.That(scaler && scaler.enabled, Is.True,
                        "CanvasScaler MUST remain enabled during the matrix.");
                    Assert.That(scaler.uiScaleMode,
                        Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize));
                    Assert.That(safeArea, Is.Not.Null);

                    for (var scaleStep = 0; scaleStep <= 2; scaleStep++)
                    {
                        textScaler.SetStep(scaleStep);
                        yield return null;
                        Assert.That(textScaler.ScalePercent,
                            Is.EqualTo(100 + scaleStep * 50));
                        for (var pageIndex = 0; pageIndex < Pages.Length; pageIndex++)
                        {
                            if (pageIndex > 0) creator.NextCreatorPage();
                            yield return null;
                            Canvas.ForceUpdateCanvases();
                            AssertCreatorActions(safeArea.GetComponent<RectTransform>(),
                                creator.GetComponent<RectTransform>(), Pages[pageIndex],
                                dimensions, textScaler.ScalePercent);

                            if (saveRequested)
                            {
                                // Capture the real resized screen, NOT a 1080x1920
                                // RenderTexture with CanvasScaler disabled.
                                yield return new WaitForEndOfFrame();
                                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                                try
                                {
                                    Assert.That(screenshot.width, Is.EqualTo(dimensions.x));
                                    Assert.That(screenshot.height, Is.EqualTo(dimensions.y));
                                    Directory.CreateDirectory(output);
                                    var filename = "creator-" + dimensions.x + "x" +
                                        dimensions.y + "-text" + textScaler.ScalePercent + "-" +
                                        Pages[pageIndex] + ".png";
                                    File.WriteAllBytes(Path.Combine(output, filename),
                                        screenshot.EncodeToPNG());
                                }
                                finally
                                {
                                    UnityEngine.Object.Destroy(screenshot);
                                }
                            }
                        }

                        // Identity becomes available after navigating back. A
                        // font change must not commit a save/game command.
                        for (var page = 1; page < Pages.Length; page++)
                            creator.PreviousCreatorPage();
                        yield return null;
                    }
                    LogAssert.NoUnexpectedReceived();
                }
            }
            finally
            {
                // Destroy active bootstrap to stop potential end-of-frame
                // checkpoint writes before restoring each byte-identical save.
                foreach (var bootstrap in UnityEngine.Object.FindObjectsByType<StartupLifeBootstrapper>(
                    FindObjectsSortMode.None))
                    UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);
                foreach (var save in Directory.GetFiles(saveDirectory, "startup-life.json*"))
                    File.Delete(save);
                foreach (var pair in originals)
                    File.WriteAllBytes(pair.Key, pair.Value);
                Screen.SetResolution(originalWidth, originalHeight, originalFullscreen);
            }
        }

        private static void AssertCreatorActions(RectTransform safeRect,
            RectTransform creatorRect, string currentPage, Vector2Int dimensions,
            int fontPercent)
        {
            var page = FindChild(creatorRect, currentPage);
            Assert.That(page && page.gameObject.activeInHierarchy, Is.True,
                "Active page is not in the migrated scroll view: " + currentPage);
            var scroll = FindChild(creatorRect, "CharacterCreatorScroll")
                .GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll && scroll.enabled && scroll.vertical &&
                !scroll.horizontal && scroll.viewport && scroll.content, Is.True,
                "The creator must have an actual scrollable viewport.");

            var back = FindChild(creatorRect, "CreatorPreviousPageButton");
            var next = FindChild(creatorRect, "CreatorNextPageButton");
            var submit = FindChild(creatorRect, "CreateCharacterButton");
            Assert.That(back && next && submit, Is.True,
                "Pinned Back/Next/Submit actions are missing.");
            var pinned = new[] { back, next, submit };
            foreach (var node in pinned)
            {
                var bound = RectTransformUtility.CalculateRelativeRectTransformBounds(safeRect, node);
                Assert.That(bound.min.x, Is.GreaterThanOrEqualTo(safeRect.rect.xMin - 1f),
                    "Action exits left safe area: " + node.name);
                Assert.That(bound.max.x, Is.LessThanOrEqualTo(safeRect.rect.xMax + 1f),
                    "Action exits right safe area: " + node.name);
                Assert.That(bound.min.y, Is.GreaterThanOrEqualTo(safeRect.rect.yMin - 1f),
                    "Action exits bottom safe area: " + node.name);
                Assert.That(bound.max.y, Is.LessThanOrEqualTo(safeRect.rect.yMax + 1f),
                    "Action exits top safe area: " + node.name);
                Assert.That(node.rect.height, Is.GreaterThanOrEqualTo(95.9f),
                    "Undersized target: " + node.name);
            }

            // Buttons should not be obscured by the scroll viewport, even
            // when copy is expanded to 200% and the content gets taller.
            var scrollBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                safeRect, scroll.GetComponent<RectTransform>());
            var pinnedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                safeRect, submit);
            Assert.That(scrollBounds.min.y, Is.GreaterThanOrEqualTo(pinnedBounds.max.y - 1f),
                "Scrolling content overlaps the primary action.");
            var navBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(safeRect, back);
            Assert.That(scrollBounds.min.y, Is.GreaterThanOrEqualTo(navBounds.max.y - 1f),
                "Scrollable text overlays Back/Next actions.");

            foreach (var text in page.GetComponentsInChildren<TMP_Text>())
            {
                if (!text.gameObject.activeInHierarchy) continue;
                text.ForceMeshUpdate();
                Assert.That(text.text, Does.Not.Contain("No translation found"),
                    "Missing Vietnamese translation: " + text.name);
                Assert.That(text.isTextOverflowing, Is.False,
                    "Creator text overflow at " + dimensions + " " + fontPercent + "%: " +
                    text.name);
            }
        }

        private static RectTransform FindChild(RectTransform root, string name)
        {
            var matches = root.GetComponentsInChildren<RectTransform>(true)
                .Where(x => x.name == name).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                "Expected exactly one authored control: " + name);
            return matches[0];
        }
    }
}

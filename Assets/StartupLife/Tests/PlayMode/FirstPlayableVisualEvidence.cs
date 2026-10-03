using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StartupLife.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace StartupLife.Tests.PlayMode
{
    internal static class FirstPlayableVisualEvidence
    {
        public static void Capture(string name, bool simulateNotch = false)
        {
            var directory = Environment.GetEnvironmentVariable("STARTUP_LIFE_M7_EVIDENCE");
            if (string.IsNullOrEmpty(directory)) return;
            Directory.CreateDirectory(directory);
            var outputPath = Path.Combine(directory, name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            const int width = 1080, height = 1920;
            var camera = Camera.main;
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var safeArea = UnityEngine.Object.FindAnyObjectByType<MobileSafeArea>();
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousPlaneDistance = canvas.planeDistance;
            var previousTarget = camera.targetTexture;
            var previousScale = canvas.scaleFactor;
            var previousScaler = scaler.enabled;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                scaler.enabled = false;
                canvas.scaleFactor = 1f;
                safeArea.Apply(simulateNotch ? new Rect(0, 90, width, height - 180) : new Rect(0, 0, width, height), width, height);
                Canvas.ForceUpdateCanvases();
                foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                {
                    text.ForceMeshUpdate();
                    Assert.That(text.text.Contains("No translation found"), Is.False, "Missing translation: " + text.name);
                    Assert.That(text.isTextOverflowing, Is.False, "Clipped text: " + text.name);
                    foreach (var character in text.text.Where(value => !char.IsWhiteSpace(value)).Distinct())
                        Assert.That(text.font.HasCharacter(character), Is.True, "Missing glyph in " + text.name + ": " + character);
                }
                var safeRect = safeArea.GetComponent<RectTransform>();
                foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                {
                    var rect = button.GetComponent<RectTransform>();
                    Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(95.9f), "Undersized touch target: " + button.name);
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(safeRect, rect);
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(safeRect.rect.yMin - 0.1f), "Button leaves lower safe area: " + button.name);
                    Assert.That(bounds.max.y, Is.LessThanOrEqualTo(safeRect.rect.yMax + 0.1f), "Button leaves upper safe area: " + button.name);
                }
                var request = new RenderPipeline.StandardRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True, "Active pipeline cannot render visual evidence.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Any(pixel => pixel.r > 20 || pixel.g > 20 || pixel.b > 20), Is.True, "Screenshot is black.");
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                var evidenceLog = "[StartupLifeM7Visual] " + name + " 1080x1920; UI-inclusive camera capture; simulatedNotch=" + simulateNotch;
                UnityEngine.TestTools.LogAssert.Expect(LogType.Log, evidenceLog);
                Debug.Log(evidenceLog);
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousPlaneDistance;
                canvas.scaleFactor = previousScale;
                scaler.enabled = previousScaler;
                safeArea.Apply(Screen.safeArea, Screen.width, Screen.height);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}

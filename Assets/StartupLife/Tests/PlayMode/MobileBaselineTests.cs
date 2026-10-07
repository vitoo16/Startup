using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    public sealed class MobileBaselineTests
    {
        [UnityTest]
        public IEnumerator MobileBaselineLoadsVietnameseThroughLocalizationWithGlyphCoverage()
        {
            yield return SceneManager.LoadSceneAsync("MobileBaseline");
            yield return LocalizationSettings.InitializationOperation;
            var deadline = Time.realtimeSinceStartup + 15;
            TMP_Text[] labels;
            do
            {
                yield return null;
                labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            } while ((labels.Length != 7 || labels.Any(t => string.IsNullOrEmpty(t.text))) && Time.realtimeSinceStartup < deadline);

            Assert.That(LocalizationSettings.SelectedLocale.Identifier.Code, Is.EqualTo("vi"));
            Assert.That(labels.Length, Is.EqualTo(7));
            Assert.That(labels.All(t => !string.IsNullOrEmpty(t.text)), Is.True);
            Assert.That(labels.Any(t => t.text.Contains("Một khởi đầu")), Is.True);
            Assert.That(Camera.main.orthographic, Is.True);
            foreach (var text in labels)
            {
                text.ForceMeshUpdate();
                foreach (var character in text.text.Where(c => !char.IsWhiteSpace(c)).Distinct())
                    Assert.That(text.font.HasCharacter(character, true, true), Is.True, "Missing glyph: " + character);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}

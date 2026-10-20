using System.Collections;
using NUnit.Framework;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StartupLife.Tests.PlayMode
{
    public sealed class FirstPlayableMainMenuTests
    {
        [UnityTest]
        public IEnumerator MenuSettingsAndNewRunCancellationNeverChangeCheckpoint()
        {
            var loading = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!loading.isDone) yield return null;
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.IsReady, Is.True);
            var initial = bootstrap.Snapshot;
            var key = UiPanelReveal.ReducedMotionPreference;
            var hadKey = PlayerPrefs.HasKey(key);
            var previous = PlayerPrefs.GetInt(key, 0);
            var menuEntry = GameObject.Find("StartupMainMenuOpenButton")?.GetComponent<Button>();
            Assert.That(menuEntry, Is.Not.Null);
            try
            {
                menuEntry.onClick.Invoke();
                var overlay = GameObject.Find("StartupMainMenuOverlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.activeInHierarchy, Is.True);

                var settings = GameObject.Find("StartupMainMenuSettingsButton")
                    ?.GetComponent<Button>();
                Assert.That(settings, Is.Not.Null);
                settings.onClick.Invoke();
                var toggle = GameObject.Find("StartupMainMenuMotionToggleButton")
                    ?.GetComponent<Button>();
                Assert.That(toggle, Is.Not.Null);
                toggle.onClick.Invoke();
                Assert.That(PlayerPrefs.GetInt(key, 0), Is.EqualTo(previous == 0 ? 1 : 0));
                GameObject.Find("StartupMainMenuSettingsBackButton")
                    .GetComponent<Button>().onClick.Invoke();

                GameObject.Find("StartupMainMenuNewGameButton")
                    .GetComponent<Button>().onClick.Invoke();
                if (!string.IsNullOrEmpty(initial.Name))
                {
                    Assert.That(GameObject.Find("StartupMainMenuNewGameConfirmation"), Is.Not.Null);
                    GameObject.Find("StartupMainMenuCancelButton")
                        .GetComponent<Button>().onClick.Invoke();
                }
                else
                {
                    Assert.That(GameObject.Find("StartupMainMenuOverlay"), Is.Null,
                        "An uncommitted character should return straight to its creator.");
                    menuEntry.onClick.Invoke();
                }

                var after = bootstrap.Snapshot;
                Assert.That(after.RunId, Is.EqualTo(initial.RunId));
                Assert.That(after.Revision, Is.EqualTo(initial.Revision));
                Assert.That(after.Cash, Is.EqualTo(initial.Cash));
                GameObject.Find("StartupMainMenuBackButton")
                    .GetComponent<Button>().onClick.Invoke();
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previous);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}

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
        public IEnumerator AllMenuPagesKeepScrollableContentAndTouchButtons()
        {
            var loading = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!loading.isDone) yield return null;
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.IsReady, Is.True);
            var original = bootstrap.Snapshot;
            var opener = GameObject.Find("StartupMainMenuOpenButton").GetComponent<Button>();
            opener.onClick.Invoke();

            var overlay = GameObject.Find("StartupMainMenuOverlay");
            Assert.That(overlay, Is.Not.Null);
            var paths = new[]
            {
                "StartupMainMenuHome",
                "StartupMainMenuSettings",
                "StartupMainMenuNewGameConfirmation"
            };
            foreach (var pageName in paths)
            {
                // Inactive Settings/Confirmation pages remain part of the
                // authored runtime hierarchy, not rebuild-on-navigation copies.
                var page = overlay.transform.Find(pageName);
                Assert.That(page, Is.Not.Null, pageName);
                var scroll = page.Find(pageName + "Scroll").GetComponent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.viewport, Is.Not.Null);
                Assert.That(scroll.viewport.GetComponent<Mask>(), Is.Not.Null);
                Assert.That(scroll.content, Is.Not.Null);
                Assert.That(scroll.content.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
                Assert.That(scroll.content.GetComponent<ContentSizeFitter>(), Is.Not.Null);
                Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
                var root = (RectTransform)scroll.transform;
                Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
            }

            var home = overlay.transform.Find(
                "StartupMainMenuHome/StartupMainMenuHomeScroll/Viewport/Content");
            var settings = overlay.transform.Find(
                "StartupMainMenuSettings/StartupMainMenuSettingsScroll/Viewport/Content");
            var confirm = overlay.transform.Find(
                "StartupMainMenuNewGameConfirmation/StartupMainMenuNewGameConfirmationScroll/Viewport/Content");
            Assert.That(home.Find("StartupMainMenuContinueButton"), Is.Not.Null);
            Assert.That(home.Find("StartupMainMenuNewGameButton"), Is.Not.Null);
            Assert.That(settings.Find("StartupMainMenuMotionToggleButton"), Is.Not.Null);
            Assert.That(confirm.Find("StartupMainMenuConfirmButton"), Is.Not.Null);
            Assert.That(confirm.Find("StartupMainMenuCancelButton"), Is.Not.Null);
            var confirmButton = confirm.Find("StartupMainMenuConfirmButton").GetComponent<Button>();
            Assert.That(confirmButton.GetComponent<LayoutElement>().preferredHeight,
                Is.GreaterThanOrEqualTo(96f));

            GameObject.Find("StartupMainMenuBackButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(bootstrap.Snapshot.Revision, Is.EqualTo(original.Revision));
            Assert.That(bootstrap.Snapshot.RunId, Is.EqualTo(original.RunId));
            Assert.That(bootstrap.Snapshot.Cash, Is.EqualTo(original.Cash));
        }

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

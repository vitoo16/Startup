using System.Collections;
using NUnit.Framework;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StartupLife.Tests.PlayMode
{
    public sealed class LifeHomeCompositionTests
    {
        [UnityTest]
        public IEnumerator LifeJournalReusesActionObjectsAndPreservesSnapshot()
        {
            var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            var root = Object.FindAnyObjectByType<LifeScreenViewController>(
                FindObjectsInactive.Include);
            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(root, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.IsReady, Is.True);
            var before = bootstrap.Snapshot;

            var life = root.transform;
            var content = life.Find("LifeHomeScroll/Viewport/Content");
            Assert.That(content, Is.Not.Null, "Life shell should be wrapped in one ScrollRect.");
            Assert.That(content.Find("LifeJournalHeader/NameRow"), Is.Not.Null);
            Assert.That(content.Find("LifeProgressCard/CashRow"), Is.Not.Null);
            Assert.That(content.Find("LifeActionCard/Actions"), Is.Not.Null);
            Assert.That(content.Find("LifeNeighborhoodStage/StageMoment"), Is.Not.Null);
            Assert.That(life.Find("LifeHomeScroll").GetComponent<ScrollRect>(), Is.Not.Null);

            // Recompose must move the ORIGINAL persisted UnityEvent-bearing buttons,
            // not replace them with unscripted copies.
            var actions = content.Find("LifeActionCard/Actions");
            Assert.That(actions.Find("AcceptDeveloperButton"), Is.Not.Null);
            Assert.That(actions.Find("WorkButton"), Is.Not.Null);
            Assert.That(actions.Find("BuyCourseButton"), Is.Not.Null);
            Assert.That(actions.Find("Study60Button"), Is.Not.Null);
            Assert.That(actions.Find("AdvanceDayButton"), Is.Not.Null);
            Assert.That(actions.Find("ResignButton"), Is.Not.Null);

            Assert.That(content.Find("LifeActionCard").GetSiblingIndex(),
                Is.LessThan(content.Find("LifeProgressCard").GetSiblingIndex()),
                "Decisions must be visible before the long progress ledger.");
            var enabledActions = actions.GetComponentsInChildren<Button>(true);
            root.Refresh();
            if (System.Array.Exists(enabledActions, button => button && button.interactable))
                Assert.That(actions.GetChild(0).GetComponent<Button>().interactable, Is.True,
                    "First displayed action must be available when any action can be taken.");

            Assert.That(bootstrap.Snapshot.Revision, Is.EqualTo(before.Revision));
            Assert.That(bootstrap.Snapshot.Cash, Is.EqualTo(before.Cash));
            Assert.That(bootstrap.Snapshot.RunId, Is.EqualTo(before.RunId));
        }

        [UnityTest]
        public IEnumerator ProfileOffersReducedMotionToggleWithoutChangingGameState()
        {
            var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            var view = Object.FindAnyObjectByType<LifeScreenViewController>(
                FindObjectsInactive.Include);
            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(view, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            var originalRevision = bootstrap.Snapshot.Revision;
            var originalRunId = bootstrap.Snapshot.RunId;
            var pref = UiPanelReveal.ReducedMotionPreference;
            var hadPref = PlayerPrefs.HasKey(pref);
            var previous = PlayerPrefs.GetInt(pref, 0);
            var wasVisible = view.gameObject.activeSelf;
            var overlay = view.GetComponent<LifeDashboardOverlay>();
            Assert.That(overlay, Is.Not.Null);
            try
            {
                view.SetVisible(true);
                overlay.Open();
                overlay.Select(LifeDashboardOverlay.Section.Profile);
                var button = GameObject.Find("LifeMotionToggleButton")?.GetComponent<Button>();
                Assert.That(button, Is.Not.Null);
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                Assert.That(PlayerPrefs.GetInt(pref, 0), Is.EqualTo(previous == 0 ? 1 : 0));
                Assert.That(bootstrap.Snapshot.Revision, Is.EqualTo(originalRevision));
                Assert.That(bootstrap.Snapshot.RunId, Is.EqualTo(originalRunId));
            }
            finally
            {
                overlay.Hide();
                view.SetVisible(wasVisible);
                if (hadPref) PlayerPrefs.SetInt(pref, previous);
                else PlayerPrefs.DeleteKey(pref);
                PlayerPrefs.Save();
            }
        }
    }
}

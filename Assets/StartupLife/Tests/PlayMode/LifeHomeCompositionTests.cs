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

            root.Refresh();
            Assert.That(bootstrap.Snapshot.Revision, Is.EqualTo(before.Revision));
            Assert.That(bootstrap.Snapshot.Cash, Is.EqualTo(before.Cash));
            Assert.That(bootstrap.Snapshot.RunId, Is.EqualTo(before.RunId));
        }
    }
}

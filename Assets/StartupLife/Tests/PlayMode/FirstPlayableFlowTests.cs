using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    public sealed class FirstPlayableFlowTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DeleteSave();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var bootstrap in Object.FindObjectsByType<StartupLifeBootstrapper>(FindObjectsSortMode.None))
                Object.DestroyImmediate(bootstrap.gameObject);
            DeleteSave();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateWorkStudyAndAdvanceDayThroughViews()
        {
            var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.IsReady, Is.True);

            var nameInput = GameObject.Find("CharacterNameInput").GetComponent<TMP_InputField>();
            nameInput.text = "Nguyễn Ánh";
            GameObject.Find("CreateCharacterButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.Name, Is.EqualTo("Nguyễn Ánh"));

            GameObject.Find("AcceptDeveloperButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.CareerId, Is.EqualTo("developer"));

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            typeof(WorkShiftPlaybackController).GetField("cueHoldSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(playback, 0f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

            var guard = 0;
            while (playback.IsRunning && guard++ < 300) yield return null;
            Assert.That(playback.IsRunning, Is.False, "Work playback did not settle.");
            Assert.That(bootstrap.Snapshot.CareerXp, Is.GreaterThan(0));
            Assert.That(bootstrap.Snapshot.Instant.Minute, Is.EqualTo(1020));
            Assert.That(bootstrap.Snapshot.PlaybackCursor, Is.EqualTo(1));

            GameObject.Find("BuyCourseButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.ActiveCourse, Is.Not.Null);
            Assert.That(bootstrap.Snapshot.ActiveCourse!.DefinitionId, Is.EqualTo("communication-basics"));

            GameObject.Find("Study60Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.ActiveCourse, Is.Not.Null);
            Assert.That(bootstrap.Snapshot.ActiveCourse!.ProgressUnits, Is.GreaterThan(0));

            var beforeDate = bootstrap.Snapshot.Instant.Date;
            GameObject.Find("AdvanceDayButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.Instant.Date, Is.EqualTo(beforeDate.AddDays(1)));
            Assert.That(bootstrap.Snapshot.Instant.Minute, Is.EqualTo(0));
            Assert.That(GameObject.Find("DaySummaryModal"), Is.Not.Null);
        }

        private static void DeleteSave()
        {
            var directory = UnityEngine.Application.persistentDataPath;
            if (!Directory.Exists(directory)) return;
            foreach (var path in Directory.GetFiles(directory, "startup-life.json*"))
            {
                try { File.Delete(path); }
                catch (IOException) { }
                catch (System.UnauthorizedAccessException) { }
            }
        }
    }
}

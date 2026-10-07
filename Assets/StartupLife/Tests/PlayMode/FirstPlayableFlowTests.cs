using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using StartupLife.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Localization.Settings;
using StartupLife.Core;

namespace StartupLife.Tests.PlayMode
{
    public sealed class FirstPlayableFlowTests
    {
        private readonly Dictionary<string, byte[]> preservedSaves = new Dictionary<string, byte[]>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            preservedSaves.Clear();
            if (Directory.Exists(UnityEngine.Application.persistentDataPath))
                foreach (var path in Directory.GetFiles(UnityEngine.Application.persistentDataPath, "startup-life.json*"))
                    preservedSaves.Add(path, File.ReadAllBytes(path));
            DeleteSave();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var bootstrap in Object.FindObjectsByType<StartupLifeBootstrapper>(FindObjectsSortMode.None))
                Object.DestroyImmediate(bootstrap.gameObject);
            DeleteSave();
            foreach (var save in preservedSaves) File.WriteAllBytes(save.Key, save.Value);
            preservedSaves.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AppearanceSelectionUsesLocalizedLabelsWithoutRawGenderGlyphs()
        {
            var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return LocalizationSettings.InitializationOperation;
            yield return null;

            var appearance = GameObject.Find("AppearanceValue").GetComponent<TMP_Text>();
            Assert.That(appearance.text, Is.EqualTo("Nữ"));
            Assert.That(appearance.text, Does.Not.Contain("♀"));
            Assert.That(appearance.text, Does.Not.Contain("♂"));
            appearance.ForceMeshUpdate();

            GameObject.Find("MaleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(appearance.text, Is.EqualTo("Nam"));
            Assert.That(appearance.text, Does.Not.Contain("♀"));
            Assert.That(appearance.text, Does.Not.Contain("♂"));
            appearance.ForceMeshUpdate();

            GameObject.Find("FemaleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(appearance.text, Is.EqualTo("Nữ"));
            appearance.ForceMeshUpdate();
            Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                .All(text => !text.text.Contains("♀") && !text.text.Contains("♂")), Is.True);
            LogAssert.NoUnexpectedReceived();
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
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Assert.That(GameObject.Find("CharacterCreationPanel"), Is.Not.Null);
            Assert.That(GameObject.Find("LifePanel"), Is.Null);
            FirstPlayableVisualEvidence.Capture("01-character-creation");
            var recordingFrame = 0;
            FirstPlayableVisualEvidence.Capture("recording/frame-" + (recordingFrame++).ToString("D4"));

            var nameInput = GameObject.Find("CharacterNameInput").GetComponent<TMP_InputField>();
            nameInput.text = "Nguyễn Ánh";
            GameObject.Find("CreateCharacterButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.Name, Is.EqualTo("Nguyễn Ánh"));
            Assert.That(GameObject.Find("CharacterCreationPanel"), Is.Null);
            Assert.That(GameObject.Find("LifePanel"), Is.Not.Null);

            GameObject.Find("AcceptDeveloperButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.CareerId, Is.EqualTo("developer"));
            FirstPlayableVisualEvidence.Capture("02-life-developer");
            FirstPlayableVisualEvidence.Capture("recording/frame-" + (recordingFrame++).ToString("D4"));

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            typeof(WorkShiftPlaybackController).GetField("cueHoldSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(playback, string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("STARTUP_LIFE_M7_EVIDENCE")) ? 0.1f : 2f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.05f);
            FirstPlayableVisualEvidence.Capture("03-work-playback");

            var deadline = Time.realtimeSinceStartup + 10f;
            while (playback.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                FirstPlayableVisualEvidence.Capture("recording/frame-" + (recordingFrame++).ToString("D4"));
                yield return new WaitForSecondsRealtime(0.2f);
            }
            Assert.That(playback.IsRunning, Is.False, "Work playback did not settle.");
            Assert.That(bootstrap.Snapshot.CareerXp, Is.GreaterThan(0));
            Assert.That(bootstrap.Snapshot.Instant.Minute, Is.EqualTo(1020));
            Assert.That(bootstrap.Snapshot.PlaybackCursor, Is.EqualTo(1));
            Assert.That(GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().interactable,
                Is.True, "Work button must recover after playback settles.");
            var earned = bootstrap.Snapshot;
            var acknowledged = bootstrap.Flow.AcknowledgePlayback(earned.PlaybackCursor + 1).Command;
            Assert.That(acknowledged.Status, Is.EqualTo(CommandStatus.Committed));
            Assert.That(acknowledged.Outcome.CashDelta, Is.Zero);
            Assert.That(acknowledged.Outcome.CareerXpDelta, Is.Zero);
            Assert.That(acknowledged.Outcome.SkillDeltas, Is.Empty);
            Assert.That(acknowledged.Outcome.GrantedIds, Is.Empty);
            Assert.That(bootstrap.Snapshot.CareerXp, Is.EqualTo(earned.CareerXp));
            Assert.That(bootstrap.Snapshot.Cash, Is.EqualTo(earned.Cash));

            GameObject.Find("BuyCourseButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.ActiveCourse, Is.Not.Null);
            Assert.That(bootstrap.Snapshot.ActiveCourse!.DefinitionId, Is.EqualTo("communication-basics"));

            GameObject.Find("Study60Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.ActiveCourse, Is.Not.Null);
            Assert.That(bootstrap.Snapshot.ActiveCourse!.ProgressUnits, Is.GreaterThan(0));
            Assert.That(GameObject.Find("CourseNameValue").GetComponent<TMP_Text>().text, Is.EqualTo("Giao tiếp cơ bản"));
            FirstPlayableVisualEvidence.Capture("04-evening-study");
            FirstPlayableVisualEvidence.Capture("recording/frame-" + (recordingFrame++).ToString("D4"));

            var beforeDate = bootstrap.Snapshot.Instant.Date;
            GameObject.Find("AdvanceDayButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.Instant.Date, Is.EqualTo(beforeDate.AddDays(1)));
            Assert.That(bootstrap.Snapshot.Instant.Minute, Is.EqualTo(0));
            Assert.That(GameObject.Find("DaySummaryModal"), Is.Not.Null);
            Assert.That(GameObject.Find("SummaryCashValue").GetComponent<TMP_Text>().text,
                Is.EqualTo(bootstrap.Snapshot.Cash.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫"));
            Assert.That(GameObject.Find("SummaryXpValue").GetComponent<TMP_Text>().text,
                Is.EqualTo(bootstrap.Snapshot.CareerXp.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            FirstPlayableVisualEvidence.Capture("05-day-summary");
            FirstPlayableVisualEvidence.Capture("recording/frame-" + (recordingFrame++).ToString("D4"));
            GameObject.Find("CloseSummaryButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            FirstPlayableVisualEvidence.Capture("06-simulated-notched-safe-area", true);

            var expected = bootstrap.Snapshot;
            var savePath = Path.Combine(UnityEngine.Application.persistentDataPath, "startup-life.json");
            Assert.That(File.Exists(savePath), Is.True, "Normal first-playable actions must write a save.");
            var evidenceDirectory = System.Environment.GetEnvironmentVariable("STARTUP_LIFE_M7_EVIDENCE");
            if (!string.IsNullOrEmpty(evidenceDirectory))
                File.Copy(savePath, Path.Combine(evidenceDirectory, "normal-save.json"), true);
            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;
            var reloaded = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(reloaded.IsReady, Is.True, "Normal Application save failed to reload.");
            Assert.That(reloaded.Snapshot.Name, Is.EqualTo(expected.Name));
            Assert.That(reloaded.Snapshot.Instant, Is.EqualTo(expected.Instant));
            Assert.That(reloaded.Snapshot.Cash, Is.EqualTo(expected.Cash));
            Assert.That(reloaded.Snapshot.CareerXp, Is.EqualTo(expected.CareerXp));
            Assert.That(reloaded.Snapshot.ActiveCourse.ProgressUnits, Is.EqualTo(expected.ActiveCourse.ProgressUnits));
            LogAssert.NoUnexpectedReceived();
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

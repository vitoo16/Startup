using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StartupLife.Core;
using StartupLife.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StartupLife.Tests.PlayMode
{
    public sealed class FirstPlayableLifecycleTests
    {
        private readonly Dictionary<string, byte[]> preservedSaves = new Dictionary<string, byte[]>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            preservedSaves.Clear();
            var directory = UnityEngine.Application.persistentDataPath;
            if (Directory.Exists(directory))
            {
                foreach (var path in Directory.GetFiles(directory, "startup-life.json*"))
                    preservedSaves.Add(path, File.ReadAllBytes(path));
            }

            DeleteSave();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var bootstrap in Object.FindObjectsByType<StartupLifeBootstrapper>(FindObjectsSortMode.None))
                Object.DestroyImmediate(bootstrap.gameObject);

            DeleteSave();
            foreach (var save in preservedSaves)
                File.WriteAllBytes(save.Key, save.Value);
            preservedSaves.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreshLaunchWithoutSaveShowsCharacterCreation()
        {
            yield return LoadFirstPlayable();

            var bootstrap = CurrentBootstrap();
            Assert.That(bootstrap.IsReady, Is.True);
            Assert.That(bootstrap.Snapshot.Name, Is.Empty);
            Assert.That(GameObject.Find("CharacterCreationPanel"), Is.Not.Null);
            Assert.That(GameObject.Find("LifePanel"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PauseDuringWorkRestoresPendingCueWithoutDuplicateRewards()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Pause Before Ack");

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            SetCueHold(playback, 5f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            Assert.That(playback.IsRunning, Is.True);
            var committed = bootstrap.Snapshot;
            Assert.That(committed.CareerXp, Is.GreaterThan(0));
            Assert.That(committed.CurrentActivityId, Is.Not.Empty);
            Assert.That(committed.PlaybackCursor, Is.Zero);
            FirstPlayableVisualEvidence.Capture("M7-T02/01-work-before-pause");

            bootstrap.HandleApplicationPause(true);
            Assert.That(playback.IsRunning, Is.False);
            AssertSnapshotIdentity(committed, bootstrap.Snapshot);

            var reload = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!reload.isDone) yield return null;

            var restored = CurrentBootstrap();
            Assert.That(restored.IsReady, Is.True);
            var beforeReplayAck = restored.Snapshot;
            AssertSnapshotIdentity(committed, beforeReplayAck);
            Assert.That(beforeReplayAck.PlaybackCursor, Is.Zero);
            FirstPlayableVisualEvidence.Capture("M7-T02/02-work-restored-before-ack");

            yield return new WaitForSecondsRealtime(0.35f);

            var acknowledged = restored.Snapshot;
            Assert.That(acknowledged.CurrentActivityId, Is.EqualTo(committed.CurrentActivityId));
            Assert.That(acknowledged.PlaybackCursor, Is.EqualTo(1));
            Assert.That(acknowledged.Revision, Is.EqualTo(committed.Revision + 1));
            Assert.That(acknowledged.CareerXp, Is.EqualTo(committed.CareerXp));
            Assert.That(acknowledged.Cash, Is.EqualTo(committed.Cash));
            Assert.That(acknowledged.Instant, Is.EqualTo(committed.Instant));
            Assert.That(GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AcknowledgedWorkCueDoesNotReplayAfterRestart()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Pause After Ack");

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            SetCueHold(playback, 0f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            Assert.That(playback.IsRunning, Is.False);
            var acknowledged = bootstrap.Snapshot;
            Assert.That(acknowledged.Instant.Minute, Is.EqualTo(1020));
            Assert.That(acknowledged.PlaybackCursor, Is.EqualTo(1));

            bootstrap.HandleApplicationPause(true);
            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(0.35f);

            var restored = CurrentBootstrap();
            Assert.That(restored.IsReady, Is.True);
            AssertSnapshotIdentity(acknowledged, restored.Snapshot);
            Assert.That(Object.FindAnyObjectByType<WorkShiftPlaybackController>().IsRunning, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MidCourseRestartRestoresExactProgressAndCompletesOnce()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Course Restore");

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            SetCueHold(playback, 0f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(bootstrap.Snapshot.Instant.Minute, Is.EqualTo(1020));

            GameObject.Find("BuyCourseButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            GameObject.Find("Study60Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            var beforeRestart = bootstrap.Snapshot;
            var course = beforeRestart.ActiveCourse;
            Assert.That(course, Is.Not.Null);
            Assert.That(course!.DefinitionId, Is.EqualTo("communication-basics"));
            Assert.That(course.ProgressUnits, Is.EqualTo(600000));
            var cashAfterPurchase = beforeRestart.Cash;
            FirstPlayableVisualEvidence.Capture("M7-T02/03-course-before-restart");

            bootstrap.HandleApplicationPause(true);
            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;

            var restored = CurrentBootstrap();
            var restoredCourse = restored.Snapshot.ActiveCourse;
            Assert.That(restoredCourse, Is.Not.Null);
            Assert.That(restoredCourse!.InstanceId, Is.EqualTo(course.InstanceId));
            Assert.That(restoredCourse.DefinitionId, Is.EqualTo(course.DefinitionId));
            Assert.That(restoredCourse.ProgressUnits, Is.EqualTo(course.ProgressUnits));
            Assert.That(restoredCourse.TargetUnits, Is.EqualTo(course.TargetUnits));
            Assert.That(restored.Snapshot.Cash, Is.EqualTo(cashAfterPurchase));
            FirstPlayableVisualEvidence.Capture("M7-T02/04-course-after-restore");

            for (var i = 0; i < 4; i++)
            {
                var study = restored.Flow.Study(60).Command;
                Assert.That(study.Status, Is.EqualTo(CommandStatus.Committed));
            }

            Assert.That(restored.Snapshot.ActiveCourse, Is.Not.Null);
            Assert.That(restored.Snapshot.ActiveCourse!.ProgressUnits, Is.EqualTo(3000000));
            Assert.That(restored.Snapshot.Instant.Minute, Is.EqualTo(1320));

            var nextDay = restored.Flow.AdvanceDay().Advance;
            Assert.That(nextDay.StopReason, Is.EqualTo("TargetReached"));
            Assert.That(restored.Snapshot.Instant.Minute, Is.Zero);

            var nextPlayback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            SetCueHold(nextPlayback, 0f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(restored.Snapshot.Instant.Minute, Is.EqualTo(1020));

            var completion = restored.Flow.Study(60).Command;
            Assert.That(completion.Status, Is.EqualTo(CommandStatus.Committed));
            Assert.That(completion.Outcome, Is.Not.Null);
            Assert.That(completion.Outcome!.CourseChange, Is.Not.Null);
            Assert.That(completion.Outcome.CourseChange!.Kind, Is.EqualTo(CourseChangeKind.Completed));
            Assert.That(restored.Snapshot.ActiveCourse, Is.Null);
            Assert.That(restored.Snapshot.Cash, Is.EqualTo(cashAfterPurchase));
            Assert.That(restored.Snapshot.History.Count(entry => entry == "course.completed:communication-basics"), Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NextDayRestartDoesNotRecommitBoundary()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Next Day");

            var playback = Object.FindAnyObjectByType<WorkShiftPlaybackController>();
            SetCueHold(playback, 0f);
            GameObject.Find("WorkButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            var priorDate = bootstrap.Snapshot.Instant.Date;
            var advance = bootstrap.Flow.AdvanceDay().Advance;
            Assert.That(advance.StopReason, Is.EqualTo("TargetReached"));
            var committedNextDay = bootstrap.Snapshot;
            Assert.That(committedNextDay.Instant.Date, Is.EqualTo(priorDate.AddDays(1)));
            Assert.That(committedNextDay.Instant.Minute, Is.Zero);
            // This fixture dispatches directly through Flow; refresh the view before capturing it.
            bootstrap.RefreshMode();
            Assert.That(GameObject.Find("DateValue").GetComponent<TMP_Text>().text,
                Is.EqualTo(committedNextDay.Instant.Date.ToString()), "Capture must show the committed day.");
            Assert.That(GameObject.Find("TimeValue").GetComponent<TMP_Text>().text,
                Is.EqualTo("00:00"), "Capture must show the committed minute.");
            FirstPlayableVisualEvidence.Capture("M7-T02/05-next-day-before-restart");

            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(0.1f);

            var restored = CurrentBootstrap();
            Assert.That(restored.IsReady, Is.True);
            AssertSnapshotIdentity(committedNextDay, restored.Snapshot);
            FirstPlayableVisualEvidence.Capture("M7-T02/06-next-day-after-restore");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CorruptPrimaryRecoversBackupAndAllowsNextWrite()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Backup Recovery");
            var expected = bootstrap.Snapshot;

            var primary = SavePath();
            var backup = primary + ".backup";
            Assert.That(File.Exists(primary), Is.True);
            File.Copy(primary, backup, true);
            File.WriteAllText(primary, "{corrupt-primary");

            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;

            var recovered = CurrentBootstrap();
            Assert.That(recovered.IsReady, Is.True);
            AssertSnapshotIdentity(expected, recovered.Snapshot);

            var resign = recovered.Flow.Resign().Command;
            Assert.That(resign.Status, Is.EqualTo(CommandStatus.Committed));
            Assert.That(recovered.Snapshot.CareerId, Is.Empty);

            var afterRecoveryWrite = recovered.Snapshot;
            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;
            var reloaded = CurrentBootstrap();
            Assert.That(reloaded.IsReady, Is.True);
            AssertSnapshotIdentity(afterRecoveryWrite, reloaded.Snapshot);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UnreadablePrimaryFailsClosedWithoutUsingBackup()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Unreadable Primary");

            var primary = SavePath();
            var backup = primary + ".backup";
            File.Copy(primary, backup, true);
            File.Delete(primary);
            Directory.CreateDirectory(primary);

            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;

            var failed = CurrentBootstrap();
            Assert.That(failed.IsReady, Is.False);
            Assert.That(GameObject.Find("LifePanel"), Is.Null);
            Assert.That(File.Exists(backup), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BothInvalidSavesFailWithoutStartingNewRun()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterAndAcceptDeveloper(bootstrap, "Both Invalid");

            var primary = SavePath();
            var backup = primary + ".backup";
            File.WriteAllText(primary, "{broken-primary");
            File.WriteAllText(backup, "{broken-backup");

            yield return SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            yield return null;

            var failed = CurrentBootstrap();
            Assert.That(failed.IsReady, Is.False);
            Assert.That(GameObject.Find("CharacterCreationPanel"), Is.Null);
            Assert.That(GameObject.Find("LifePanel"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RealElapsedPauseDoesNotAdvanceSimulation()
        {
            yield return LoadFirstPlayable();
            var bootstrap = CurrentBootstrap();
            CreateCharacterOnly(bootstrap, "Wall Clock");

            var before = bootstrap.Snapshot;
            bootstrap.HandleApplicationPause(true);
            yield return new WaitForSecondsRealtime(0.4f);
            bootstrap.HandleApplicationPause(false);
            yield return null;

            var after = bootstrap.Snapshot;
            AssertSnapshotIdentity(before, after);
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadFirstPlayable()
        {
            var load = SceneManager.LoadSceneAsync("FirstPlayable", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
        }

        private static StartupLifeBootstrapper CurrentBootstrap()
        {
            var bootstrap = Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            Assert.That(bootstrap, Is.Not.Null);
            return bootstrap;
        }

        private static void CreateCharacterOnly(StartupLifeBootstrapper bootstrap, string name)
        {
            var input = GameObject.Find("CharacterNameInput").GetComponent<TMP_InputField>();
            input.text = name;
            GameObject.Find("CreateCharacterButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(bootstrap.IsReady, Is.True);
            Assert.That(bootstrap.Snapshot.Name, Is.EqualTo(name));
        }

        private static void CreateCharacterAndAcceptDeveloper(StartupLifeBootstrapper bootstrap, string name)
        {
            CreateCharacterOnly(bootstrap, name);
            GameObject.Find("AcceptDeveloperButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(bootstrap.Snapshot.CareerId, Is.EqualTo("developer"));
        }

        private static void SetCueHold(WorkShiftPlaybackController playback, float seconds)
        {
            Assert.That(playback, Is.Not.Null);
            typeof(WorkShiftPlaybackController)
                .GetField("cueHoldSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(playback, seconds);
        }

        private static void AssertSnapshotIdentity(FirstPlayableState expected, FirstPlayableState actual)
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Instant, Is.EqualTo(expected.Instant));
            Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
            Assert.That(actual.Cash, Is.EqualTo(expected.Cash));
            Assert.That(actual.CareerId, Is.EqualTo(expected.CareerId));
            Assert.That(actual.CareerXp, Is.EqualTo(expected.CareerXp));
            Assert.That(actual.Rank, Is.EqualTo(expected.Rank));
            Assert.That(actual.CurrentActivityId, Is.EqualTo(expected.CurrentActivityId));
            Assert.That(actual.PlaybackCursor, Is.EqualTo(expected.PlaybackCursor));
            Assert.That(actual.History, Is.EqualTo(expected.History));
        }

        private static string SavePath() =>
            Path.Combine(UnityEngine.Application.persistentDataPath, "startup-life.json");

        private static void DeleteSave()
        {
            var directory = UnityEngine.Application.persistentDataPath;
            if (!Directory.Exists(directory)) return;

            var primary = SavePath();
            if (Directory.Exists(primary))
            {
                try { Directory.Delete(primary, true); }
                catch (IOException) { }
                catch (System.UnauthorizedAccessException) { }
            }

            foreach (var path in Directory.GetFiles(directory, "startup-life.json*"))
            {
                try { File.Delete(path); }
                catch (IOException) { }
                catch (System.UnauthorizedAccessException) { }
            }
        }
    }
}

using System;
using System.Collections;
using StartupLife.Core;
using UnityEngine;

namespace StartupLife.Presentation
{
    public sealed class WorkShiftPlaybackController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float cueHoldSeconds = 0.25f;

        private FirstPlayableFlow flow;
        private ContentCatalog content;
        private Action refresh;
        private LocalizedKeyLabel status;
        private bool running;

        public bool IsRunning => running;

        public void Bind(FirstPlayableFlow coordinator, ContentCatalog catalog, Action refreshView, LocalizedKeyLabel statusLabel)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            content = catalog ?? throw new ArgumentNullException(nameof(catalog));
            refresh = refreshView ?? throw new ArgumentNullException(nameof(refreshView));
            status = statusLabel;
        }

        public void RunShift()
        {
            if (!running) StartCoroutine(RunShiftRoutine());
        }

        internal void SuspendForLifecycle()
        {
            if (running) StopAllCoroutines();
            running = false;
            refresh?.Invoke();
        }

        internal void RestoreFromSnapshot()
        {
            if (running || flow == null || content == null) return;
            var snapshot = flow.Refresh();
            if (!TryGetPendingWorkCue(snapshot, out var cue)) return;
            StartCoroutine(ReplayPendingCueRoutine(
                snapshot.CurrentActivityId,
                snapshot.PlaybackCursor,
                cue));
        }

        private IEnumerator RunShiftRoutine()
        {
            if (flow == null || content == null) throw new InvalidOperationException("Work playback is not bound.");

            running = true;
            try
            {
                var initial = flow.Refresh();
                if (string.IsNullOrEmpty(initial.CareerId) || !content.Careers.TryGetValue(initial.CareerId, out var career))
                {
                    status?.SetKey("reason.career.required");
                    yield break;
                }

                var date = initial.Instant.Date;
                var guard = 0;
                while (guard++ < 32)
                {
                    var before = flow.Refresh();
                    if (before.Instant.Date != date || before.CareerId != career.Id || before.Instant.Minute >= career.EndMinute)
                        break;

                    var result = flow.AdvanceBoundary().Command;
                    if (result.Status != CommandStatus.Committed && result.Status != CommandStatus.AlreadyCommitted)
                    {
                        status?.SetKey("reason." + result.ReasonKey);
                        yield break;
                    }

                    var outcome = result.Outcome;
                    refresh();
                    if (outcome != null && career.Scenes.Exists(scene => scene.Id == outcome.Cue))
                    {
                        var expectedActivityId = outcome.ActivityId;
                        var expectedCursor = outcome.PlaybackCursor;
                        var expectedCue = outcome.Cue;
                        status?.SetKey("scene." + expectedCue);
                        if (cueHoldSeconds > 0f) yield return new WaitForSecondsRealtime(cueHoldSeconds);

                        var snapshot = flow.Refresh();
                        if (!MatchesPendingCue(snapshot, expectedActivityId, expectedCursor, expectedCue))
                            yield break;

                        var acknowledge = flow.AcknowledgePlayback(expectedCursor + 1).Command;
                        if (acknowledge.Status != CommandStatus.Committed && acknowledge.Status != CommandStatus.AlreadyCommitted)
                        {
                            status?.SetKey("reason." + acknowledge.ReasonKey);
                            yield break;
                        }
                    }
                }

                status?.SetKey("status.work.complete");
            }
            finally
            {
                running = false;
                refresh();
            }
        }

        private IEnumerator ReplayPendingCueRoutine(string expectedActivityId, int expectedCursor, string expectedCue)
        {
            running = true;
            refresh();
            try
            {
                status?.SetKey("scene." + expectedCue);
                if (cueHoldSeconds > 0f) yield return new WaitForSecondsRealtime(cueHoldSeconds);

                var snapshot = flow.Refresh();
                if (!MatchesPendingCue(snapshot, expectedActivityId, expectedCursor, expectedCue))
                    yield break;

                var acknowledge = flow.AcknowledgePlayback(expectedCursor + 1).Command;
                if (acknowledge.Status != CommandStatus.Committed && acknowledge.Status != CommandStatus.AlreadyCommitted)
                    status?.SetKey("reason." + acknowledge.ReasonKey);
            }
            finally
            {
                running = false;
                refresh();
            }
        }

        private bool TryGetPendingWorkCue(FirstPlayableState snapshot, out string cue)
        {
            cue = "";
            if (snapshot.PlaybackCursor != 0 ||
                string.IsNullOrEmpty(snapshot.CurrentActivityId) ||
                string.IsNullOrEmpty(snapshot.CareerId) ||
                !content.Careers.TryGetValue(snapshot.CareerId, out var career) ||
                !career.Scenes.Exists(scene => scene.Id == snapshot.Cue))
                return false;

            cue = snapshot.Cue;
            return true;
        }

        private static bool MatchesPendingCue(
            FirstPlayableState snapshot,
            string expectedActivityId,
            int expectedCursor,
            string expectedCue) =>
            snapshot.CurrentActivityId == expectedActivityId &&
            snapshot.PlaybackCursor == expectedCursor &&
            snapshot.Cue == expectedCue;

    }

    internal static class CareerSceneListExtensions
    {
        public static bool Exists(this System.Collections.Generic.IReadOnlyList<CareerSceneDefinition> scenes, Predicate<CareerSceneDefinition> predicate)
        {
            for (var i = 0; i < scenes.Count; i++)
                if (predicate(scenes[i])) return true;
            return false;
        }
    }
}

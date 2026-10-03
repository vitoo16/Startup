using System;
using System.Collections;
using StartupLife.Core;
using UnityEngine;

namespace StartupLife.Presentation
{
    public sealed class WorkShiftPlaybackController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float cueHoldSeconds = 0.25f;

        private FirstPlayableFlowCoordinator flow;
        private ContentCatalog content;
        private Action refresh;
        private LocalizedKeyLabel status;
        private bool running;

        public bool IsRunning => running;

        public void Bind(FirstPlayableFlowCoordinator coordinator, ContentCatalog catalog, Action refreshView, LocalizedKeyLabel statusLabel)
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

        private IEnumerator RunShiftRoutine()
        {
            if (flow == null || content == null) throw new InvalidOperationException("Work playback is not bound.");

            running = true;
            try
            {
                var initial = flow.Snapshot();
                if (string.IsNullOrEmpty(initial.CareerId) || !content.Careers.TryGetValue(initial.CareerId, out var career))
                {
                    status?.SetKey("reason.career.required");
                    yield break;
                }

                var date = initial.Instant.Date;
                var guard = 0;
                while (guard++ < 32)
                {
                    var before = flow.Snapshot();
                    if (before.Instant.Date != date || before.CareerId != career.Id || before.Instant.Minute >= career.EndMinute)
                        break;

                    var result = flow.AdvanceBoundary();
                    if (result.Status != CommandStatus.Committed && result.Status != CommandStatus.AlreadyCommitted)
                    {
                        status?.SetKey("reason." + result.ReasonKey);
                        yield break;
                    }

                    var outcome = result.Outcome;
                    refresh();
                    if (outcome != null && career.Scenes.Exists(scene => scene.Id == outcome.Cue))
                    {
                        status?.SetKey("scene." + outcome.Cue);
                        if (cueHoldSeconds > 0f) yield return new WaitForSecondsRealtime(cueHoldSeconds);

                        var snapshot = flow.Snapshot();
                        if (!string.IsNullOrEmpty(snapshot.CurrentActivityId))
                        {
                            var acknowledge = flow.AcknowledgePlayback(snapshot.PlaybackCursor + 1);
                            if (acknowledge.Status != CommandStatus.Committed && acknowledge.Status != CommandStatus.AlreadyCommitted)
                            {
                                status?.SetKey("reason." + acknowledge.ReasonKey);
                                yield break;
                            }
                        }
                    }
                }

                status?.SetKey("status.work.complete");
                refresh();
            }
            finally
            {
                running = false;
            }
        }
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

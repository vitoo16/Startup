#nullable enable
using System;

namespace StartupLife.Presentation
{
    internal sealed class FirstPlayableLifecycle
    {
        private readonly FirstPlayableFlow flow;
        private readonly WorkShiftPlaybackController workPlayback;
        private readonly Action refresh;
        private bool suspended;

        internal FirstPlayableLifecycle(
            FirstPlayableFlow coordinator,
            WorkShiftPlaybackController workController,
            Action refreshView)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            workPlayback = workController ?? throw new ArgumentNullException(nameof(workController));
            refresh = refreshView ?? throw new ArgumentNullException(nameof(refreshView));
        }

        internal FirstPlayableState? LastPauseSnapshot { get; private set; }
        internal bool IsSuspended => suspended;

        internal void RestoreAfterBootstrap()
        {
            if (suspended) return;
            workPlayback.RestoreFromSnapshot();
            refresh();
        }

        internal void SetPaused(bool paused)
        {
            if (paused == suspended) return;

            if (paused)
            {
                LastPauseSnapshot = flow.Refresh();
                suspended = true;
                workPlayback.SuspendForLifecycle();
            }
            else
            {
                suspended = false;
                workPlayback.RestoreFromSnapshot();
            }

            refresh();
        }

        internal void Quit()
        {
            if (suspended) return;
            LastPauseSnapshot = flow.Refresh();
            suspended = true;
            workPlayback.SuspendForLifecycle();
            refresh();
        }
    }
}

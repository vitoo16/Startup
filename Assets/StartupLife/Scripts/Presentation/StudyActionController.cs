using System;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    public sealed class StudyActionController
    {
        private readonly FirstPlayableFlowCoordinator flow;
        private readonly Action refresh;
        private readonly LocalizedKeyLabel status;

        public StudyActionController(FirstPlayableFlowCoordinator coordinator, Action refreshView, LocalizedKeyLabel statusLabel)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            refresh = refreshView ?? throw new ArgumentNullException(nameof(refreshView));
            status = statusLabel;
        }

        public CommandResult PurchaseCommunicationCourse()
        {
            var result = flow.PurchaseCourse("communication-basics");
            Publish(result, "status.course.purchased");
            return result;
        }

        public CommandResult Study60Minutes()
        {
            var result = flow.Study(60);
            Publish(result, result.Outcome?.CourseChange?.Kind == CourseChangeKind.Completed
                ? "status.course.completed"
                : "status.course.progressed");
            return result;
        }

        private void Publish(CommandResult result, string successKey)
        {
            if (result.Status == CommandStatus.Committed || result.Status == CommandStatus.AlreadyCommitted)
                status?.SetKey(successKey);
            else
                status?.SetKey("reason." + result.ReasonKey);
            refresh();
        }
    }
}

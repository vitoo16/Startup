using System;
using StartupLife.Core;

namespace StartupLife.Presentation
{
    public sealed class StudyActionController
    {
        private readonly FirstPlayableFlow flow;
        private readonly Action refresh;
        private readonly LocalizedKeyLabel status;

        public StudyActionController(FirstPlayableFlow coordinator, Action refreshView, LocalizedKeyLabel statusLabel)
        {
            flow = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            refresh = refreshView ?? throw new ArgumentNullException(nameof(refreshView));
            status = statusLabel;
        }

        public CommandResult PurchaseCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                throw new ArgumentException("A catalog course ID is required.", nameof(courseId));
            var result = flow.PurchaseCourse(courseId).Command;
            Publish(result, "status.course.purchased");
            return result;
        }

        // Legacy UnityEvent buttons in older authored scenes still target
        // this entry point. The five-tab course picker uses PurchaseCourse.
        public CommandResult PurchaseCommunicationCourse() =>
            PurchaseCourse("communication-basics");

        public CommandResult Study60Minutes()
        {
            var result = flow.Study(60).Command;
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

#nullable enable
using System;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Reconstructs only committed time provenance; no simulation replay, rewards or mutation.
    // StateValidation's receipt timeline remains the authority for cursor and duration validity.
    public static class CommittedTimeTrace
    {
        public const byte Free = 0;
        public const byte Employment = 1;
        public const byte Study = 2;
        public const byte Elapsed = 3;

        public static byte[] Project(GameState state, ContentCatalog content)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var slots = new byte[1440];
            var date = StateValidation.ReceiptOrigin(state);
            var minute = 0;
            var jobIndex = 0;
            EmploymentState? active = null;
            var jobs = state.PreviousEmployment.Concat(state.Employment == null
                ? Array.Empty<EmploymentState>() : new[] { state.Employment }).ToArray();

            foreach (var receipt in state.Receipts)
            {
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                if (command.Kind == CommandKind.AdvanceBoundary || command.Kind == CommandKind.Study)
                {
                    var end = checked(minute + receipt.MinutesConsumed);
                    if (date == state.Date && receipt.MinutesConsumed > 0)
                    {
                        var kind = Elapsed;
                        if (command.Kind == CommandKind.Study) kind = Study;
                        else if (active != null && IsCommittedCareerSlot(active, content, date, minute, end))
                            kind = Employment;
                        Mark(slots, minute, Math.Min(1440, end), kind,
                            content.Schedule.WakeMinute, content.Schedule.SleepMinute);
                    }
                }
                if (command.Kind == CommandKind.AcceptJob)
                {
                    if (jobIndex >= jobs.Length || active != null || jobs[jobIndex].CareerId != command.ContentId)
                        throw new ArgumentException("Untrusted committed employment order.");
                    active = jobs[jobIndex++];
                }
                else if (command.Kind == CommandKind.Resign)
                {
                    if (active == null) throw new ArgumentException("Unmatched committed resignation.");
                    active = null;
                }
                var next = checked(minute + receipt.MinutesConsumed);
                date = date.AddDays(next / 1440);
                minute = next % 1440;
            }
            if (date != state.Date || minute != state.Minute || jobIndex != jobs.Length ||
                (active == null) != (state.Employment == null))
                throw new ArgumentException("Receipt occupancy provenance does not match current state.");

            // The active employment definition reserves its whole mandatory shift,
            // including not-yet-committed future minutes. Resigned shifts do not.
            if (state.Employment != null)
            {
                var career = ResolveCareer(state.Employment, content);
                if (string.CompareOrdinal(state.DateIso, state.Employment.FirstShiftIso) >= 0 &&
                    career.WorksOn(state.Date))
                    Mark(slots, career.StartMinute, career.EndMinute, Employment,
                        content.Schedule.WakeMinute, content.Schedule.SleepMinute);
            }

            // Time skipped or left idle before the cursor is elapsed, not available for new allocation.
            var cutoff = Math.Min(content.Schedule.SleepMinute, Math.Max(content.Schedule.WakeMinute, state.Minute));
            for (var t = content.Schedule.WakeMinute; t < cutoff; t++)
                if (slots[t] == Free) slots[t] = Elapsed;
            return slots;
        }

        private static bool IsCommittedCareerSlot(EmploymentState employment, ContentCatalog content,
            SimDate date, int start, int end)
        {
            var career = ResolveCareer(employment, content);
            return string.CompareOrdinal(date.ToString(), employment.FirstShiftIso) >= 0 &&
                career.WorksOn(date) && start >= career.StartMinute && end <= career.EndMinute;
        }

        private static CareerDefinition ResolveCareer(EmploymentState employment, ContentCatalog content)
        {
            if (!content.Careers.TryGetValue(employment.CareerId, out var career))
                throw new ContentCompatibilityException("save.content_id", "Historical career is unavailable.");
            if (career.Revision != employment.DefinitionRevision)
                throw new ContentCompatibilityException("save.content_revision", "Historical career revision is unavailable.");
            return career;
        }

        private static void Mark(byte[] slots, int start, int end, byte kind, int wake, int sleep)
        {
            for (var t = Math.Max(start, wake); t < Math.Min(end, sleep); t++)
            {
                if (slots[t] != Free && slots[t] != kind)
                    throw new ArgumentException("Overlapping committed owner-time classifications.");
                slots[t] = kind;
            }
        }
    }
}

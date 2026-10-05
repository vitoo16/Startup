#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace StartupLife.Core
{
    public static class StateValidation
    {
        public static void Validate(GameState s, ContentCatalog c, IRestoreStateValidator? restoreValidator = null)
        {
            var activities = ValidateStructure(s);
            if (s.ContentVersion != c.Version)
                throw new ContentCompatibilityException("save.content_version", "Save content version is unsupported by the active catalog.");

            ContentCompatibilityException? incompatibility = null;
            void Unsupported(string key, string message) { incompatibility ??= new ContentCompatibilityException(key, message); }
            void CheckContent(Action check)
            {
                try { check(); }
                catch (ContentCompatibilityException e) { incompatibility ??= e; }
            }
            if (s.Name.Length > 0)
            {
                if (!c.Starts.TryGetValue(s.BackgroundId, out var start))
                    Unsupported("save.content_id", "Character background is unavailable in the active catalog.");
                else
                {
                    if (!start.AppearanceIds.Contains(s.AppearanceId))
                        Unsupported("save.content_id", "Character appearance is unavailable in the active catalog.");
                    if (s.LearningSpeed != start.LearningSpeed)
                        throw new ArgumentException("Character learning speed differs from its start definition.");
                }
            }
            var missingSkill = false;
            foreach (var skill in s.Skills)
                if (!c.Skills.ContainsKey(skill.Id))
                { missingSkill = true; Unsupported("save.content_id", "Skill definition is unavailable in the active catalog."); }
            if (!missingSkill && s.Name.Length > 0 && s.Skills.Count != c.Skills.Count)
                throw new ArgumentException("Character skill set is incomplete.");
            CheckContent(() => ValidateEmploymentContent(s.Employment, c));
            foreach (var employment in s.PreviousEmployment) CheckContent(() => ValidateEmploymentContent(employment, c));
            CheckContent(() => ValidateCourseContent(s.Course, c));
            foreach (var course in s.CompletedCourses) CheckContent(() => ValidateCourseContent(course, c));

            foreach (var id in s.Scheduler.Deck)
                if (!c.Careers.Values.Any(career => career.Scenes.Any(scene => scene.Id == id)))
                    Unsupported("save.content_id", "Scheduled scene is unavailable in the active catalog.");
            if (s.Scheduler.LastScene.Length > 0 && !c.Careers.Values.Any(career => career.Scenes.Any(scene => scene.Id == s.Scheduler.LastScene)))
                Unsupported("save.content_id", "Last scheduled scene is unavailable in the active catalog.");

            var schedulerEmployment = s.Employment ?? (s.PreviousEmployment.Count == 0 ? null : s.PreviousEmployment[s.PreviousEmployment.Count - 1]);
            if (s.Scheduler.Deck.Count > 0 && schedulerEmployment == null)
                throw new ArgumentException("Scheduler has no owning employment.");
            var schedulerReferencesAvailable = s.Scheduler.Deck.All(id => c.Careers.Values.Any(career => career.Scenes.Any(scene => scene.Id == id))) &&
                (s.Scheduler.LastScene.Length == 0 || c.Careers.Values.Any(career => career.Scenes.Any(scene => scene.Id == s.Scheduler.LastScene)));
            if (schedulerEmployment != null && schedulerReferencesAvailable && c.Careers.TryGetValue(schedulerEmployment.CareerId, out var schedulerCareer) &&
                schedulerCareer.Revision == schedulerEmployment.DefinitionRevision)
                ValidateSchedulerContent(s, schedulerEmployment, schedulerCareer, activities, s.Employment != null);

            if (s.Employment != null && c.Careers.TryGetValue(s.Employment.CareerId, out var career) &&
                career.Revision == s.Employment.DefinitionRevision)
            {
                var e = s.Employment;
                if (s.Scheduler.Deck.Any(id => c.Careers.Values.Any(definition => definition.Scenes.Any(scene => scene.Id == id)) &&
                    !career.Scenes.Any(scene => scene.Id == id)))
                    throw new ArgumentException("Deck belongs to another career.");
                var scheduled = career.WorksOn(s.Date) && string.CompareOrdinal(s.DateIso, e.FirstShiftIso) >= 0;
                var expectedScenes = 0;
                if (scheduled && s.Minute >= career.StartMinute)
                {
                    var offset = Math.Min(s.Minute, career.EndMinute) - career.StartMinute;
                    var slot = (career.EndMinute - career.StartMinute) / career.ScenesPerShift;
                    if (offset % slot != 0) throw new ArgumentException("Work cursor is not at a committed boundary.");
                    expectedScenes = offset / slot;
                }
                if (e.ScenesToday != expectedScenes || (expectedScenes > 0 ? e.WorkDateIso != s.DateIso : e.WorkDateIso.Length != 0))
                    throw new ArgumentException("Work cursor/reward state is inconsistent.");
                var committedToday = CountCareerActivities(activities, career, ParseDate(e.FirstShiftIso), s.Date);
                if (committedToday.Today != expectedScenes)
                    throw new ArgumentException("Committed work activities do not match today's work cursor.");
            }
            CheckContent(() => BusinessStateValidation.ValidateContent(s, c));
            if (restoreValidator != null) CheckContent(() => restoreValidator.Validate(s, c));
            if (incompatibility != null) throw incompatibility;
        }

        private static IReadOnlyList<ActivityStamp> ValidateStructure(GameState s)
        {
            if (s.SaveVersion != SaveSchema.CurrentVersion || string.IsNullOrWhiteSpace(s.RunId) ||
                s.Revision < 0 || s.NextEntity < 1 || s.NextOperation < 1 || s.Minute < 0 || s.Minute >= 1440 ||
                s.Cash < 0 || s.SchedulerRng == 0 || s.EventRng == 0 || s.RngVersion != "xorshift32-v1" || s.PlaybackCursor < 0)
                throw new ArgumentException("Invalid save root.");
            _ = s.Date;
            BusinessStateValidation.ValidateStructure(s);

            if (s.Name.Length > 0)
            {
                if (s.StartingAge < 18 || s.StartingAge > 40 || s.LearningSpeed <= 0 ||
                    !ContentId.IsValid(s.BackgroundId) || !ContentId.IsValid(s.AppearanceId))
                    throw new ArgumentException("Invalid character.");
                _ = DateTime.ParseExact(s.BirthDateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            Unique(s.Skills.Select(x => x.Id)); Unique(s.Grants); Unique(s.ConsumedActivities);
            Unique(s.Receipts.Select(x => x.CommandId)); Unique(s.Receipts.Select(x => x.OperationId));
            Unique(s.Ledger.Select(x => x.Id)); Unique(s.Claims.Select(x => x.Id)); Unique(s.Arrears.Select(x => x.Id));

            foreach (var skill in s.Skills)
                if (!ContentId.IsValid(skill.Id) || skill.Exposure < 0 || skill.GrantedLevel < 0 || skill.GrantedLevel > 5)
                    throw new ArgumentException("Invalid skill.");

            ValidateSchedulerStructure(s.Scheduler);
            ValidateEmploymentStructure(s.Employment);
            foreach (var employment in s.PreviousEmployment) ValidateEmploymentStructure(employment);
            ValidateCourseStructure(s.Course);
            foreach (var course in s.CompletedCourses) ValidateCourseStructure(course);

            long maxOperation = 0;
            var operationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in s.Receipts)
            {
                if (r.Revision <= 0 || r.Revision > s.Revision || r.MinutesConsumed < 0 || string.IsNullOrEmpty(r.Payload) ||
                    !TryParseRunSequence(s.RunId, "op", r.OperationId, out var sequence))
                    throw new ArgumentException("Invalid receipt.");
                maxOperation = Math.Max(maxOperation, sequence);
                operationIds.Add(r.OperationId);
            }
            if (s.NextOperation <= maxOperation)
                throw new ArgumentException("Operation counter would reuse an issued identity.");

            var activities = new List<ActivityStamp>(s.ConsumedActivities.Count);
            foreach (var activity in s.ConsumedActivities)
            {
                if (!TryParseActivity(s.RunId, activity, out var stamp))
                    throw new ArgumentException("Invalid consumed activity identity.");
                if (stamp.Date > s.Date || (stamp.Date == s.Date && stamp.Minute >= s.Minute))
                    throw new ArgumentException("Consumed activity is ahead of the simulation cursor.");
                activities.Add(stamp);
            }
            if (s.CurrentActivity.Length > 0 && !operationIds.Contains(s.CurrentActivity) && !s.ConsumedActivities.Contains(s.CurrentActivity))
                throw new ArgumentException("Current activity was never committed.");
            ValidateReceiptTimeline(s);
            if (s.Scheduler.Generation > s.ConsumedActivities.Count || s.Scheduler.Cycle > s.ConsumedActivities.Count)
                throw new ArgumentException("Scheduler generated more cycles/rebuilds than committed activities.");

            var entityIds = new HashSet<string>(StringComparer.Ordinal);
            var entitySequences = new Dictionary<long, string>();
            long maxEntity = 0;
            void Reference(string id, string kind)
            {
                if (!TryParseRunSequence(s.RunId, kind, id, out var sequence) ||
                    (entitySequences.TryGetValue(sequence, out var previous) && previous != id))
                    throw new ArgumentException("Invalid or reused entity sequence.");
                entitySequences[sequence] = id; maxEntity = Math.Max(maxEntity, sequence);
            }
            void Issued(string id, string kind)
            {
                if (!entityIds.Add(id))
                    throw new ArgumentException("Invalid or reused entity identity.");
                Reference(id, kind);
            }
            var employmentIds = new HashSet<string>(StringComparer.Ordinal);
            void Employment(EmploymentState? employment)
            {
                if (employment == null) return;
                Issued(employment.InstanceId, "employment");
                Issued(employment.EmployerId, "employer");
                employmentIds.Add(employment.InstanceId);
            }
            Employment(s.Employment);
            foreach (var employment in s.PreviousEmployment) Employment(employment);
            if (s.Course != null) Issued(s.Course.InstanceId, "course");
            foreach (var course in s.CompletedCourses) Issued(course.InstanceId, "course");
            foreach (var business in s.Businesses) Issued(business.InstanceId, "business");
            foreach (var claim in s.Claims) Issued(claim.Id, "claim");
            foreach (var arrear in s.Arrears) Issued(arrear.Id, "arrear");
            foreach (var ledger in s.Ledger) Issued(ledger.Id, "transaction");
            foreach (var ledger in s.Ledger)
            {
                if (ledger.Category == "arrear.settlement") Reference(ledger.AttributionId, "arrear");
                if (ledger.Category == "salary.accrual" && !employmentIds.Contains(ledger.AttributionId))
                    throw new ArgumentException("Salary ledger has no owning employment.");
                foreach (var kind in new[] { "employment", "employer", "course", "business", "claim", "arrear", "transaction" })
                    if (ledger.AttributionId.StartsWith(s.RunId + "/" + kind + "/", StringComparison.Ordinal)) Reference(ledger.AttributionId, kind);
            }
            foreach (var grant in s.Grants)
                if (grant.StartsWith("course/", StringComparison.Ordinal)) Reference(grant.Substring("course/".Length), "course");
            foreach (var entry in s.History)
                if (entry.StartsWith("career.resigned:", StringComparison.Ordinal))
                {
                    var dateSeparator = entry.LastIndexOf(':');
                    if (dateSeparator <= "career.resigned:".Length) throw new ArgumentException("Invalid historical employment identity.");
                    var id = entry.Substring("career.resigned:".Length, dateSeparator - "career.resigned:".Length);
                    Reference(id, "employment");
                    if (!employmentIds.Contains(id)) throw new ArgumentException("Historical resignation has no owning employment.");
                    _ = ParseDate(entry.Substring(dateSeparator + 1));
                }
            if (s.NextEntity <= maxEntity)
                throw new ArgumentException("Entity counter would reuse an issued identity.");

            var earned = RationalAmount.Zero;
            foreach (var claim in s.Claims)
            {
                var numerator = BigInteger.Parse(claim.Numerator, CultureInfo.InvariantCulture);
                var denominator = BigInteger.Parse(claim.Denominator, CultureInfo.InvariantCulture);
                if (numerator < 0 || denominator <= 0 || !employmentIds.Contains(claim.EmploymentId))
                    throw new ArgumentException("Invalid salary fraction.");
                _ = DateTime.ParseExact(claim.EarnedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                earned += new RationalAmount(numerator, denominator);
            }
            _ = earned.FloorToInt64();
            long arrears = 0;
            foreach (var a in s.Arrears)
            {
                if (a.Amount <= 0) throw new ArgumentException("Invalid arrears.");
                _ = DateTime.ParseExact(a.DueIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                arrears = checked(arrears + a.Amount);
            }
            foreach (var l in s.Ledger)
                if (l.Amount < 0 || string.IsNullOrEmpty(l.OperationId) || !operationIds.Contains(l.OperationId))
                    throw new ArgumentException("Invalid ledger.");
            return activities.AsReadOnly();
        }

        public static SimDate ReceiptOrigin(GameState state)
        {
            if (state.Receipts.Count == 0) return state.Date;
            const string prefix = "character.created:";
            var origins = state.History.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            if (origins.Length != 1) throw new ArgumentException("Missing or ambiguous receipt timeline origin.");
            return ParseDate(origins[0].Substring(prefix.Length));
        }

        private static void ValidateReceiptTimeline(GameState state)
        {
            if (state.Receipts.Count != state.Revision) throw new ArgumentException("Committed receipt history is incomplete.");
            var date = ReceiptOrigin(state); var minute = 0; var activityIndex = 0; var currentActivity = "";
            for (var i = 0; i < state.Receipts.Count; i++)
            {
                var receipt = state.Receipts[i];
                if (receipt.Revision != i + 1L) throw new ArgumentException("Receipt revisions are not a committed sequence.");
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                if ((i == 0) != (command.Kind == CommandKind.CreateCharacter))
                    throw new ArgumentException("Character creation does not own the timeline origin.");
                if (i == 0)
                {
                    var expectedBirth = new DateTime(date.Year, date.Month, date.Day).AddYears(-command.Amount)
                        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (state.Name != command.Name.Trim() || state.StartingAge != command.Amount ||
                        state.BirthDateIso != expectedBirth || state.BackgroundId != command.ContentId ||
                        state.AppearanceId != command.AppearanceId)
                        throw new ArgumentException("Character identity differs from its committed creation receipt.");
                }
                if (command.Kind == CommandKind.AdvanceBoundary)
                {
                    var activity = state.RunId + "/activity/" + date + "/" + minute.ToString(CultureInfo.InvariantCulture);
                    if (receipt.MinutesConsumed <= 0 || receipt.MinutesConsumed > 1440 || activityIndex >= state.ConsumedActivities.Count ||
                        state.ConsumedActivities[activityIndex++] != activity)
                        throw new ArgumentException("Consumed activity does not belong to its committed receipt.");
                    currentActivity = activity;
                }
                else
                {
                    if (command.Kind == CommandKind.Study)
                    {
                        if (receipt.MinutesConsumed <= 0 || receipt.MinutesConsumed > command.Amount)
                            throw new ArgumentException("Invalid committed study duration.");
                    }
                    else if (receipt.MinutesConsumed != 0) throw new ArgumentException("Non-time command advanced the timeline.");
                    if (command.Kind != CommandKind.AcknowledgePlayback) currentActivity = receipt.OperationId;
                }
                var nextMinute = checked(minute + receipt.MinutesConsumed);
                date = date.AddDays(nextMinute / 1440); minute = nextMinute % 1440;
            }
            if (activityIndex != state.ConsumedActivities.Count || date != state.Date || minute != state.Minute || currentActivity != state.CurrentActivity)
                throw new ArgumentException("Simulation cursor/activity history differs from committed receipts.");
        }

        private static void ValidateSchedulerStructure(SchedulerState scheduler)
        {
            if (scheduler.Cursor < 0 || scheduler.Cursor > scheduler.Deck.Count || scheduler.Cycle < 0 || scheduler.Generation < 0 ||
                scheduler.Deck.Any(id => !ContentId.IsValid(id)) ||
                (scheduler.LastScene.Length > 0 && !ContentId.IsValid(scheduler.LastScene)))
                throw new ArgumentException("Invalid scheduler.");
            if (scheduler.Deck.Count == 0)
            {
                if (scheduler.Cursor != 0 || scheduler.Cycle != 0 || scheduler.Generation != 0 || scheduler.Signature.Length != 0)
                    throw new ArgumentException("Empty scheduler retains generated-cycle state.");
                return;
            }
            if (scheduler.Cursor == 0 || scheduler.Cycle == 0 || scheduler.Generation < scheduler.Cycle ||
                string.IsNullOrWhiteSpace(scheduler.Signature) || string.IsNullOrWhiteSpace(scheduler.LastScene) ||
                scheduler.LastScene != scheduler.Deck[scheduler.Cursor - 1])
                throw new ArgumentException("Scheduler prefix is not a committed runtime state.");
        }

        private static void ValidateSchedulerContent(GameState state, EmploymentState employment, CareerDefinition career,
            IReadOnlyList<ActivityStamp> activities, bool currentEmployment)
        {
            if (state.Scheduler.Deck.Count == 0)
            {
                if (currentEmployment)
                {
                    var committed = CountCareerActivities(activities, career, ParseDate(employment.FirstShiftIso), state.Date);
                    if (committed.Total != 0) throw new ArgumentException("Scheduler lost committed career scenes.");
                }
                return;
            }
            if (state.Scheduler.Deck.Count != career.QuotaSlots || !career.Scenes.Any(scene => scene.Id == state.Scheduler.LastScene))
                throw new ArgumentException("Scheduler cycle does not match its career definition.");
            if (state.Scheduler.Deck.Any(id => !career.Scenes.Any(scene => scene.Id == id)))
                throw new ArgumentException("Deck belongs to another career.");

            var signatureMatches = false;
            for (var rank = 0; rank <= employment.Rank && rank < career.Ranks.Count; rank++)
            {
                var expected = career.Id + "/" + career.Revision + "/" + rank.ToString(CultureInfo.InvariantCulture);
                if (state.Scheduler.Signature == expected) { signatureMatches = true; break; }
            }
            if (!signatureMatches) throw new ArgumentException("Scheduler signature cannot belong to the current employment transition.");
            // Historical employment has only a date-level EndedIso. An unemployed boundary later on that
            // same date can share an old career slot timestamp, so exact receipt reconstruction owns
            // historical cursor/cycle provenance instead of this timestamp heuristic.
            if (!currentEmployment) return;

            var counts = CountCareerActivities(activities, career, ParseDate(employment.FirstShiftIso), state.Date);
            if (counts.Total <= 0) throw new ArgumentException("Generated scheduler has no committed career scene.");
            var expectedCycle = checked((counts.Total - 1) / career.QuotaSlots + 1);
            var expectedCursor = checked((counts.Total - 1) % career.QuotaSlots + 1);
            if (state.Scheduler.Cycle != expectedCycle || state.Scheduler.Cursor != expectedCursor)
                throw new ArgumentException("Scheduler cursor/cycle does not match committed career activities.");
        }

        private static ActivityCounts CountCareerActivities(IReadOnlyList<ActivityStamp> activities, CareerDefinition career,
            SimDate firstShift, SimDate endDate)
        {
            var total = 0;
            var today = 0;
            var slot = (career.EndMinute - career.StartMinute) / career.ScenesPerShift;
            foreach (var activity in activities)
            {
                if (activity.Date < firstShift || activity.Date > endDate || !career.WorksOn(activity.Date) ||
                    activity.Minute < career.StartMinute || activity.Minute >= career.EndMinute ||
                    (activity.Minute - career.StartMinute) % slot != 0)
                    continue;
                total++;
                if (activity.Date == endDate) today++;
            }
            return new ActivityCounts(total, today);
        }

        private static SimDate EndDate(GameState state, EmploymentState employment) =>
            employment.EndedIso.Length > 0 ? ParseDate(employment.EndedIso) : state.Date;

        private static SimDate ParseDate(string value)
        {
            var date = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new SimDate(date.Year, date.Month, date.Day);
        }

        private static bool TryParseRunSequence(string runId, string kind, string value, out long sequence)
        {
            sequence = 0;
            var prefix = runId + "/" + kind + "/";
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = value.Substring(prefix.Length);
            return suffix.Length > 0 && long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out sequence) &&
                sequence > 0 && suffix == sequence.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseActivity(string runId, string value, out ActivityStamp activity)
        {
            activity = default;
            var prefix = runId + "/activity/";
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = value.Substring(prefix.Length);
            var separator = suffix.LastIndexOf('/');
            if (separator <= 0 || separator == suffix.Length - 1) return false;
            var dateText = suffix.Substring(0, separator);
            var minuteText = suffix.Substring(separator + 1);
            if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !int.TryParse(minuteText, NumberStyles.None, CultureInfo.InvariantCulture, out var minute) || minute < 0 || minute >= 1440 ||
                minuteText != minute.ToString(CultureInfo.InvariantCulture)) return false;
            activity = new ActivityStamp(new SimDate(date.Year, date.Month, date.Day), minute);
            return true;
        }

        private static void Unique(IEnumerable<string> values)
        {
            var items = values.ToArray();
            if (items.Any(string.IsNullOrWhiteSpace) || items.Distinct(StringComparer.Ordinal).Count() != items.Length)
                throw new ArgumentException("Duplicate/empty identity.");
        }

        private static void ValidateEmploymentStructure(EmploymentState? e)
        {
            if (e == null) return;
            if (!ContentId.IsValid(e.CareerId) || string.IsNullOrWhiteSpace(e.DefinitionRevision) || e.Rank < 0 || e.Xp < 0 || e.ScenesToday < 0 ||
                string.IsNullOrWhiteSpace(e.InstanceId) || string.IsNullOrWhiteSpace(e.EmployerId))
                throw new ArgumentException("Invalid employment.");
            _ = DateTime.ParseExact(e.StartedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            _ = DateTime.ParseExact(e.FirstShiftIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (e.EndedIso.Length > 0) _ = DateTime.ParseExact(e.EndedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static void ValidateEmploymentContent(EmploymentState? e, ContentCatalog c)
        {
            if (e == null) return;
            if (!c.Careers.TryGetValue(e.CareerId, out var definition))
                throw new ContentCompatibilityException("save.content_id", "Career definition is unavailable in the active catalog.");
            if (e.DefinitionRevision != definition.Revision)
                throw new ContentCompatibilityException("save.content_revision", "Career revision is unsupported by the active catalog.");
            if (e.Rank >= definition.Ranks.Count || e.ScenesToday > definition.ScenesPerShift)
                throw new ArgumentException("Invalid employment.");
        }

        private static void ValidateCourseStructure(CourseState? course)
        {
            if (course == null) return;
            if (!ContentId.IsValid(course.DefinitionId) || course.ProgressUnits < 0 || string.IsNullOrWhiteSpace(course.InstanceId))
                throw new ArgumentException("Invalid course state.");
        }

        private static void ValidateCourseContent(CourseState? course, ContentCatalog c)
        {
            if (course == null) return;
            if (!c.Courses.TryGetValue(course.DefinitionId, out var definition))
                throw new ContentCompatibilityException("save.content_id", "Course definition is unavailable in the active catalog.");
            if (course.ProgressUnits > checked((long)definition.BaseMinutes * 10000))
                throw new ArgumentException("Invalid course state.");
        }

        private readonly struct ActivityStamp
        {
            public SimDate Date { get; }
            public int Minute { get; }
            public ActivityStamp(SimDate date, int minute) { Date = date; Minute = minute; }
        }

        private readonly struct ActivityCounts
        {
            public int Total { get; }
            public int Today { get; }
            public ActivityCounts(int total, int today) { Total = total; Today = today; }
        }
    }
}

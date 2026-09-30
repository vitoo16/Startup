#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace StartupLife.Core
{
    public static class StateValidation
    {
        public static void Validate(GameState s, ContentCatalog c)
        {
            ValidateStructure(s);
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
                else if (!start.AppearanceIds.Contains(s.AppearanceId))
                    Unsupported("save.content_id", "Character appearance is unavailable in the active catalog.");
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
            }
            if (incompatibility != null) throw incompatibility;
        }

        private static void ValidateStructure(GameState s)
        {
            if (s.SaveVersion != 1 || string.IsNullOrWhiteSpace(s.RunId) ||
                s.Revision < 0 || s.NextEntity < 1 || s.NextOperation < 1 || s.Minute < 0 || s.Minute >= 1440 ||
                s.Cash < 0 || s.SchedulerRng == 0 || s.EventRng == 0 || s.RngVersion != "xorshift32-v1" || s.PlaybackCursor < 0)
                throw new ArgumentException("Invalid save root.");
            _ = s.Date;

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

            if (s.Scheduler.Cursor < 0 || s.Scheduler.Cursor > s.Scheduler.Deck.Count || s.Scheduler.Cycle < 0 || s.Scheduler.Generation < 0 ||
                s.Scheduler.Deck.Any(id => !ContentId.IsValid(id)))
                throw new ArgumentException("Invalid scheduler.");

            ValidateEmploymentStructure(s.Employment);
            foreach (var employment in s.PreviousEmployment) ValidateEmploymentStructure(employment);
            ValidateCourseStructure(s.Course);
            foreach (var course in s.CompletedCourses) ValidateCourseStructure(course);

            var earned = RationalAmount.Zero;
            foreach (var claim in s.Claims)
            {
                var numerator = BigInteger.Parse(claim.Numerator, CultureInfo.InvariantCulture);
                var denominator = BigInteger.Parse(claim.Denominator, CultureInfo.InvariantCulture);
                if (numerator < 0 || denominator <= 0) throw new ArgumentException("Invalid salary fraction.");
                _ = DateTime.ParseExact(claim.EarnedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                earned += new RationalAmount(numerator, denominator);
            }
            _ = earned.FloorToInt64();
            long arrears = 0; foreach (var a in s.Arrears)
            {
                if (a.Amount <= 0) throw new ArgumentException("Invalid arrears.");
                _ = DateTime.ParseExact(a.DueIso, "yyyy-MM-dd", CultureInfo.InvariantCulture); arrears = checked(arrears + a.Amount);
            }
            foreach (var l in s.Ledger) if (l.Amount < 0 || string.IsNullOrEmpty(l.OperationId)) throw new ArgumentException("Invalid ledger.");
            foreach (var r in s.Receipts)
                if (r.Revision <= 0 || r.Revision > s.Revision || r.MinutesConsumed < 0 || string.IsNullOrEmpty(r.Payload)) throw new ArgumentException("Invalid receipt.");
        }

        private static void Unique(System.Collections.Generic.IEnumerable<string> values)
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
    }
}

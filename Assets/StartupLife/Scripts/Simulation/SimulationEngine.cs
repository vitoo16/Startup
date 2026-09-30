#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    public sealed class SimulationEngine
    {
        private readonly ContentCatalog content;
        public SimulationEngine(ContentCatalog catalog) { content = catalog; }
        public int Evaluate(GameState candidate, GameCommand command, string operationId)
        {
            if (candidate.PendingChoiceId.Length > 0 && command.Kind == CommandKind.AdvanceBoundary) Fail("choice.required");
            if (command.Kind != CommandKind.CreateCharacter && candidate.Name.Length == 0) Fail("character.required");
            if (command.Kind != CommandKind.AcknowledgePlayback) candidate.CurrentCue = "";
            switch (command.Kind)
            {
                case CommandKind.CreateCharacter: Create(candidate, command, operationId); break;
                case CommandKind.AcceptJob: Accept(candidate, command.ContentId); break;
                case CommandKind.PurchaseCourse: Purchase(candidate, command.ContentId, operationId); break;
                case CommandKind.Study: return Study(candidate, command.Amount);
                case CommandKind.AdvanceBoundary: return AdvanceBoundary(candidate, operationId);
                case CommandKind.Resign: Resign(candidate); break;
                case CommandKind.AcknowledgePlayback:
                    if (command.ContentId != candidate.CurrentActivity || command.Amount < candidate.PlaybackCursor) Fail("playback.stale");
                    candidate.PlaybackCursor = command.Amount; break;
                default: Fail("command.unsupported"); break;
            }
            return 0;
        }
        private void Create(GameState state, GameCommand command, string operation)
        {
            if (state.Name.Length > 0) Fail("character.exists");
            if (command.Amount < 18 || command.Amount > 40 || string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 80) Fail("character.invalid");
            if (!content.Starts.TryGetValue(command.ContentId, out var start)) Fail("content.missing");
            if (!start!.AppearanceIds.Contains(command.AppearanceId)) Fail("appearance.invalid");
            state.Name = command.Name.Trim(); state.StartingAge = command.Amount;
            var birthday = new DateTime(state.Date.Year, state.Date.Month, state.Date.Day).AddYears(-command.Amount);
            state.BirthDateIso = birthday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            state.BackgroundId = start.Id; state.AppearanceId = command.AppearanceId; state.LearningSpeed = start.LearningSpeed;
            state.Cash = start.Cash;
            state.Skills = content.Skills.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(x => new SkillState { Id = x }).ToList();
            state.History.Add("character.created:" + state.DateIso); state.CurrentCue = "character.created";
            SettleNewDate(state, operation);
        }
        private void Accept(GameState state, string id)
        {
            if (state.Employment != null) Fail("career.already_employed");
            if (!content.Careers.TryGetValue(id, out var career)) Fail("content.missing");
            var first = state.Date;
            if (state.Minute > career!.StartMinute) first = first.AddDays(1);
            while (!career.WorksOn(first)) first = first.AddDays(1);
            state.Employment = new EmploymentState { InstanceId = state.NewEntity("employment"), EmployerId = state.NewEntity("employer"),
                CareerId = id, DefinitionRevision = career.Revision, StartedIso = state.DateIso, FirstShiftIso = first.ToString() };
            state.Scheduler = new SchedulerState { LastScene = state.Scheduler.LastScene };
            state.History.Add("career.accepted:" + id + ":" + state.DateIso); state.CurrentCue = "career.accepted";
            Promote(state);
        }
        private void Resign(GameState state)
        {
            if (state.Employment == null) Fail("career.not_employed");
            state.Employment!.EndedIso = state.DateIso; state.PreviousEmployment.Add(state.Employment);
            state.History.Add("career.resigned:" + state.Employment.InstanceId + ":" + state.DateIso);
            state.Employment = null; state.CurrentCue = "career.resigned";
        }
        private void Purchase(GameState state, string id, string operation)
        {
            if (state.Course != null) Fail("course.already_active");
            if (!content.Courses.TryGetValue(id, out var course)) Fail("content.missing");
            var level = Level(state, course!.SkillId);
            if (level < course.PrerequisiteLevel) Fail("course.prerequisite");
            if (level >= course.TargetLevel) Fail("course.target_met");
            if (state.Arrears.Count > 0) Fail("economy.arrears");
            if (state.Cash < course.Price) Fail("economy.insufficient_cash");
            state.Cash = checked(state.Cash - course.Price);
            Entry(state, operation, "course.purchase", -course.Price, course.Price, id);
            state.Course = new CourseState { InstanceId = state.NewEntity("course"), DefinitionId = id };
            state.History.Add("course.purchased:" + id); state.CurrentCue = "course.purchased";
        }
        private int Study(GameState state, int requested)
        {
            if (requested <= 0) Fail("time.invalid_minutes");
            CompleteObsoleteCourse(state);
            if (state.Course == null) Fail("course.not_active");
            var course = content.Courses[state.Course!.DefinitionId]; var available = FreeMinutes(state);
            if (available <= 0) Fail("time.unavailable");
            var target = checked((long)course.BaseMinutes * 10000);
            var remaining = target - state.Course.ProgressUnits;
            var needed = (remaining - 1) / state.LearningSpeed + 1;
            var minutes = (int)Math.Min(Math.Min((long)requested, available), needed);
            state.Course.ProgressUnits = Math.Min(target, checked(state.Course.ProgressUnits + checked((long)minutes * state.LearningSpeed)));
            state.Minute += minutes; state.CurrentCue = "course.study";
            if (state.Course.ProgressUnits >= target) FinishCourse(state);
            return minutes;
        }
        public int FreeMinutes(GameState state)
        {
            if (state.Minute < content.Schedule.WakeMinute || state.Minute >= content.Schedule.SleepMinute) return 0;
            var career = WorkingCareer(state);
            if (career == null) return content.Schedule.SleepMinute - state.Minute;
            if (state.Minute < career.StartMinute) return career.StartMinute - state.Minute;
            if (state.Minute < career.EndMinute) return 0;
            return content.Schedule.SleepMinute - state.Minute;
        }
        private CareerDefinition? WorkingCareer(GameState state)
        {
            var e = state.Employment;
            if (e == null || string.CompareOrdinal(state.DateIso, e.FirstShiftIso) < 0) return null;
            var career = content.Careers[e.CareerId]; return career.WorksOn(state.Date) ? career : null;
        }
        public int AdvanceBoundary(GameState state, string operation)
        {
            var start = state.Minute;
            var activity = state.RunId + "/activity/" + state.DateIso + "/" + start.ToString(CultureInfo.InvariantCulture);
            if (state.ConsumedActivities.Contains(activity)) Fail("activity.already_consumed");
            var career = WorkingCareer(state);
            if (start < content.Schedule.WakeMinute) state.Minute = content.Schedule.WakeMinute;
            else if (career != null && start < career.StartMinute) state.Minute = career.StartMinute;
            else if (career != null && start < career.EndMinute) ApplyCareerScene(state, career, operation);
            else if (start < content.Schedule.SleepMinute) state.Minute = content.Schedule.SleepMinute;
            else
            {
                state.DateIso = state.Date.AddDays(1).ToString(); state.Minute = 0;
                if (state.Employment != null) { state.Employment.ScenesToday = 0; state.Employment.WorkDateIso = ""; }
                SettleNewDate(state, operation); Promote(state); state.CurrentCue = "day.completed";
            }
            state.ConsumedActivities.Add(activity); state.CurrentActivity = activity; state.PlaybackCursor = 0;
            return state.Minute == 0 ? 1440 - start : state.Minute - start;
        }
        public void ApplyCareerScene(GameState state, CareerDefinition career, string operation)
        {
            var e = state.Employment!;
            var slotMinutes = (career.EndMinute - career.StartMinute) / career.ScenesPerShift;
            if ((state.Minute - career.StartMinute) % slotMinutes != 0) Fail("activity.invalid_boundary");
            var selected = QuotaScheduler.Consume(state, career); var scene = career.Scenes.Single(x => x.Id == selected);
            e.Xp = checked(e.Xp + scene.CareerXp);
            var skill = state.Skills.Single(x => x.Id == scene.SkillId); skill.Exposure = checked(skill.Exposure + scene.Exposure);
            e.ScenesToday++; e.WorkDateIso = state.DateIso; state.Minute += slotMinutes; state.CurrentCue = selected;
            if (state.Minute == career.EndMinute && e.ScenesToday == career.ScenesPerShift)
            {
                var salary = career.Ranks[e.Rank].MonthlySalary; var denominator = ScheduledDays(career, state.Date);
                var amount = new RationalAmount(salary, denominator);
                state.Claims.Add(new SalaryClaim { Id = state.NewEntity("claim"), EmploymentId = e.InstanceId, EarnedIso = state.DateIso,
                    Numerator = amount.Numerator.ToString(CultureInfo.InvariantCulture), Denominator = amount.Denominator.ToString(CultureInfo.InvariantCulture) });
                Entry(state, operation, "salary.accrual", 0, amount.FloorToInt64(), e.InstanceId);
            }
            Promote(state); CompleteObsoleteCourse(state);
        }
        public static int ScheduledDays(CareerDefinition career, SimDate month)
        {
            var count = 0;
            for (var day = 1; day <= month.DaysInMonth; day++) if (career.WorksOn(new SimDate(month.Year, month.Month, day))) count++;
            return count;
        }
        private void Promote(GameState state)
        {
            var e = state.Employment; if (e == null) return;
            var career = content.Careers[e.CareerId];
            var started = DateTime.ParseExact(e.StartedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var today = DateTime.ParseExact(state.DateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var days = (today - started).Days;
            for (var i = e.Rank; i < career.Ranks.Count; i++)
            {
                var rank = career.Ranks[i]; if (e.Xp < rank.RequiredXp || days < rank.RequiredServiceDays) break;
                if (i > e.Rank) { e.Rank = i; state.History.Add("career.promoted:" + rank.Id); }
                Grant(state, "career/" + career.Id + "/" + rank.Id, rank.GrantSkillId, rank.GrantLevel);
            }
            CompleteObsoleteCourse(state);
        }
        private void Grant(GameState state, string id, string skillId, int level)
        {
            if (state.Grants.Contains(id)) return;
            var skill = state.Skills.Single(x => x.Id == skillId); skill.GrantedLevel = Math.Max(skill.GrantedLevel, level);
            state.Grants.Add(id);
        }
        private int Level(GameState state, string id) => GameSnapshot.Level(state.Skills.Single(x => x.Id == id), content.Skills[id]);
        private void CompleteObsoleteCourse(GameState state)
        {
            if (state.Course == null) return; var c = content.Courses[state.Course.DefinitionId];
            if (Level(state, c.SkillId) >= c.TargetLevel)
            {
                state.Course.ProgressUnits = checked((long)c.BaseMinutes * 10000); state.Course.Completed = true;
                state.CompletedCourses.Add(state.Course); state.Course = null; state.History.Add("course.target_already_met");
            }
        }
        private void FinishCourse(GameState state)
        {
            var course = state.Course!; var definition = content.Courses[course.DefinitionId];
            Grant(state, "course/" + course.InstanceId, definition.SkillId, definition.TargetLevel);
            course.Completed = true; state.CompletedCourses.Add(course); state.Course = null; state.CurrentCue = "course.completed";
            state.History.Add("course.completed:" + definition.Id);
        }
        private void SettleNewDate(GameState state, string operation)
        {
            var date = state.Date;
            if (date.Day == Math.Min(content.Economy.Payday, date.DaysInMonth))
            {
                var due = state.Claims.Where(x => string.CompareOrdinal(x.EarnedIso, state.DateIso) < 0).ToList();
                var total = RationalAmount.Zero;
                foreach (var claim in due) total += ClaimAmount(claim);
                var whole = total.FloorToInt64();
                if (whole > 0)
                {
                    var unpaid = RationalAmount.FromWhole(whole);
                    foreach (var claim in due) // stable persisted accrual order; no remainder lost after resignation
                    {
                        var amount = ClaimAmount(claim);
                        var consumed = amount.Numerator * unpaid.Denominator <= unpaid.Numerator * amount.Denominator ? amount : unpaid;
                        amount -= consumed; unpaid -= consumed;
                        claim.Numerator = amount.Numerator.ToString(CultureInfo.InvariantCulture); claim.Denominator = amount.Denominator.ToString(CultureInfo.InvariantCulture);
                        if (unpaid.IsZero) break;
                    }
                    state.Claims.RemoveAll(x => ClaimAmount(x).IsZero); Income(state, whole, operation);
                }
            }
            if (date.Day == Math.Min(content.Economy.ExpenseDay, date.DaysInMonth))
            {
                var cost = content.Economy.LivingCost; var paid = Math.Min(state.Cash, cost); state.Cash -= paid;
                Entry(state, operation, "living.expense", -paid, cost, "personal");
                if (paid < cost) state.Arrears.Add(new ArrearState { Id = state.NewEntity("arrear"), DueIso = state.DateIso, Amount = cost - paid });
            }
        }
        private void Income(GameState state, long amount, string operation)
        {
            var remaining = amount;
            // OrderBy is stable: persisted creation order breaks equal due-date ties, not lexicographic entity numbers.
            foreach (var arrear in state.Arrears.OrderBy(x => x.DueIso, StringComparer.Ordinal))
            {
                var paid = Math.Min(remaining, arrear.Amount); arrear.Amount -= paid; remaining -= paid;
                if (paid > 0) Entry(state, operation, "arrear.settlement", 0, paid, arrear.Id);
                if (remaining == 0) break;
            }
            state.Arrears.RemoveAll(x => x.Amount == 0); state.Cash = checked(state.Cash + remaining);
            Entry(state, operation, "salary.payment", remaining, amount, "personal");
        }
        public static RationalAmount ClaimAmount(SalaryClaim claim) => new RationalAmount(BigInteger.Parse(claim.Numerator, CultureInfo.InvariantCulture), BigInteger.Parse(claim.Denominator, CultureInfo.InvariantCulture));
        private static void Entry(GameState state, string operation, string category, long cashDelta, long amount, string attribution) =>
            state.Ledger.Add(new LedgerEntry { Id = state.NewEntity("transaction"), OperationId = operation, Category = category, CashDelta = cashDelta, Amount = amount, AttributionId = attribution });
        private static void Fail(string key) => throw new RuleFailure(key);
    }
}

#nullable enable
using System;
using System.Collections.Generic;
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
                case CommandKind.LaunchBusiness: LaunchBusiness(candidate, command, operationId); break;
                case CommandKind.ReinvestBusiness: ReinvestBusiness(candidate, command, operationId); break;
                case CommandKind.SetBusinessPricing: SetBusinessPricing(candidate, command, operationId); break;
                case CommandKind.CloseBusiness: CloseBusiness(candidate, command, operationId); break;
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
            var limit = PromotionLimit(e, career, state.Date);
            for (var i = e.Rank; i < limit; i++)
            {
                var rank = career.Ranks[i];
                if (i > e.Rank) { e.Rank = i; state.History.Add("career.promoted:" + rank.Id); }
                Grant(state, "career/" + career.Id + "/" + rank.Id, rank.GrantSkillId, rank.GrantLevel);
            }
            CompleteObsoleteCourse(state);
        }
        internal static int PromotionLimit(EmploymentState employment, CareerDefinition career, SimDate date)
        {
            var started = DateTime.ParseExact(employment.StartedIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var today = DateTime.ParseExact(date.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var days = (today - started).Days; var limit = employment.Rank;
            while (limit < career.Ranks.Count && employment.Xp >= career.Ranks[limit].RequiredXp && days >= career.Ranks[limit].RequiredServiceDays) limit++;
            return limit;
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
        private void LaunchBusiness(GameState state, GameCommand command, string operation)
        {
            if (!ContentId.IsValid(command.ContentId) || command.Name.Length != 0 || string.IsNullOrWhiteSpace(command.AppearanceId))
                Fail("business.invalid_payload");
            if (!content.Businesses.TryGetValue(command.ContentId, out var definition)) Fail("content.missing");
            if (definition!.Revision != command.AppearanceId) Fail("business.definition_revision");
            if (state.Businesses.Count(x => x.IsActive) >= 4) Fail("business.active_limit");
            if (state.Businesses.Where(x => x.IsActive).Any(x =>
                content.Businesses.TryGetValue(x.DefinitionId, out var existing) && existing.Type == definition.Type))
                Fail("business.type_active");
            if (state.Arrears.Count > 0) Fail("economy.arrears");
            if (command.Amount < definition.MinimumStartupInvestment || command.Amount > definition.MaximumStartupInvestment)
                Fail("business.invalid_investment");
            if (state.Cash < command.Amount) Fail("economy.insufficient_cash");
            if (definition.OperationMode == BusinessOperationMode.ManagerOperable) Fail("business.manager_unsupported");
            if (state.Employment != null && definition.OperationMode == BusinessOperationMode.FullTimeRequired)
                Fail("business.employment_incompatible");

            _ = checked(state.Cash - command.Amount);
            _ = checked(state.NextEntity + 2);
            var business = new BusinessState
            {
                InstanceId = state.NewEntity("business"),
                DefinitionId = definition.Id,
                DefinitionRevision = definition.Revision,
                OpenedIso = state.DateIso,
                OpenedMinute = state.Minute,
                PricingPosture = definition.DefaultPricingPosture,
                InitialInvestment = command.Amount,
                ReinvestedAmount = 0
            };
            state.Cash = checked(state.Cash - command.Amount);
            state.Businesses.Add(business);
            Entry(state, operation, "business.launch", -checked((long)command.Amount), command.Amount, business.InstanceId);
            state.History.Add(BusinessHistory.Encode("business.launched", operation, business.InstanceId, state.DateIso,
                state.Minute, command.Amount, business.PricingPosture));
            state.CurrentCue = "business.launched";
        }

        private void ReinvestBusiness(GameState state, GameCommand command, string operation)
        {
            if (string.IsNullOrWhiteSpace(command.ContentId) || command.Name.Length != 0 || command.AppearanceId.Length != 0)
                Fail("business.invalid_payload");
            if (command.Amount <= 0) Fail("business.invalid_reinvestment");
            var business = state.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
            if (business == null) Fail("business.not_found");
            if (!business!.IsActive) Fail("business.closed");
            if (!content.Businesses.TryGetValue(business.DefinitionId, out var definition) ||
                definition!.Revision != business.DefinitionRevision) Fail("business.definition_revision");
            if (command.Amount < definition.MinimumReinvestment || command.Amount > definition.MaximumReinvestment)
                Fail("business.invalid_reinvestment");
            if (state.Arrears.Count > 0) Fail("economy.arrears");
            if (state.Cash < command.Amount) Fail("economy.insufficient_cash");

            var reinvested = checked(business.ReinvestedAmount + command.Amount);
            _ = checked(business.InitialInvestment + reinvested);
            _ = checked(state.Cash - command.Amount);
            _ = checked(state.NextEntity + 1);
            state.Cash = checked(state.Cash - command.Amount);
            business.ReinvestedAmount = reinvested;
            Entry(state, operation, "business.reinvest", -checked((long)command.Amount), command.Amount, business.InstanceId);
            state.History.Add(BusinessHistory.Encode("business.reinvested", operation, business.InstanceId, state.DateIso,
                state.Minute, command.Amount, business.PricingPosture));
            state.CurrentCue = "business.reinvested";
        }

        private void SetBusinessPricing(GameState state, GameCommand command, string operation)
        {
            if (string.IsNullOrWhiteSpace(command.ContentId) || command.Name.Length != 0 || command.AppearanceId.Length != 0)
                Fail("business.invalid_payload");
            if (!Enum.IsDefined(typeof(PricingPosture), command.Amount)) Fail("business.invalid_payload");
            var business = state.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
            if (business == null) Fail("business.not_found");
            if (!business!.IsActive) Fail("business.closed");
            if (!content.Businesses.TryGetValue(business.DefinitionId, out var definition) ||
                definition!.Revision != business.DefinitionRevision) Fail("business.definition_revision");
            var pricing = (PricingPosture)command.Amount;
            if (!definition.AllowedPricingPostures.Contains(pricing)) Fail("business.invalid_payload");
            if (business.PricingPosture == pricing) Fail("business.pricing_unchanged");
            business.PricingPosture = pricing;
            state.History.Add(BusinessHistory.Encode("business.pricing_changed", operation, business.InstanceId, state.DateIso,
                state.Minute, 0, pricing));
            state.CurrentCue = "business.pricing_changed";
        }

        private void CloseBusiness(GameState state, GameCommand command, string operation)
        {
            if (string.IsNullOrWhiteSpace(command.ContentId) || command.Amount != 0 ||
                command.Name.Length != 0 || command.AppearanceId.Length != 0)
                Fail("business.invalid_payload");
            var business = state.Businesses.SingleOrDefault(x => x.InstanceId == command.ContentId);
            if (business == null) Fail("business.not_found");
            if (!business!.IsActive) Fail("business.closed");
            if (!content.Businesses.TryGetValue(business.DefinitionId, out var definition) ||
                definition!.Revision != business.DefinitionRevision) Fail("business.definition_revision");
            business.ClosedIso = state.DateIso;
            business.ClosedMinute = state.Minute;
            state.History.Add(BusinessHistory.Encode("business.closed", operation, business.InstanceId, state.DateIso,
                state.Minute, 0, business.PricingPosture));
            state.CurrentCue = "business.closed";
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

    // Reconstruct a detached candidate with the runtime rules. Never rebuild or publish the saved state.
    public sealed class SimulationRestoreValidator : IRestoreStateValidator
    {
        public void Validate(GameState state, ContentCatalog content)
        {
            ValidateKnownScheduler(state, content);
            // Unrelated unavailable definitions do not suppress the scheduler checks above.
            if (!FullContentAvailable(state, content)) return;
            var events = DeterministicRng.Seed(state.Seed, "events");
            var scheduler = DeterministicRng.Seed(state.Seed, "scheduler");
            if (events == scheduler) { events = unchecked(events + 1); if (events == 0) events = 1; }
            var replay = new GameState { ContentVersion = content.Version, RunId = state.RunId, Seed = state.Seed,
                DateIso = StateValidation.ReceiptOrigin(state).ToString(), SchedulerRng = scheduler, EventRng = events };
            var engine = new SimulationEngine(content);
            foreach (var receipt in state.Receipts)
            {
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                Require(replay.NewOperation() == receipt.OperationId, "operation identity");
                if (command.Kind == CommandKind.AdvanceBoundary && replay.Employment != null)
                {
                    var employment = replay.Employment;
                    var career = content.Careers[employment.CareerId];
                    if (career.WorksOn(replay.Date) && string.CompareOrdinal(replay.DateIso, employment.FirstShiftIso) >= 0 &&
                        replay.Minute >= career.StartMinute && replay.Minute < career.EndMinute)
                    {
                        if (!ContentId.IsValid(receipt.Cue)) throw new ArgumentException("Invalid committed scene identity.");
                        if (!content.Careers.Values.Any(x => x.Scenes.Any(scene => scene.Id == receipt.Cue)))
                            throw new ContentCompatibilityException("save.content_id", "Committed career scene is unavailable in the active catalog.");
                        Require(career.Scenes.Any(scene => scene.Id == receipt.Cue), "committed scene career");
                    }
                }
                int minutes;
                try { minutes = engine.Evaluate(replay, command, receipt.OperationId); }
                catch (RuleFailure error) { throw new ArgumentException("Receipt cannot represent a committed command: " + error.ReasonKey, error); }
                Require(minutes == receipt.MinutesConsumed, "committed duration");
                Require(receipt.Cue == replay.CurrentCue, "committed cue");
                if (command.Kind != CommandKind.AdvanceBoundary && command.Kind != CommandKind.AcknowledgePlayback)
                { replay.CurrentActivity = receipt.OperationId; replay.PlaybackCursor = 0; }
                replay.Revision = receipt.Revision;
            }
            Require(state.DateIso == replay.DateIso && state.Minute == replay.Minute && state.CurrentActivity == replay.CurrentActivity,
                "activity cursor");
            Require(state.CurrentCue == replay.CurrentCue && state.PlaybackCursor == replay.PlaybackCursor,
                "playback provenance");
            Require(state.Name == replay.Name && state.StartingAge == replay.StartingAge && state.BirthDateIso == replay.BirthDateIso &&
                state.BackgroundId == replay.BackgroundId && state.AppearanceId == replay.AppearanceId &&
                state.LearningSpeed == replay.LearningSpeed, "character provenance");
            Require(state.Cash == replay.Cash, "cash provenance");
            Require(state.NextOperation == replay.NextOperation && state.NextEntity == replay.NextEntity, "identity counters");
            var actual = state.Scheduler; var expected = replay.Scheduler;
            Require(actual.Deck.SequenceEqual(expected.Deck) && actual.Cursor == expected.Cursor && actual.Cycle == expected.Cycle &&
                actual.Generation == expected.Generation && actual.Signature == expected.Signature && actual.LastScene == expected.LastScene &&
                state.SchedulerRng == replay.SchedulerRng, "deterministic scheduler");
            Require(EmploymentEquals(state.Employment, replay.Employment) && Same(state.PreviousEmployment, replay.PreviousEmployment, EmploymentEquals),
                "employment provenance");
            Require(Same(state.Skills, replay.Skills, (a, b) => a.Id == b.Id && a.Exposure == b.Exposure && a.GrantedLevel == b.GrantedLevel) &&
                state.Grants.SequenceEqual(replay.Grants), "committed skill rewards");
            Require(CourseEquals(state.Course, replay.Course) && Same(state.CompletedCourses, replay.CompletedCourses, CourseEquals), "course provenance");
            Require(Same(state.Claims, replay.Claims, (a, b) => a.Id == b.Id && a.EmploymentId == b.EmploymentId && a.EarnedIso == b.EarnedIso &&
                a.Numerator == b.Numerator && a.Denominator == b.Denominator), "salary provenance");
            Require(Same(state.Arrears, replay.Arrears, (a, b) => a.Id == b.Id && a.DueIso == b.DueIso && a.Amount == b.Amount), "arrear provenance");
            Require(Same(state.Ledger, replay.Ledger, (a, b) => a.Id == b.Id && a.OperationId == b.OperationId && a.Category == b.Category &&
                a.AttributionId == b.AttributionId && a.Amount == b.Amount && a.CashDelta == b.CashDelta), "ledger provenance");
            Require(state.History.SequenceEqual(replay.History), "committed history");
            // PendingChoiceId, event RNG and presentation fields have separate contracts; no new M1 rules here.
        }
        private static bool FullContentAvailable(GameState state, ContentCatalog content) =>
            (state.Name.Length == 0 || (content.Starts.TryGetValue(state.BackgroundId, out var start) && start.AppearanceIds.Contains(state.AppearanceId))) &&
            state.Skills.All(x => content.Skills.ContainsKey(x.Id)) &&
            state.PreviousEmployment.Concat(state.Employment == null ? Array.Empty<EmploymentState>() : new[] { state.Employment }).All(x =>
                content.Careers.TryGetValue(x.CareerId, out var career) && career.Revision == x.DefinitionRevision) &&
            state.CompletedCourses.Concat(state.Course == null ? Array.Empty<CourseState>() : new[] { state.Course }).All(x => content.Courses.ContainsKey(x.DefinitionId));

        private static void ValidateKnownScheduler(GameState state, ContentCatalog content)
        {
            var jobs = state.PreviousEmployment.Concat(state.Employment == null ? Array.Empty<EmploymentState>() : new[] { state.Employment }).ToArray();
            var trace = new GameState { RunId = state.RunId, DateIso = StateValidation.ReceiptOrigin(state).ToString(),
                SchedulerRng = DeterministicRng.Seed(state.Seed, "scheduler") };
            var jobIndex = 0;
            var engine = new SimulationEngine(content);
            foreach (var receipt in state.Receipts)
            {
                var command = GameCommand.ParseCanonicalPayload(receipt.Payload);
                if (command.Kind == CommandKind.AcceptJob)
                {
                    Require(trace.Employment == null && jobIndex < jobs.Length, "employment receipt sequence");
                    var saved = jobs[jobIndex++];
                    Require(saved.CareerId == command.ContentId && saved.StartedIso == trace.DateIso, "employment start");
                    if (!content.Careers.TryGetValue(saved.CareerId, out var career) || career.Revision != saved.DefinitionRevision) return;
                    var first = trace.Date;
                    if (trace.Minute > career.StartMinute) first = first.AddDays(1);
                    while (!career.WorksOn(first)) first = first.AddDays(1);
                    trace.Employment = new EmploymentState { InstanceId = saved.InstanceId, EmployerId = saved.EmployerId,
                        CareerId = career.Id, DefinitionRevision = career.Revision, StartedIso = trace.DateIso, FirstShiftIso = first.ToString() };
                    trace.Scheduler = new SchedulerState { LastScene = trace.Scheduler.LastScene };
                    PromoteTrace(trace, career);
                }
                else if (command.Kind == CommandKind.Resign)
                {
                    Require(trace.Employment != null, "resignation receipt");
                    trace.Employment!.EndedIso = trace.DateIso; trace.PreviousEmployment.Add(trace.Employment); trace.Employment = null;
                }
                else if (command.Kind == CommandKind.AdvanceBoundary)
                {
                    var employment = trace.Employment;
                    var career = employment == null ? null : content.Careers[employment.CareerId];
                    var working = career != null && career.WorksOn(trace.Date) && string.CompareOrdinal(trace.DateIso, employment!.FirstShiftIso) >= 0;
                    int duration;
                    if (trace.Minute < content.Schedule.WakeMinute) duration = content.Schedule.WakeMinute - trace.Minute;
                    else if (working && trace.Minute < career!.StartMinute) duration = career.StartMinute - trace.Minute;
                    else if (working && trace.Minute < career!.EndMinute)
                    {
                        if (!ContentId.IsValid(receipt.Cue)) throw new ArgumentException("Invalid committed scene identity.");
                        if (!content.Careers.Values.Any(x => x.Scenes.Any(scene => scene.Id == receipt.Cue)))
                            throw new ContentCompatibilityException("save.content_id", "Committed scheduler content is unavailable.");
                        Require(career.Scenes.Any(x => x.Id == receipt.Cue), "committed scene career");
                        Require(QuotaScheduler.Consume(trace, career) == receipt.Cue, "deterministic committed scene");
                        var scene = career.Scenes.Single(x => x.Id == receipt.Cue);
                        employment!.Xp = checked(employment.Xp + scene.CareerXp); employment.ScenesToday++; employment.WorkDateIso = trace.DateIso;
                        PromoteTrace(trace, career); duration = (career.EndMinute - career.StartMinute) / career.ScenesPerShift;
                    }
                    else duration = trace.Minute < content.Schedule.SleepMinute ? content.Schedule.SleepMinute - trace.Minute : 1440 - trace.Minute;
                    Require(receipt.MinutesConsumed == duration, "committed work boundary");
                }
                else if (command.Kind == CommandKind.Study)
                    Require(receipt.MinutesConsumed <= engine.FreeMinutes(trace), "eligible study time");

                var next = checked(trace.Minute + receipt.MinutesConsumed);
                trace.DateIso = trace.Date.AddDays(next / 1440).ToString(); trace.Minute = next % 1440;
                if (next >= 1440 && trace.Employment != null)
                {
                    trace.Employment.ScenesToday = 0; trace.Employment.WorkDateIso = "";
                    PromoteTrace(trace, content.Careers[trace.Employment.CareerId]);
                }
            }
            Require(jobIndex == jobs.Length, "employment receipt history");
            Require(EmploymentEquals(state.Employment, trace.Employment) && Same(state.PreviousEmployment, trace.PreviousEmployment, EmploymentEquals), "known employment history");
            var actual = state.Scheduler; var expected = trace.Scheduler;
            Require(actual.Deck.SequenceEqual(expected.Deck) && actual.Cursor == expected.Cursor && actual.Cycle == expected.Cycle &&
                actual.Generation == expected.Generation && actual.Signature == expected.Signature && actual.LastScene == expected.LastScene &&
                state.SchedulerRng == trace.SchedulerRng, "known deterministic scheduler");
        }
        private static void PromoteTrace(GameState trace, CareerDefinition career)
        {
            var employment = trace.Employment!;
            employment.Rank = Math.Max(employment.Rank, SimulationEngine.PromotionLimit(employment, career, trace.Date) - 1);
        }
        private static void Require(bool value, string field)
        { if (!value) throw new ArgumentException("Checkpoint differs from committed " + field + "."); }
        private static bool Same<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected, Func<T, T, bool> equal)
        { return actual.Count == expected.Count && actual.Zip(expected, equal).All(x => x); }
        private static bool EmploymentEquals(EmploymentState? a, EmploymentState? b) =>
            a == null || b == null ? a == b : a.InstanceId == b.InstanceId && a.EmployerId == b.EmployerId && a.CareerId == b.CareerId &&
            a.DefinitionRevision == b.DefinitionRevision && a.StartedIso == b.StartedIso && a.EndedIso == b.EndedIso && a.FirstShiftIso == b.FirstShiftIso &&
            a.Xp == b.Xp && a.Rank == b.Rank && a.ScenesToday == b.ScenesToday && a.WorkDateIso == b.WorkDateIso;
        private static bool CourseEquals(CourseState? a, CourseState? b) =>
            a == null || b == null ? a == b : a.InstanceId == b.InstanceId && a.DefinitionId == b.DefinitionId && a.ProgressUnits == b.ProgressUnits && a.Completed == b.Completed;
    }
}

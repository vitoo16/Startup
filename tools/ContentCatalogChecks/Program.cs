#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Content;
using StartupLife.Core;
using StartupLife.Simulation;

internal static class Program
{
    private static readonly List<object> Results = new();
    private static int passed;

    private static int Main(string[] args)
    {
        Check("first playable template builds expected immutable catalog", FirstPlayableBuilds);
        Check("M15 narrative events have unique immutable content and valid policies", EventCatalogValid);
        Check("M15 story eligibility uses authoritative tag/stage combinations", EventStoryEligibility);
        Check("M15 authored event chains replay exactly and require accepted choices", EventStoryChainReplay);
        Check("M15 course event choice unlocks deterministic cold replay follow-up", CourseEventStoryReplay);
        Check("duplicate content ids reject at conversion boundary", DuplicateIdsReject);
        Check("missing career skill reference rejects", MissingCareerSkillRejects);
        Check("missing course skill reference rejects", MissingCourseSkillRejects);
        Check("invalid appearance id rejects", InvalidAppearanceRejects);
        Check("career outside day schedule rejects", CareerOutsideScheduleRejects);
        Check("built catalog is detached from mutable source arrays", BuiltCatalogIsDetached);
        Check("null authoring entries reject explicitly", NullEntryRejects);

        Check("M8-T02 authored business definitions build with functional schedules", AuthoredBusinesses);
        Check("M8-T02 business authoring rejects missing or malformed requirements", InvalidBusinessAuthoring);
        Check("M8-T02 business authoring defensive copy", DetachedBusinessAuthoring);
        Check("M8-T02 business source collection and duplicate id validation", BusinessSourceCollection);

        Check("M8-T02 production content evaluates Developer Freelance and kiosk", ProductionBusinessEligibility);

        var reportPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(Path.GetTempPath(), "StartupLifeContentChecks", "report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            suite = "Startup Life content catalog bridge — .NET, not Unity",
            runtime = Environment.Version.ToString(),
            passed,
            failed = Results.Count - passed,
            tests = Results
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"{passed}/{Results.Count} passed. Report: {reportPath}");
        return passed == Results.Count ? 0 : 1;
    }

    private static void EventCatalogValid()
    {
        var definitions = FirstPlayableEventContent.Definitions;
        var policies = FirstPlayableEventContent.Policies;
        // Live M15 authoring evolved beyond the first released V4 roster.
        // Keep an exact live count AND assert the historical snapshot rather
        // than weakening this gate to an arbitrary lower bound.
        Equal(35, definitions.Count);
        var baseline = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
        Equal(20, baseline.Definitions.Count);
        Equal(40, baseline.Definitions.Sum(x => x.Choices.Count));
        Equal(80, baseline.VietnameseText.Count);
        Equal(4, policies.Count);
        Equal(definitions.Count, definitions.Select(x => x.Id)
            .Distinct(StringComparer.Ordinal).Count());
        EventDecisionJournal.Validate(EventDecisionJournal.New("catalog-test", 3),
            definitions, policies);
        var providers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < definitions.Count; i++)
        {
            var story = definitions[i];
            foreach (var tag in story.RequiredTags.Where(x =>
                x.StartsWith("event/", StringComparison.Ordinal)))
                if (!providers.TryGetValue(tag, out var provider) || provider >= i)
                    throw new Exception("Event chain has missing or forward-only unlock: " + story.Id + " / " + tag);
            foreach (var tag in story.Choices.SelectMany(x => x.GrantedTags))
            {
                if (providers.ContainsKey(tag))
                    throw new Exception("Event tag must have a single authored source: " + tag);
                providers.Add(tag, i);
            }
        }
        foreach (var story in definitions)
        {
            Equal(1, story.MaxOccurrences);
            if (story.TitleKey.Length == 0 || story.BodyKey.Length == 0 ||
                story.Choices.Count < 2 ||
                story.Choices.Any(x => x.GrantedTags.Any(t =>
                    !t.StartsWith("event/", StringComparison.Ordinal))))
                throw new Exception("Incomplete narrative or unauthorized effect: " + story.Id);
        }
        try { ((IList<GameEventDefinition>)definitions).Clear(); }
        catch (NotSupportedException) { return; }
        throw new Exception("Authored event catalog must be immutable.");
    }

    private static void EventStoryChainReplay()
    {
        var full = FirstPlayableEventContent.Definitions;
        var pair = new[]
        {
            full.Single(x => x.Id == "welcome-neighbor"),
            full.Single(x => x.Id == "coffee-connection")
        };
        var categories = new[]
        {
            FirstPlayableEventContent.Policies.Single(x => x.Id == "life")
        };
        var start = new SimDate(2026, 10, 12);
        var later = new SimDate(2026, 10, 15);
        var baseTags = new[] { "character-created", "unemployed" };
        GameEventContext Context(SimDate date, params string[] tags) =>
            new GameEventContext(start, date, 1, tags, Array.Empty<EventOccurrence>());
        var initial = EventDecisionJournal.New("m15-story-chain", 101U);
        var first = EventDecisionJournal.Offer(initial, pair, categories,
            Context(start, baseTags), initial.Revision, "chain-offer-1");
        Equal(EventJournalStatus.Offered, first.Status);
        Equal("welcome-neighbor", first.Next.Tickets[0].EventId);
        var granted = EventDecisionJournal.Resolve(first.Next, pair, categories,
            first.Next.Revision, "chain-resolve-1", first.TicketId, "join");
        Equal(EventJournalStatus.Resolved, granted.Status);
        Equal("event/contact", granted.Next.OutcomeReceipts.Single().GrantedTags.Single());

        var second = EventDecisionJournal.Offer(granted.Next, pair, categories,
            Context(later, "character-created", "unemployed", "event/contact"),
            granted.Next.Revision, "chain-offer-2");
        Equal(EventJournalStatus.Offered, second.Status);
        Equal("coffee-connection", second.Next.Tickets[1].EventId);
        EventJournalReplay.Validate(second.Next, pair, categories);
        var tampered = second.Next.Copy();
        tampered.Tickets[1].Tags.Remove("event/contact");
        Throws(() => EventJournalReplay.Validate(tampered, pair, categories));

        var declined = EventDecisionJournal.Resolve(first.Next, pair, categories,
            first.Next.Revision, "chain-resolve-decline", first.TicketId, "decline");
        Equal(EventJournalStatus.Resolved, declined.Status);
        Equal(0, declined.Next.OutcomeReceipts[0].GrantedTags.Count);
        var notUnlocked = EventDecisionJournal.Offer(declined.Next, pair, categories,
            Context(later, baseTags), declined.Next.Revision, "chain-offer-2-declined");
        Equal(EventJournalStatus.NoEligibleEvent, notUnlocked.Status);
        Equal(declined.Next.EventRng, notUnlocked.Next.EventRng);
    }

    private static void CourseEventStoryReplay()
    {
        var full = FirstPlayableEventContent.Definitions;
        var stories = new[]
        {
            full.Single(x => x.Id == "course-study-questions"),
            full.Single(x => x.Id == "course-question-follow-up")
        };
        var categories = new[]
        {
            FirstPlayableEventContent.Policies.Single(x => x.Id == "study")
        };
        var firstDay = new SimDate(2026, 10, 12);
        var nextWeek = new SimDate(2026, 10, 19);
        GameEventContext Context(SimDate date, params string[] tags) =>
            new GameEventContext(firstDay, date, 1, tags, Array.Empty<EventOccurrence>());
        var initial = EventDecisionJournal.New("course-story-run", 0xCA71U);
        var offered = EventDecisionJournal.Offer(initial, stories, categories,
            Context(firstDay, "character-created", "course-active"),
            initial.Revision, "course-offer");
        Equal(EventJournalStatus.Offered, offered.Status);
        Equal("course-study-questions", offered.Next.Tickets.Single().EventId);
        var prepared = EventDecisionJournal.Resolve(offered.Next, stories, categories,
            offered.Next.Revision, "course-prepare", offered.TicketId, "prepare");
        Equal(EventJournalStatus.Resolved, prepared.Status);
        True(prepared.Next.OutcomeReceipts.Single().GrantedTags
            .Contains("event/course-questions-prepared"));

        // The course may have completed by the following week: the later
        // narrative depends only on the authenticated event outcome tag.
        var continued = EventDecisionJournal.Offer(prepared.Next, stories, categories,
            Context(nextWeek, "character-created", "event/course-questions-prepared"),
            prepared.Next.Revision, "course-next-week");
        Equal(EventJournalStatus.Offered, continued.Status);
        Equal("course-question-follow-up", continued.Next.Tickets.Last().EventId);
        EventJournalReplay.Validate(continued.Next, stories, categories);
        var forged = continued.Next.Copy();
        forged.Tickets.Last().Tags.Remove("event/course-questions-prepared");
        Throws(() => EventJournalReplay.Validate(forged, stories, categories));

        var reviewed = EventDecisionJournal.Resolve(offered.Next, stories, categories,
            offered.Next.Revision, "course-review", offered.TicketId, "review");
        Equal(EventJournalStatus.Resolved, reviewed.Status);
        True(!reviewed.Next.OutcomeReceipts.Single().GrantedTags
            .Contains("event/course-questions-prepared"));
        var noFollowUp = EventDecisionJournal.Offer(reviewed.Next, stories, categories,
            Context(nextWeek, "character-created"),
            reviewed.Next.Revision, "course-no-followup");
        Equal(EventJournalStatus.NoEligibleEvent, noFollowUp.Status);
        Equal(reviewed.Next.EventRng, noFollowUp.Next.EventRng);
    }

    private static void EventStoryEligibility()
    {
        var stories = FirstPlayableEventContent.Definitions;
        var day = new SimDate(2026, 10, 13);
        var start = new SimDate(2026, 10, 12);
        GameEventContext Context(int stage, params string[] tags) =>
            new GameEventContext(start, day, stage, tags, Array.Empty<EventOccurrence>());
        GameEventDefinition Story(string id) => stories.Single(x => x.Id == id);
        var newPlayer = Context(1, "character-created", "unemployed");
        if (!GameEventSelector.IsEligible(Story("welcome-neighbor"), newPlayer) ||
            GameEventSelector.IsEligible(Story("coffee-connection"), newPlayer) ||
            GameEventSelector.IsEligible(Story("first-market-day"), newPlayer))
            throw new Exception("Events cannot unlock without the authenticated tags/stage.");
        var contact = Context(1, "character-created", "unemployed", "event/contact");
        if (!GameEventSelector.IsEligible(Story("coffee-connection"), contact))
            throw new Exception("First social event must unlock networking follow-up.");
        var founder = Context(2, "character-created", "business-active");
        if (!GameEventSelector.IsEligible(Story("first-market-day"), founder) ||
            GameEventSelector.IsEligible(Story("supplier-chat"), founder))
            throw new Exception("Business chain must wait for customer relationship.");
        var course = Context(1, "character-created", "course-active");
        if (!GameEventSelector.IsEligible(Story("weekend-study"), course) ||
            GameEventSelector.IsEligible(Story("study-friend"), course))
            throw new Exception("Study chain must wait for event receipt.");
        Throws(() => new GameEventDefinition("unpaired-title", "life", 2026, 1,
            1, 0, 1, 1, 0, Array.Empty<string>(), Array.Empty<string>(),
            new[] { new EventChoiceDefinition("ok", "choice.ok", "result-ok") },
            "event.unpaired.title"));
    }

    private static void FirstPlayableBuilds()
    {
        var catalog = FirstPlayableContentTemplate.BuildCatalog();
        Equal("first-playable.v1", catalog.Version);
        Equal(6, catalog.Skills.Count);
        Equal(1, catalog.Careers.Count);
        Equal(2, catalog.Courses.Count);
        Equal(1, catalog.Starts.Count);

        var developer = catalog.Careers["developer"];
        Equal(20, developer.QuotaSlots);
        Equal(4, developer.ScenesPerShift);
        Equal(100, developer.Scenes.Sum(x => x.Weight));
        Sequence(new[] { 40, 20, 15, 10, 10, 5 }, developer.Scenes.Select(x => x.Weight));
        Sequence(new[] { "base.female", "base.male" }, catalog.Starts["fresh"].AppearanceIds);
        Equal(480, catalog.Schedule.WakeMinute);
        Equal(1320, catalog.Schedule.SleepMinute);
        Equal(3000000L, catalog.Starts["fresh"].Cash);
    }

    private static void DuplicateIdsReject()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Skills = new[] { source.Skills[0], source.Skills[0] };
        Throws(() => source.Build());
    }

    private static void MissingCareerSkillRejects()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Careers[0].Scenes[0].SkillId = "missing-skill";
        Throws(() => source.Build());
    }

    private static void MissingCourseSkillRejects()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Courses[0].SkillId = "missing-skill";
        Throws(() => source.Build());
    }

    private static void InvalidAppearanceRejects()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Starts[0].AppearanceIds = new[] { "Base Female" };
        Throws(() => source.Build());
    }

    private static void CareerOutsideScheduleRejects()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Schedule.WakeMinute = 600;
        Throws(() => source.Build());
    }

    private static void BuiltCatalogIsDetached()
    {
        var source = FirstPlayableContentTemplate.Create();
        var catalog = source.Build();

        source.Skills[0].Thresholds[0] = 999999;
        source.Starts[0].AppearanceIds[0] = "forged.appearance";
        source.Careers[0].Scenes[0].Weight = 1;

        Equal(100L, catalog.Skills["communication"].Thresholds[0]);
        Equal("base.female", catalog.Starts["fresh"].AppearanceIds[0]);
        Equal(40, catalog.Careers["developer"].Scenes[0].Weight);
    }

    private static void NullEntryRejects()
    {
        var source = FirstPlayableContentTemplate.Create();
        source.Courses = new CourseContentSource[] { null! };
        Throws(() => source.Build());
    }

    private static void ProductionBusinessEligibility()
    {
        var c = FirstPlayableContentTemplate.BuildCatalog();
        var date = new SimDate(2026,9,2);
        var state = new GameState { DateIso = date.ToString(), Minute = 480, Employment = new EmploymentState
            { CareerId = "developer", DefinitionRevision = "v1", FirstShiftIso = date.ToString() } };
        var f = BusinessEligibilityEvaluator.Evaluate(c,state,FirstPlayableContentTemplate.FreelanceId,"v1");
        Equal(BusinessEligibilityStatus.Eligible,f.Status); Equal<int?>(240,f.AvailableOwnerMinutes); Equal<int?>(120,f.RequiredOwnerMinutes);
        var k = BusinessEligibilityEvaluator.Evaluate(c,state,FirstPlayableContentTemplate.CoffeeKioskId,"v1");
        Equal(BusinessEligibilityStatus.EmploymentIncompatible,k.Status);
        state.Employment = null;
        k = BusinessEligibilityEvaluator.Evaluate(c,state,FirstPlayableContentTemplate.CoffeeKioskId,"v1");
        Equal(BusinessEligibilityStatus.Eligible,k.Status); Equal<int?>(480,k.AvailableOwnerMinutes); Equal<int?>(480,k.RequiredOwnerMinutes);
    }

    private static void AuthoredBusinesses()
    {
        var c = FirstPlayableContentTemplate.BuildCatalog();
        Equal(4, c.Businesses.Count);
        Equal(true, c.Businesses.ContainsKey("online-store"));
        Equal(true, c.Businesses.ContainsKey("home-food-preorder"));
        var f = c.Businesses[FirstPlayableContentTemplate.FreelanceId];
        var k = c.Businesses[FirstPlayableContentTemplate.CoffeeKioskId];
        Equal(BusinessOperationMode.SideHustleCompatible, f.OperationMode);
        Equal(BusinessOperationMode.FullTimeRequired, k.OperationMode);
        Equal(120, f.OperatingRequirements!.RequiredOwnerMinutes); Equal(480, k.OperatingRequirements!.RequiredOwnerMinutes);
        Equal(7, f.OperatingRequirements.OperatingWindows.Count); Equal(7, k.OperatingRequirements.OperatingWindows.Count);
        foreach (var w in f.OperatingRequirements.OperatingWindows) { Equal(1080, w.StartMinute); Equal(1320, w.EndMinute); }
        foreach (var w in k.OperatingRequirements.OperatingWindows) { Equal(540, w.StartMinute); Equal(1020, w.EndMinute); }
    }
    private static void InvalidBusinessAuthoring()
    {
        foreach (var alter in new Action<BusinessContentSource>[]
        {
            b => b.OperatingWindows = null!, b => b.OperatingWindows = Array.Empty<BusinessOperatingWindowContentSource>(),
            b => b.OperatingWindows = new BusinessOperatingWindowContentSource[] { null! }, b => b.RequiredOwnerMinutes = 0,
            b => b.AllowedPricingPostures = null!, b => b.OperationMode = (BusinessOperationMode)99,
            b => b.MinimumStartupInvestment = 0, b => b.MinimumReinvestment = 0,
            b => b.OperatingWindows[0].StartMinute = 470, b => b.OperatingWindows[0].EndMinute = 1321,
            b => b.OperatingWindows = new[] { b.OperatingWindows[0], b.OperatingWindows[0] }
        })
        {
            var source = FirstPlayableContentTemplate.Create(); alter(source.Businesses[0]); Throws(() => source.Build());
        }
    }
    private static void DetachedBusinessAuthoring()
    {
        var source = FirstPlayableContentTemplate.Create(); var catalog = source.Build();
        source.Businesses[0].OperatingWindows[0].StartMinute = 1200;
        source.Businesses[0].RequiredOwnerMinutes = 240; source.Businesses[0].AllowedPricingPostures[0] = PricingPosture.Premium;
        var b = catalog.Businesses[FirstPlayableContentTemplate.FreelanceId];
        Equal(1080, b.OperatingRequirements!.OperatingWindows[0].StartMinute); Equal(120,b.OperatingRequirements.RequiredOwnerMinutes);
        Equal(PricingPosture.Budget,b.AllowedPricingPostures[0]);
    }
    private static void BusinessSourceCollection()
    {
        var source = FirstPlayableContentTemplate.Create(); source.Businesses = null!; Throws(() => source.Build());
        source = FirstPlayableContentTemplate.Create(); source.Businesses = new BusinessContentSource[] { null! }; Throws(() => source.Build());
        source = FirstPlayableContentTemplate.Create(); source.Businesses = new[] { source.Businesses[0], source.Businesses[0] }; Throws(() => source.Build());
        source = FirstPlayableContentTemplate.Create(); source.Businesses = Array.Empty<BusinessContentSource>(); Equal(0,source.Build().Businesses.Count);
    }

    private static void Check(string name, Action action)
    {
        try
        {
            action();
            passed++;
            Results.Add(new { name, status = "passed" });
            Console.WriteLine("PASS " + name);
        }
        catch (Exception error)
        {
            Results.Add(new { name, status = "failed", error = error.ToString() });
            Console.WriteLine("FAIL " + name + ": " + error.Message);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}; got {actual}");
    }

    private static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
            throw new Exception("Sequence mismatch.");
    }

    private static void Throws(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Expected ArgumentException.");
    }
}

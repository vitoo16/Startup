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

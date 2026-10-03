#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StartupLife.Content;
using StartupLife.Core;

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

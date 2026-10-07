#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StartupLife.Core
{
    public sealed class DayScheduleDefinition
    {
        public int WakeMinute { get; }
        public int SleepMinute { get; }
        public DayScheduleDefinition(int wake, int sleep)
        {
            if (wake < 0 || wake >= sleep || sleep >= 1440) throw new ArgumentException("Invalid sleep schedule.");
            WakeMinute = wake; SleepMinute = sleep;
        }
    }
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public IReadOnlyList<long> Thresholds { get; }
        public SkillDefinition(string id, string nameKey, params long[] thresholds)
        {
            _ = new ContentId(id);
            if (string.IsNullOrWhiteSpace(nameKey) || thresholds.Length != 5 || thresholds[0] <= 0 ||
                thresholds.Zip(thresholds.Skip(1), (a, b) => b > a).Any(x => !x))
                throw new ArgumentException("Invalid skill thresholds/localization key.");
            Id = id; NameKey = nameKey; Thresholds = Array.AsReadOnly((long[])thresholds.Clone());
        }
    }
    public sealed class CareerSceneDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public string SkillId { get; }
        public int Weight { get; }
        public long CareerXp { get; }
        public long Exposure { get; }
        public CareerSceneDefinition(string id, string key, int weight, long xp, string skillId, long exposure)
        {
            _ = new ContentId(id); _ = new ContentId(skillId);
            if (string.IsNullOrWhiteSpace(key) || weight < 0 || weight > 100 || xp < 0 || exposure < 0)
                throw new ArgumentException("Invalid scene.");
            Id = id; NameKey = key; Weight = weight; CareerXp = xp; SkillId = skillId; Exposure = exposure;
        }
    }
    public sealed class CareerRankDefinition
    {
        public string Id { get; }
        public long RequiredXp { get; }
        public int RequiredServiceDays { get; }
        public long MonthlySalary { get; }
        public string GrantSkillId { get; }
        public int GrantLevel { get; }
        public CareerRankDefinition(string id, long requiredXp, int serviceDays, long salary, string skillId, int grantLevel)
        {
            _ = new ContentId(id); _ = new ContentId(skillId);
            if (requiredXp < 0 || serviceDays < 0 || salary < 0 || grantLevel < 0 || grantLevel > 5)
                throw new ArgumentException("Invalid rank.");
            Id = id; RequiredXp = requiredXp; RequiredServiceDays = serviceDays;
            MonthlySalary = salary; GrantSkillId = skillId; GrantLevel = grantLevel;
        }
    }
    public sealed class CareerDefinition
    {
        public string Id { get; }
        public string Revision { get; }
        public string NameKey { get; }
        public int StartMinute { get; }
        public int EndMinute { get; }
        public int ScenesPerShift { get; }
        public int QuotaSlots { get; }
        public IReadOnlyList<DayOfWeek> WorkDays { get; }
        public IReadOnlyList<CareerSceneDefinition> Scenes { get; }
        public IReadOnlyList<CareerRankDefinition> Ranks { get; }
        public CareerDefinition(string id, string revision, string key, int start, int end, int scenesPerShift,
            int quotaSlots, IEnumerable<DayOfWeek> days, IEnumerable<CareerSceneDefinition> scenes,
            IEnumerable<CareerRankDefinition> ranks)
        {
            _ = new ContentId(id);
            var sceneArray = scenes.ToArray(); var rankArray = ranks.ToArray(); var dayArray = days.ToArray();
            if (string.IsNullOrWhiteSpace(revision) || string.IsNullOrWhiteSpace(key) || start < 0 || end > 1440 ||
                end <= start || scenesPerShift <= 0 || (end - start) % scenesPerShift != 0 || quotaSlots <= 0 ||
                dayArray.Length == 0 || dayArray.Distinct().Count() != dayArray.Length || dayArray.Any(x => (int)x < 0 || (int)x > 6) ||
                sceneArray.Length == 0 || sceneArray.Select(x => x.Id).Distinct().Count() != sceneArray.Length ||
                sceneArray.Sum(x => x.Weight) != 100 || rankArray.Length == 0 || rankArray[0].RequiredXp != 0 ||
                rankArray[0].RequiredServiceDays != 0 || rankArray.Select(x => x.Id).Distinct().Count() != rankArray.Length ||
                rankArray.Zip(rankArray.Skip(1), (a, b) => b.RequiredXp >= a.RequiredXp && b.RequiredServiceDays >= a.RequiredServiceDays).Any(x => !x))
                throw new ArgumentException("Invalid career/schedule/quota.");
            Id = id; Revision = revision; NameKey = key; StartMinute = start; EndMinute = end;
            ScenesPerShift = scenesPerShift; QuotaSlots = quotaSlots;
            WorkDays = Array.AsReadOnly(dayArray); Scenes = Array.AsReadOnly(sceneArray); Ranks = Array.AsReadOnly(rankArray);
        }
        public bool WorksOn(SimDate date) => WorkDays.Contains(date.DayOfWeek);
    }
    public sealed class CourseDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public string SkillId { get; }
        public int TargetLevel { get; }
        public int PrerequisiteLevel { get; }
        public int BaseMinutes { get; }
        public long Price { get; }
        public CourseDefinition(string id, string key, string skillId, int target, int prerequisite, int minutes, long price)
        {
            _ = new ContentId(id); _ = new ContentId(skillId);
            if (string.IsNullOrWhiteSpace(key) || target < 1 || target > 5 || prerequisite < 0 || prerequisite >= target || minutes <= 0 || price < 0)
                throw new ArgumentException("Invalid course.");
            Id = id; NameKey = key; SkillId = skillId; TargetLevel = target; PrerequisiteLevel = prerequisite;
            BaseMinutes = minutes; Price = price;
        }
    }
    public sealed class CharacterStartDefinition
    {
        public string Id { get; }
        public long Cash { get; }
        public int LearningSpeed { get; }
        public IReadOnlyList<string> AppearanceIds { get; }
        public CharacterStartDefinition(string id, long cash, int speed, params string[] appearances)
        {
            _ = new ContentId(id);
            if (cash < 0 || speed <= 0 || appearances.Length == 0 || appearances.Any(x => !ContentId.IsValid(x)))
                throw new ArgumentException("Invalid starting package.");
            Id = id; Cash = cash; LearningSpeed = speed; AppearanceIds = Array.AsReadOnly((string[])appearances.Clone());
        }
    }
    public sealed class EconomyBalanceDefinition
    {
        public int Payday { get; }
        public int ExpenseDay { get; }
        public long LivingCost { get; }
        public EconomyBalanceDefinition(int payday, int expenseDay, long cost)
        {
            if (payday < 1 || payday > 31 || expenseDay < 1 || expenseDay > 31 || cost < 0) throw new ArgumentException("Invalid economy.");
            Payday = payday; ExpenseDay = expenseDay; LivingCost = cost;
        }
    }
    public sealed class ContentCatalog
    {
        public string Version { get; }
        public IReadOnlyDictionary<string, SkillDefinition> Skills { get; }
        public IReadOnlyDictionary<string, CareerDefinition> Careers { get; }
        public IReadOnlyDictionary<string, CourseDefinition> Courses { get; }
        public IReadOnlyDictionary<string, CharacterStartDefinition> Starts { get; }
        public IReadOnlyDictionary<string, BusinessDefinition> Businesses { get; }
        public EconomyBalanceDefinition Economy { get; }
        public DayScheduleDefinition Schedule { get; }
        public ContentCatalog(string version, IEnumerable<SkillDefinition> skills, IEnumerable<CareerDefinition> careers,
            IEnumerable<CourseDefinition> courses, IEnumerable<CharacterStartDefinition> starts, EconomyBalanceDefinition economy, DayScheduleDefinition schedule)
            : this(version, skills, careers, courses, starts, Array.Empty<BusinessDefinition>(), economy, schedule) { }

        public ContentCatalog(string version, IEnumerable<SkillDefinition> skills, IEnumerable<CareerDefinition> careers,
            IEnumerable<CourseDefinition> courses, IEnumerable<CharacterStartDefinition> starts, IEnumerable<BusinessDefinition> businesses,
            EconomyBalanceDefinition economy, DayScheduleDefinition schedule)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Content version required.");
            Version = version; Skills = Index(skills, x => x.Id); Careers = Index(careers, x => x.Id);
            Courses = Index(courses, x => x.Id); Starts = Index(starts, x => x.Id); Businesses = Index(businesses, x => x.Id);
            Economy = economy; Schedule = schedule;
            foreach (var c in Careers.Values)
                if (c.StartMinute < schedule.WakeMinute || c.EndMinute > schedule.SleepMinute || c.Scenes.Any(x => !Skills.ContainsKey(x.SkillId)) || c.Ranks.Any(x => !Skills.ContainsKey(x.GrantSkillId)))
                    throw new ArgumentException("Missing career skill reference.");
            foreach (var business in Businesses.Values)
                if (business.OperatingRequirements != null && business.OperatingRequirements.OperatingWindows.Any(window =>
                    window.StartMinute < schedule.WakeMinute || window.EndMinute > schedule.SleepMinute))
                    throw new ArgumentException("Business operating window is outside the waking day.");
            if (Courses.Values.Any(x => !Skills.ContainsKey(x.SkillId))) throw new ArgumentException("Missing course skill reference.");
        }
        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> values, Func<T, string> id) =>
            new ReadOnlyDictionary<string, T>(values.ToDictionary(id, StringComparer.Ordinal));
    }
}

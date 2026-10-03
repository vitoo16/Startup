#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Content
{
    public static class FirstPlayableContentTemplate
    {
        private static readonly string[] SkillIds =
        {
            "communication",
            "negotiation",
            "time-management",
            "problem-solving",
            "networking",
            "leadership"
        };

        public static ContentCatalogSource Create()
        {
            var skills = new SkillContentSource[SkillIds.Length];
            for (var i = 0; i < SkillIds.Length; i++)
            {
                skills[i] = new SkillContentSource
                {
                    Id = SkillIds[i],
                    NameKey = "skill." + SkillIds[i],
                    Thresholds = new long[] { 100, 300, 600, 1000, 1500 }
                };
            }

            return new ContentCatalogSource
            {
                Version = "first-playable.v1",
                Skills = skills,
                Careers = new[]
                {
                    new CareerContentSource
                    {
                        Id = "developer",
                        Revision = "v1",
                        NameKey = "career.developer",
                        StartMinute = 540,
                        EndMinute = 1020,
                        ScenesPerShift = 4,
                        QuotaSlots = 20,
                        WorkDays = new[]
                        {
                            DayOfWeek.Monday,
                            DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday
                        },
                        Scenes = new[]
                        {
                            Scene("coding", 40),
                            Scene("meeting", 20),
                            Scene("bug-fixing", 15),
                            Scene("client-discussion", 10),
                            Scene("demo", 10),
                            Scene("documentation", 5)
                        },
                        Ranks = new[]
                        {
                            new CareerRankContentSource
                            {
                                Id = "junior",
                                RequiredXp = 0,
                                RequiredServiceDays = 0,
                                MonthlySalary = 10000000,
                                GrantSkillId = "problem-solving",
                                GrantLevel = 0
                            },
                            new CareerRankContentSource
                            {
                                Id = "mid",
                                RequiredXp = 1000,
                                RequiredServiceDays = 30,
                                MonthlySalary = 15000000,
                                GrantSkillId = "problem-solving",
                                GrantLevel = 2
                            }
                        }
                    }
                },
                Courses = new[]
                {
                    new CourseContentSource
                    {
                        Id = "communication-basics",
                        NameKey = "course.communication",
                        SkillId = "communication",
                        TargetLevel = 1,
                        PrerequisiteLevel = 0,
                        BaseMinutes = 360,
                        Price = 200000
                    },
                    new CourseContentSource
                    {
                        Id = "problem-course",
                        NameKey = "course.problem",
                        SkillId = "problem-solving",
                        TargetLevel = 2,
                        PrerequisiteLevel = 0,
                        BaseMinutes = 360,
                        Price = 200000
                    }
                },
                Starts = new[]
                {
                    new CharacterStartContentSource
                    {
                        Id = "fresh",
                        Cash = 3000000,
                        LearningSpeed = 10000,
                        AppearanceIds = new[] { "base.female", "base.male" }
                    }
                },
                Economy = new EconomyContentSource
                {
                    Payday = 1,
                    ExpenseDay = 1,
                    LivingCost = 1000000
                },
                Schedule = new DayScheduleContentSource
                {
                    WakeMinute = 480,
                    SleepMinute = 1320
                }
            };
        }

        public static ContentCatalog BuildCatalog() => Create().Build();

        private static CareerSceneContentSource Scene(string id, int weight) =>
            new CareerSceneContentSource
            {
                Id = id,
                NameKey = "scene." + id,
                Weight = weight,
                CareerXp = 10,
                SkillId = "communication",
                Exposure = 1
            };
    }
}

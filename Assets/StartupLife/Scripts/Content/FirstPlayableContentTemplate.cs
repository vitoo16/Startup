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

        public const string FreelanceId = "freelance-service";
        public const string CoffeeKioskId = "coffee-kiosk";
        public const string OnlineStoreId = "online-store";
        public const string HomeFoodPreorderId = "home-food-preorder";

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
                    },
                    MarketingCareer(),
                    SalesCareer(),
                    OfficeAdminCareer()
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
                Businesses = new[]
                {
                    FunctionalBusiness(FreelanceId, BusinessType.FreelanceService, BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120),
                    FunctionalBusiness(CoffeeKioskId, BusinessType.CoffeeKiosk, BusinessOperationMode.FullTimeRequired, 540, 1020, 480),
                    FunctionalBusiness(OnlineStoreId, BusinessType.OnlineStore, BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120),
                    FunctionalBusiness(HomeFoodPreorderId, BusinessType.HomeFoodPreorder, BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120)
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

        // Batch A follows the original GDD's MVP profession catalog. New
        // definitions are additive: the shipped Developer definition above
        // stays byte-for-byte equivalent in its schedule, scene rewards and ranks.
        private static CareerContentSource MarketingCareer() => new CareerContentSource
        {
            Id = "marketing",
            Revision = "v1",
            NameKey = "career.marketing",
            StartMinute = 540,
            EndMinute = 1020,
            ScenesPerShift = 4,
            QuotaSlots = 20,
            WorkDays = Weekdays(),
            Scenes = new[]
            {
                ProfessionScene("marketing-campaign-planning", 25, "problem-solving", 1),
                ProfessionScene("marketing-content-review", 20, "communication", 1),
                ProfessionScene("marketing-analytics", 20, "problem-solving", 2),
                ProfessionScene("marketing-team-meeting", 15, "leadership", 1),
                ProfessionScene("marketing-client-presentation", 10, "communication", 2),
                ProfessionScene("marketing-partner-communication", 10, "networking", 1)
            },
            Ranks = new[]
            {
                Rank("junior", 0, 0, 9000000, "networking", 0),
                Rank("mid", 1000, 30, 13500000, "communication", 1),
                Rank("senior", 3000, 90, 18000000, "networking", 2)
            }
        };

        private static CareerContentSource SalesCareer() => new CareerContentSource
        {
            Id = "sales",
            Revision = "v1",
            NameKey = "career.sales",
            StartMinute = 540,
            EndMinute = 1080,
            ScenesPerShift = 4,
            QuotaSlots = 20,
            WorkDays = MondayToSaturday(),
            Scenes = new[]
            {
                ProfessionScene("sales-prospecting", 20, "networking", 1),
                ProfessionScene("sales-needs-discovery", 20, "communication", 1),
                ProfessionScene("sales-product-demo", 15, "communication", 1),
                ProfessionScene("sales-negotiation", 20, "negotiation", 2),
                ProfessionScene("sales-follow-up", 15, "networking", 1),
                ProfessionScene("sales-sales-report", 10, "time-management", 1)
            },
            Ranks = new[]
            {
                Rank("junior", 0, 0, 8000000, "negotiation", 0),
                Rank("mid", 1000, 30, 12000000, "communication", 1),
                Rank("senior", 3000, 90, 16000000, "negotiation", 2)
            }
        };

        private static CareerContentSource OfficeAdminCareer() => new CareerContentSource
        {
            Id = "office-admin",
            Revision = "v1",
            NameKey = "career.office-admin",
            StartMinute = 510,
            EndMinute = 1050,
            ScenesPerShift = 4,
            QuotaSlots = 20,
            WorkDays = Weekdays(),
            Scenes = new[]
            {
                ProfessionScene("office-document-processing", 25, "time-management", 1),
                ProfessionScene("office-schedule-coordination", 20, "communication", 1),
                ProfessionScene("office-data-review", 20, "problem-solving", 1),
                ProfessionScene("office-internal-support", 15, "communication", 1),
                ProfessionScene("office-record-keeping", 10, "time-management", 2),
                ProfessionScene("office-team-briefing", 10, "leadership", 1)
            },
            Ranks = new[]
            {
                Rank("junior", 0, 0, 7000000, "time-management", 0),
                Rank("mid", 1000, 30, 10500000, "problem-solving", 1),
                Rank("senior", 3000, 90, 14000000, "leadership", 2)
            }
        };

        private static DayOfWeek[] Weekdays() => new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };

        private static DayOfWeek[] MondayToSaturday() => new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
        };

        private static CareerRankContentSource Rank(
            string id, long requiredXp, int serviceDays, long monthlySalary,
            string grantSkillId, int grantLevel) => new CareerRankContentSource
        {
            Id = id,
            RequiredXp = requiredXp,
            RequiredServiceDays = serviceDays,
            MonthlySalary = monthlySalary,
            GrantSkillId = grantSkillId,
            GrantLevel = grantLevel
        };

        private static CareerSceneContentSource ProfessionScene(
            string id, int weight, string skillId, long exposure) =>
            new CareerSceneContentSource
            {
                Id = id,
                NameKey = "scene." + id,
                Weight = weight,
                CareerXp = 10,
                SkillId = skillId,
                Exposure = exposure
            };

        // Provisional functional content only; capital/pricing are not final economy balance.
        private static BusinessContentSource FunctionalBusiness(string id, BusinessType type, BusinessOperationMode mode,
            int start, int end, int required)
        {
            var windows = new BusinessOperatingWindowContentSource[7];
            for (var day = 0; day < windows.Length; day++)
                windows[day] = new BusinessOperatingWindowContentSource { DayOfWeek = (DayOfWeek)day, StartMinute = start, EndMinute = end };
            return new BusinessContentSource { Id = id, Revision = "v1", NameKey = "business." + id,
                Type = type, OperationMode = mode, MinimumStartupInvestment = 100, MaximumStartupInvestment = 1000,
                AllowedPricingPostures = new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium },
                DefaultPricingPosture = PricingPosture.Standard, MinimumReinvestment = 1, MaximumReinvestment = 1000,
                OperatingWindows = windows, RequiredOwnerMinutes = required };
        }

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

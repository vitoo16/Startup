#nullable enable
using System;
using StartupLife.Core;

namespace StartupLife.Content
{
    // Frozen data extracted from the authored Unity asset at baseline
    // fe78dc29d590ecacca6056eba07a7b2cf4605557.
    // No reference to FirstPlayableContentTemplate or current live v3 authoring data:
    // edited current prices/careers must NEVER be substituted for a historical receipt.
    public sealed class BundledHistoricalV2ContentResolver : IHistoricalEconomicRulesResolver
    {
        public const string ArchiveId = "first-playable.v1/v2-semantics@051a9314";
        public const string ContentVersion = "first-playable.v1";

        public bool TryResolve(string archivedRulesetId, string originalContentVersion, out ContentCatalog? content)
        {
            if (!string.Equals(archivedRulesetId, ArchiveId, StringComparison.Ordinal) ||
                !string.Equals(originalContentVersion, ContentVersion, StringComparison.Ordinal))
            {
                content = null;
                return false;
            }
            content = BuildFrozenCatalog();
            return true;
        }

        private static ContentCatalog BuildFrozenCatalog()
        {
            var skills = new[] {
                "communication", "negotiation", "time-management",
                "problem-solving", "networking", "leadership"
            };
            var definitions = new SkillDefinition[skills.Length];
            for (var i = 0; i < skills.Length; i++)
                definitions[i] = new SkillDefinition(skills[i], "skill." + skills[i],
                    100L, 300L, 600L, 1000L, 1500L);

            var developer = new CareerDefinition(
                "developer", "v1", "career.developer", 540, 1020, 4, 20,
                new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                    DayOfWeek.Thursday, DayOfWeek.Friday },
                new[] {
                    Scene("coding", 40), Scene("meeting", 20), Scene("bug-fixing", 15),
                    Scene("client-discussion", 10), Scene("demo", 10), Scene("documentation", 5)
                },
                new[] {
                    new CareerRankDefinition("junior",0,0,10000000,"problem-solving",0),
                    new CareerRankDefinition("mid",1000,30,15000000,"problem-solving",2)
                });
            var courses = new[] {
                new CourseDefinition("communication-basics","course.communication","communication",1,0,360,200000),
                new CourseDefinition("problem-course","course.problem","problem-solving",2,0,360,200000)
            };
            var starts = new[] {
                new CharacterStartDefinition("fresh",3000000,10000,"base.female","base.male")
            };
            var businesses = new[] {
                Business("freelance-service", BusinessType.FreelanceService,
                    BusinessOperationMode.SideHustleCompatible,1080,1320,120),
                Business("coffee-kiosk", BusinessType.CoffeeKiosk,
                    BusinessOperationMode.FullTimeRequired,540,1020,480)
            };
            return new ContentCatalog(ContentVersion, definitions,
                new[] {developer}, courses, starts, businesses,
                new EconomyBalanceDefinition(1,1,1000000),
                new DayScheduleDefinition(480,1320));
        }

        private static CareerSceneDefinition Scene(string id, int weight) =>
            new CareerSceneDefinition(id, "scene." + id, weight, 10, "communication", 1);

        private static BusinessDefinition Business(string id, BusinessType type, BusinessOperationMode mode,
            int start, int end, int required)
        {
            var windows = new BusinessOperatingWindow[7];
            for (var d = 0; d < 7; d++)
                windows[d] = new BusinessOperatingWindow((DayOfWeek)d, start, end);
            return new BusinessDefinition(id, "v1", "business." + id, type, mode,
                100, 1000,
                new[] { PricingPosture.Budget, PricingPosture.Standard, PricingPosture.Premium },
                PricingPosture.Standard, 1, 1000,
                new BusinessOperatingRequirements(windows, required));
        }
    }
}

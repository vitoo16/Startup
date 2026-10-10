#nullable enable
using System;
using System.Collections.Generic;

namespace StartupLife.Application
{
    // Topic IDs only: onboarding never changes gameplay state or invents economic
    // rules. Presentation/localization owns the visible explanations.
    public static class OnboardingGuideCatalog
    {
        public const int Version = 1;

        private static readonly string[] DefinedTopics =
        {
            "onboarding.skills.free",
            "onboarding.study.paid",
            "onboarding.business.compatibility",
            "onboarding.time.automatic",
            "onboarding.resign.not-forced"
        };

        public static IReadOnlyList<string> Topics { get; } =
            Array.AsReadOnly(DefinedTopics);

        public static string At(int index)
        {
            if (index < 0 || index >= DefinedTopics.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return DefinedTopics[index];
        }
    }
}

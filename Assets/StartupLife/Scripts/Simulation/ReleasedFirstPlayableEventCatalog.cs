#nullable enable
using System;
using System.Collections.Generic;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // RELEASED V4 BASELINE -- intentionally duplicated *authored content*, not
    // simulation code. DO NOT regenerate or edit when current stories change:
    // these exact event identities/rules selected already-persisted V4 tickets.
    // Captured from FirstPlayableEventContent at 5adfc340 (2026-10-13).
    // New releases go in a separate immutable snapshot class.
    public static class ReleasedFirstPlayableEventCatalog
    {
        private static readonly EventCatalogSnapshot BaselineValue =
            new EventCatalogSnapshot(new GameEventDefinition[]
            {
                new GameEventDefinition("welcome-neighbor", "life", 2026, 1, 12,
                    0, 1, 1, 0, new[] { "character-created" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("join", ChoiceKey("welcome-neighbor", "join"),
                        "welcome-neighbor" + "-" + "join", new[] { "event/contact" }),
                    new EventChoiceDefinition("decline", ChoiceKey("welcome-neighbor", "decline"),
                        "welcome-neighbor" + "-" + "decline", Array.Empty<string>())
                    }, TitleKey("welcome-neighbor"), BodyKey("welcome-neighbor")),
                new GameEventDefinition("coffee-connection", "life", 2026, 1, 8,
                    0, 1, 1, 0, new[] { "event/contact" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("listen", ChoiceKey("coffee-connection", "listen"),
                        "coffee-connection" + "-" + "listen", new[] { "event/networking" }),
                    new EventChoiceDefinition("later", ChoiceKey("coffee-connection", "later"),
                        "coffee-connection" + "-" + "later", Array.Empty<string>())
                    }, TitleKey("coffee-connection"), BodyKey("coffee-connection")),
                new GameEventDefinition("mentor-introduction", "career", 2026, 1, 6,
                    0, 1, 1, 0, new[] { "event/networking" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("ask", ChoiceKey("mentor-introduction", "ask"),
                        "mentor-introduction" + "-" + "ask", new[] { "event/mentor" }),
                    new EventChoiceDefinition("thank", ChoiceKey("mentor-introduction", "thank"),
                        "mentor-introduction" + "-" + "thank", Array.Empty<string>())
                    }, TitleKey("mentor-introduction"), BodyKey("mentor-introduction")),
                new GameEventDefinition("workplace-lunch", "career", 2026, 1, 10,
                    0, 1, 1, 0, new[] { "employed" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("connect", ChoiceKey("workplace-lunch", "connect"),
                        "workplace-lunch" + "-" + "connect", new[] { "event/teamwork" }),
                    new EventChoiceDefinition("rest", ChoiceKey("workplace-lunch", "rest"),
                        "workplace-lunch" + "-" + "rest", Array.Empty<string>())
                    }, TitleKey("workplace-lunch"), BodyKey("workplace-lunch")),
                new GameEventDefinition("salary-reflection", "career", 2026, 1, 8,
                    0, 1, 1, 0, new[] { "employed" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("plan", ChoiceKey("salary-reflection", "plan"),
                        "salary-reflection" + "-" + "plan", new[] { "event/planning" }),
                    new EventChoiceDefinition("reflect", ChoiceKey("salary-reflection", "reflect"),
                        "salary-reflection" + "-" + "reflect", Array.Empty<string>())
                    }, TitleKey("salary-reflection"), BodyKey("salary-reflection")),
                new GameEventDefinition("weekend-study", "study", 2026, 1, 9,
                    0, 1, 1, 0, new[] { "course-active" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("join", ChoiceKey("weekend-study", "join"),
                        "weekend-study" + "-" + "join", new[] { "event/study-group" }),
                    new EventChoiceDefinition("solo", ChoiceKey("weekend-study", "solo"),
                        "weekend-study" + "-" + "solo", Array.Empty<string>())
                    }, TitleKey("weekend-study"), BodyKey("weekend-study")),
                new GameEventDefinition("study-friend", "study", 2026, 1, 7,
                    0, 1, 1, 0, new[] { "event/study-group" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("share", ChoiceKey("study-friend", "share"),
                        "study-friend" + "-" + "share", new[] { "event/study-circle" }),
                    new EventChoiceDefinition("keep", ChoiceKey("study-friend", "keep"),
                        "study-friend" + "-" + "keep", Array.Empty<string>())
                    }, TitleKey("study-friend"), BodyKey("study-friend")),
                new GameEventDefinition("first-market-day", "business", 2026, 2, 9,
                    0, 1, 1, 0, new[] { "business-active" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("visit", ChoiceKey("first-market-day", "visit"),
                        "first-market-day" + "-" + "visit", new[] { "event/local-customer" }),
                    new EventChoiceDefinition("skip", ChoiceKey("first-market-day", "skip"),
                        "first-market-day" + "-" + "skip", Array.Empty<string>())
                    }, TitleKey("first-market-day"), BodyKey("first-market-day")),
                new GameEventDefinition("supplier-chat", "business", 2026, 2, 7,
                    0, 1, 1, 0, new[] { "business-active", "event/local-customer" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("learn", ChoiceKey("supplier-chat", "learn"),
                        "supplier-chat" + "-" + "learn", new[] { "event/supplier" }),
                    new EventChoiceDefinition("wait", ChoiceKey("supplier-chat", "wait"),
                        "supplier-chat" + "-" + "wait", Array.Empty<string>())
                    }, TitleKey("supplier-chat"), BodyKey("supplier-chat")),
                new GameEventDefinition("customer-feedback", "business", 2026, 2, 8,
                    0, 1, 1, 0, new[] { "business-active" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("listen", ChoiceKey("customer-feedback", "listen"),
                        "customer-feedback" + "-" + "listen", new[] { "event/customer-listening" }),
                    new EventChoiceDefinition("later", ChoiceKey("customer-feedback", "later"),
                        "customer-feedback" + "-" + "later", Array.Empty<string>())
                    }, TitleKey("customer-feedback"), BodyKey("customer-feedback")),
                new GameEventDefinition("founder-community", "life", 2026, 1, 9,
                    0, 1, 1, 0, new[] { "unemployed", "character-created" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("attend", ChoiceKey("founder-community", "attend"),
                        "founder-community" + "-" + "attend", new[] { "event/founder-peer" }),
                    new EventChoiceDefinition("decline", ChoiceKey("founder-community", "decline"),
                        "founder-community" + "-" + "decline", Array.Empty<string>())
                    }, TitleKey("founder-community"), BodyKey("founder-community")),
                new GameEventDefinition("pitch-practice", "life", 2026, 1, 6,
                    0, 1, 1, 0, new[] { "event/founder-peer" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("practice", ChoiceKey("pitch-practice", "practice"),
                        "pitch-practice" + "-" + "practice", new[] { "event/pitch-practice" }),
                    new EventChoiceDefinition("observe", ChoiceKey("pitch-practice", "observe"),
                        "pitch-practice" + "-" + "observe", Array.Empty<string>())
                    }, TitleKey("pitch-practice"), BodyKey("pitch-practice")),
                new GameEventDefinition("mentor-follow-up", "career", 2026, 1, 7,
                    0, 1, 1, 0, new[] { "event/mentor" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("prepare", ChoiceKey("mentor-follow-up", "prepare"),
                        "mentor-follow-up" + "-" + "prepare", new[] { "event/mentor-notes" }),
                    new EventChoiceDefinition("listen", ChoiceKey("mentor-follow-up", "listen"),
                        "mentor-follow-up" + "-" + "listen", Array.Empty<string>())
                    }, TitleKey("mentor-follow-up"), BodyKey("mentor-follow-up")),
                new GameEventDefinition("team-code-review", "career", 2026, 1, 8,
                    0, 1, 1, 0, new[] { "employed", "event/teamwork" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("share", ChoiceKey("team-code-review", "share"),
                        "team-code-review" + "-" + "share", new[] { "event/peer-learning" }),
                    new EventChoiceDefinition("observe", ChoiceKey("team-code-review", "observe"),
                        "team-code-review" + "-" + "observe", Array.Empty<string>())
                    }, TitleKey("team-code-review"), BodyKey("team-code-review")),
                new GameEventDefinition("savings-checkin", "life", 2026, 1, 8,
                    0, 1, 1, 0, new[] { "event/planning" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("review", ChoiceKey("savings-checkin", "review"),
                        "savings-checkin" + "-" + "review", new[] { "event/budget-awareness" }),
                    new EventChoiceDefinition("postpone", ChoiceKey("savings-checkin", "postpone"),
                        "savings-checkin" + "-" + "postpone", Array.Empty<string>())
                    }, TitleKey("savings-checkin"), BodyKey("savings-checkin")),
                new GameEventDefinition("study-presentation", "study", 2026, 1, 7,
                    0, 1, 1, 0, new[] { "event/study-circle" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("present", ChoiceKey("study-presentation", "present"),
                        "study-presentation" + "-" + "present", new[] { "event/study-presenter" }),
                    new EventChoiceDefinition("attend", ChoiceKey("study-presentation", "attend"),
                        "study-presentation" + "-" + "attend", Array.Empty<string>())
                    }, TitleKey("study-presentation"), BodyKey("study-presentation")),
                new GameEventDefinition("service-improvement", "business", 2026, 2, 8,
                    0, 1, 1, 0, new[] { "business-active", "event/customer-listening" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("discuss", ChoiceKey("service-improvement", "discuss"),
                        "service-improvement" + "-" + "discuss", new[] { "event/service-ideas" }),
                    new EventChoiceDefinition("consider", ChoiceKey("service-improvement", "consider"),
                        "service-improvement" + "-" + "consider", Array.Empty<string>())
                    }, TitleKey("service-improvement"), BodyKey("service-improvement")),
                new GameEventDefinition("supplier-follow-up", "business", 2026, 2, 6,
                    0, 1, 1, 0, new[] { "business-active", "event/supplier" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("visit", ChoiceKey("supplier-follow-up", "visit"),
                        "supplier-follow-up" + "-" + "visit", new[] { "event/supplier-insight" }),
                    new EventChoiceDefinition("later", ChoiceKey("supplier-follow-up", "later"),
                        "supplier-follow-up" + "-" + "later", Array.Empty<string>())
                    }, TitleKey("supplier-follow-up"), BodyKey("supplier-follow-up")),
                new GameEventDefinition("pitch-feedback", "life", 2026, 1, 7,
                    0, 1, 1, 0, new[] { "event/pitch-practice" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("revise", ChoiceKey("pitch-feedback", "revise"),
                        "pitch-feedback" + "-" + "revise", new[] { "event/pitch-feedback" }),
                    new EventChoiceDefinition("reflect", ChoiceKey("pitch-feedback", "reflect"),
                        "pitch-feedback" + "-" + "reflect", Array.Empty<string>())
                    }, TitleKey("pitch-feedback"), BodyKey("pitch-feedback")),
                new GameEventDefinition("community-collaboration", "life", 2026, 1, 6,
                    0, 1, 1, 0, new[] { "event/networking", "event/founder-peer" }, Array.Empty<string>(),
                    new[]
                    {
                    new EventChoiceDefinition("help", ChoiceKey("community-collaboration", "help"),
                        "community-collaboration" + "-" + "help", new[] { "event/community-trust" }),
                    new EventChoiceDefinition("observe", ChoiceKey("community-collaboration", "observe"),
                        "community-collaboration" + "-" + "observe", Array.Empty<string>())
                    }, TitleKey("community-collaboration"), BodyKey("community-collaboration"))
            }, new[]
            {
                new EventCategoryPolicy("life", 2, 7, 2),
                new EventCategoryPolicy("career", 3, 7, 1),
                new EventCategoryPolicy("study", 3, 7, 1),
                new EventCategoryPolicy("business", 3, 7, 1)
            }, ReleasedFirstPlayableEventCopy.Vietnamese);

        public static EventCatalogSnapshot LegacyBaseline => BaselineValue;

        // Bootstrap passes the LIVE catalog separately. Once it changes, V4
        // cold replay still resolves old pinned/unpinned runs to BaselineValue.
        // The baseline never pulls definitions from the live content provider.
        public static EventCatalogArchive CreateArchive(
            IEnumerable<GameEventDefinition> current,
            IEnumerable<EventCategoryPolicy> policies) =>
            new EventCatalogArchive(new EventCatalogSnapshot(current, policies),
                new[] { BaselineValue }, legacyBaseline: BaselineValue);

        private static string TitleKey(string id) => "event.story." + id + ".title";
        private static string BodyKey(string id) => "event.story." + id + ".body";
        private static string ChoiceKey(string id, string choice) =>
            "event.story." + id + ".choice." + choice;
    }
}

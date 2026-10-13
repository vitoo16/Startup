#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using StartupLife.Content;
using StartupLife.Core;
using StartupLife.Simulation;

namespace StartupLife.Tests.EditMode
{
    public sealed class EventChoiceAvailabilityTests
    {
        [Test]
        public void AuthoredBusinessExpenseKeepsFreeExitAndExactCashBoundary()
        {
            var story = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "local-promotion-print");
            var paid = story.Choices.Single(x => x.Id == "print");
            var free = story.Choices.Single(x => x.Id == "wait");
            Assert.That(paid.Reward, Is.Not.Null);
            Assert.That(paid.Reward!.CashDeltaVnd, Is.EqualTo(-40_000L));
            Assert.That(free.Reward, Is.Null,
                "No optional event may force an unaffordable payout to close a ticket.");

            var savedState = new GameState { Cash = 39_999L };
            Assert.That(EventDecisionJournal.RewardUnavailableReason(
                savedState, paid.Reward), Is.EqualTo("event.choice.insufficient_funds"));
            savedState.Cash = 40_000L;
            Assert.That(EventDecisionJournal.RewardUnavailableReason(
                savedState, paid.Reward), Is.Empty);
        }

        [Test]
        public void RewardPreviewRejectsUnpayableCashAndCareerXpBeforeChoice()
        {
            var state = new GameState { Cash = long.MaxValue };
            Assert.That(EventDecisionJournal.RewardUnavailableReason(
                state, new EventChoiceReward(100_000L, 0)),
                Is.EqualTo("event.choice.reward_unavailable"));

            state.Cash = 100_000L;
            var career = new EventChoiceReward(0, 60);
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, career),
                Is.EqualTo("event.choice.reward_unavailable"));
            state.Employment = new EmploymentState { Xp = long.MaxValue - 59L };
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, career),
                Is.EqualTo("event.choice.reward_unavailable"));
            state.Employment.Xp = long.MaxValue - 60L;
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, career),
                Is.Empty);
            state.Employment.EndedIso = "2026-10-13";
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, career),
                Is.EqualTo("event.choice.reward_unavailable"));
        }

        [TestCase("mentor-workshop-materials", "contribute", -25_000L, 45L)]
        [TestCase("supplier-quality-sample", "sample", -70_000L, 0L)]
        [TestCase("customer-trust-repair", "compensate", -55_000L, 0L)]
        [TestCase("delivery-incident", "support", -90_000L, 0L)]
        [TestCase("supplier-backorder", "reserve", -120_000L, 0L)]
        public void NewTradeOffsHaveAuthoredCostsAndNoCostFallback(
            string eventId, string paidId, long cashCost, long careerXp)
        {
            var story = FirstPlayableEventContent.Definitions.Single(x => x.Id == eventId);
            var choice = story.Choices.Single(x => x.Id == paidId);
            Assert.That(choice.Reward, Is.Not.Null);
            Assert.That(choice.Reward!.CashDeltaVnd, Is.EqualTo(cashCost));
            Assert.That(choice.Reward.CareerXpDelta, Is.EqualTo(careerXp));
            Assert.That(story.Choices.Any(x => x.Reward == null), Is.True);

            var state = new GameState { Cash = -cashCost - 1L };
            if (careerXp > 0)
                state.Employment = new EmploymentState { Xp = 0L };
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, choice.Reward),
                Is.EqualTo("event.choice.insufficient_funds"));

            state.Cash = -cashCost;
            Assert.That(EventDecisionJournal.RewardUnavailableReason(state, choice.Reward),
                Is.Empty);
            if (careerXp > 0)
            {
                state.Employment = null;
                Assert.That(EventDecisionJournal.RewardUnavailableReason(state, choice.Reward),
                    Is.EqualTo("event.choice.reward_unavailable"));
            }
        }

        [TestCase("customer-trust-repair", "follow-up", "customer-care-follow-up")]
        [TestCase("supplier-quality-sample", "sample", "supplier-sample-review")]
        [TestCase("mentor-workshop-materials", "contribute", "mentor-workshop-reflection")]
        [TestCase("delivery-incident", "coordinate", "delivery-incident-follow-up")]
        [TestCase("supplier-backorder", "source", "supplier-backorder-follow-up")]
        public void NewConsequencesLeadToReachableFollowUpStories(
            string sourceId, string selectedChoiceId, string continuationId)
        {
            var source = FirstPlayableEventContent.Definitions.Single(x => x.Id == sourceId);
            var selection = source.Choices.Single(x => x.Id == selectedChoiceId);
            var continuation = FirstPlayableEventContent.Definitions.Single(
                x => x.Id == continuationId);
            Assert.That(continuation.RequiredTags.Where(tag =>
                tag.StartsWith("event/", StringComparison.Ordinal)).Any(
                    tag => selection.GrantedTags.Contains(tag)), Is.True,
                "A follow-up must require a tag from the exact selected outcome.");
            Assert.That(continuation.Choices.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(continuation.Choices.All(x => x.Reward == null), Is.True,
                "A recovery/reflection step must not require another payment.");
        }

        [Test]
        public void AllCurrentStoriesProvideCompleteVietnameseNarrativeAndChoices()
        {
            var entries = FirstPlayableEventContent.VietnameseEntries;
            var definitions = FirstPlayableEventContent.Definitions;
            var copy = entries.ToDictionary(x => x.Key, x => x.Value,
                StringComparer.Ordinal);
            Assert.That(copy.Count, Is.EqualTo(entries.Count),
                "Duplicate localized keys must fail authoring.");
            foreach (var story in definitions)
            {
                Assert.That(copy.TryGetValue(story.TitleKey, out var title) &&
                    !string.IsNullOrWhiteSpace(title), Is.True, story.Id + " title");
                Assert.That(copy.TryGetValue(story.BodyKey, out var body) &&
                    !string.IsNullOrWhiteSpace(body), Is.True, story.Id + " body");
                foreach (var choice in story.Choices)
                    Assert.That(copy.TryGetValue(choice.LabelKey, out var label) &&
                        !string.IsNullOrWhiteSpace(label), Is.True,
                        story.Id + " choice " + choice.Id);
            }
            Assert.That(entries.Count, Is.EqualTo(140),
                "Every one of the 35 current stories has a title, body and two choices.");
        }

        [Test]
        public void RetainedTwentyNineStoryVersionRemainsIndependentlyResolvable()
        {
            var baseline = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
            var retained = RetainedM15DevelopmentEventCatalogs.Create(baseline);
            var previous = retained.Single(x => x.Definitions.Count == 29);
            Assert.That(previous.Definitions.Sum(x => x.Choices.Count), Is.EqualTo(58));
            Assert.That(previous.VietnameseText.Count, Is.EqualTo(116));
            Assert.That(previous.Definitions.Any(x => x.Id == "delivery-incident"),
                Is.False, "New recoverable challenges must not be backported into older saves.");

            var archive = ReleasedFirstPlayableEventCatalog.CreateArchive(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies);
            var pinned = EventDecisionJournal.New("retained-29", 31);
            pinned.CatalogFingerprint = previous.Fingerprint;
            Assert.That(archive.Resolve(pinned).Fingerprint, Is.EqualTo(previous.Fingerprint));
            Assert.That(archive.Resolve(pinned).VietnameseText.Count, Is.EqualTo(116));
        }

        [Test]
        public void RetainedThirtyThreeStoryVersionPreservesPreviousBusinessChallenges()
        {
            var historical = RetainedM15DevelopmentEventCatalogs.Create(
                ReleasedFirstPlayableEventCatalog.LegacyBaseline);
            var frozen = historical.Single(x => x.Definitions.Count == 33);
            Assert.That(frozen.Definitions.Sum(x => x.Choices.Count), Is.EqualTo(66));
            Assert.That(frozen.VietnameseText.Count, Is.EqualTo(132));
            Assert.That(frozen.Definitions.Any(x => x.Id == "delivery-incident"), Is.True);
            Assert.That(frozen.Definitions.Any(x => x.Id == "course-study-questions"), Is.False);
            var archive = ReleasedFirstPlayableEventCatalog.CreateArchive(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies);
            var pinned = EventDecisionJournal.New("retained-33", 41);
            pinned.CatalogFingerprint = frozen.Fingerprint;
            Assert.That(archive.Resolve(pinned).Fingerprint, Is.EqualTo(frozen.Fingerprint));
            Assert.That(archive.Resolve(pinned).VietnameseText.Count, Is.EqualTo(132));
        }

        [Test]
        public void CourseQuestionFollowUpOnlyUsesCommittedEventTag()
        {
            var initial = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "course-study-questions");
            var followUp = FirstPlayableEventContent.Definitions.Single(x =>
                x.Id == "course-question-follow-up");
            Assert.That(initial.RequiredTags, Does.Contain("course-active"));
            Assert.That(initial.Choices.Single(x => x.Id == "prepare").GrantedTags,
                Does.Contain("event/course-questions-prepared"));
            Assert.That(initial.Choices.Single(x => x.Id == "review").GrantedTags,
                Does.Not.Contain("event/course-questions-prepared"));
            Assert.That(followUp.RequiredTags,
                Does.Contain("event/course-questions-prepared"));
            Assert.That(followUp.Choices.All(x => x.Reward == null), Is.True);
            Assert.That(initial.Choices.All(x => x.Reward == null), Is.True);
        }

        [Test]
        public void NewExpenseLeavesHistoricalReleaseRostersUnchanged()
        {
            var released = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
            Assert.That(released.Definitions.Count, Is.EqualTo(20));
            Assert.That(released.Definitions.Sum(x => x.Choices.Count), Is.EqualTo(40));
            Assert.That(released.VietnameseText.Count, Is.EqualTo(80));
            Assert.That(released.Definitions.Any(x => x.Id == "local-promotion-print"),
                Is.False);
            Assert.That(FirstPlayableEventContent.Definitions.Count, Is.EqualTo(35));
            Assert.That(EventCatalogFingerprint.Compute(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies),
                Is.Not.EqualTo(released.Fingerprint));
        }
    }
}

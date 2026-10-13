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

        [Test]
        public void NewExpenseLeavesHistoricalReleaseRostersUnchanged()
        {
            var released = ReleasedFirstPlayableEventCatalog.LegacyBaseline;
            Assert.That(released.Definitions.Count, Is.EqualTo(20));
            Assert.That(released.Definitions.Sum(x => x.Choices.Count), Is.EqualTo(40));
            Assert.That(released.VietnameseText.Count, Is.EqualTo(80));
            Assert.That(released.Definitions.Any(x => x.Id == "local-promotion-print"),
                Is.False);
            Assert.That(FirstPlayableEventContent.Definitions.Count, Is.EqualTo(29));
            Assert.That(EventCatalogFingerprint.Compute(
                FirstPlayableEventContent.Definitions, FirstPlayableEventContent.Policies),
                Is.Not.EqualTo(released.Fingerprint));
        }
    }
}

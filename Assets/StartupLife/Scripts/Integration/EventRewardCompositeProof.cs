#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

namespace StartupLife.Integration
{
    // This is a detached proposal, NOT a serializable save or a grant.
    // Callers may inspect/mutate candidates for tests, but Validate always
    // reconstructs both ledgers from independently verified source bytes.
    public sealed class EventRewardCompositeCandidate
    {
        public GameState Core { get; }
        public EventJournalState Journal { get; }
        public EventRewardSettlementPlan Plan { get; }

        internal EventRewardCompositeCandidate(GameState core,
            EventJournalState journal, EventRewardSettlementPlan plan)
        {
            Core = core;
            Journal = journal;
            Plan = plan;
        }
    }

    // Reuses the existing V3 candidate evaluator and V4 journal planner.
    // Neither a physical CAS writer nor a special V3 replay exemption exists here.
    // Both are intentionally blocked until a single cold replay can prove the
    // combined V3 financial receipt and V4 outcome at every historical frontier.
    public static class EventRewardCompositeProof
    {
        public static EventRewardCompositeCandidate Prepare(byte[] verifiedV4Envelope,
            EventV4CompatibilitySerializer serializer, EventV4CoreCommandRunner coreRunner,
            IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies,
            string commandId, string ticketId, string choiceId)
        {
            if (verifiedV4Envelope == null || serializer == null || coreRunner == null ||
                definitions == null || policies == null)
                throw new ArgumentNullException("Composite reward sources are required.");
            // Do not accept an in-memory migration as proof of a persisted pending
            // V4 ticket. A schema-4 envelope must have passed its full cold replay.
            var envelope = JsonSaveSerializer.ReadObject<SaveEnvelope>(verifiedV4Envelope);
            if (envelope.SchemaVersion != 4)
                throw new ArgumentException("A committed schema-4 event source is required.");
            var loaded = serializer.DeserializeAndValidate(verifiedV4Envelope);
            if (loaded.Status != LoadStatus.Valid || loaded.State?.EventV4?.Journal == null)
                throw new ArgumentException("The composite source failed V4 cold replay.");
            var current = loaded.State;
            var plan = EventDecisionJournal.PlanReward(current, definitions, policies,
                commandId, ticketId, choiceId);
            var staged = EventDecisionJournal.StagePlannedRewardResolution(
                current.EventV4.Journal, definitions, policies, plan);
            if (staged.Status != EventJournalStatus.Resolved)
                throw new ArgumentException("The authored reward journal cannot be staged.");
            var core = coreRunner.PrepareRewardReceiptCandidate(
                Convert.FromBase64String(current.EventV4.EmbeddedV3EnvelopeBase64),
                plan, current.EventV4.Journal, definitions);
            var receipt = core.Receipts.Last();
            if (core.Revision != plan.CoreRevision ||
                staged.Next.Revision != current.EventV4.Journal.Revision + 1 ||
                checked(core.Revision + staged.Next.Revision) != plan.GlobalRevision ||
                plan.GlobalRevision != checked(current.Revision + 2L) ||
                receipt.CommandId != plan.CoreReceiptCommandId ||
                !plan.MatchesCoreCommand(GameCommand.ParseCanonicalPayload(receipt.Payload)) ||
                receipt.MinutesConsumed != 0 || receipt.Revision != core.Revision ||
                staged.Next.PendingTicketId.Length != 0 ||
                staged.Next.Tickets.Last().ResolutionCommandId != plan.ResolutionCommandId ||
                staged.Next.OutcomeReceipts.Last().OutcomeId != plan.OutcomeId)
                throw new ArgumentException("The staged V3/V4 reward pair has divergent receipt frontiers.");
            return new EventRewardCompositeCandidate(core, staged.Next, plan);
        }

        // Only the enclosing V4 wire owns these bytes. V3 standalone replay
        // never accepts the inner payout checkpoint without its matching journal.
        internal static EventV4Payload CreatePayload(EventRewardCompositeCandidate candidate,
            EventV4CoreCommandRunner runner)
        {
            if (candidate?.Core?.EconomicV3 == null || candidate.Journal == null ||
                runner == null)
                throw new ArgumentException("A complete staged reward pair is required.");
            var inner = runner.EncodeRewardCandidate(candidate.Core);
            string checksum;
            using (var sha = SHA256.Create())
                checksum = BitConverter.ToString(sha.ComputeHash(inner))
                    .Replace("-", "").ToLowerInvariant();
            return new EventV4Payload
            {
                EmbeddedV3EnvelopeBase64 = Convert.ToBase64String(inner),
                EmbeddedV3EnvelopeSha256 = checksum,
                Journal = candidate.Journal.Copy()
            };
        }

        // This proves candidate integrity without trusting the caller's
        // command amounts, event outcome, ledger values, or mutable DTOs.
        // It does NOT mean the pair is eligible for publishing to disk.
        public static void Validate(byte[] originalV4Envelope,
            EventRewardCompositeCandidate candidate,
            EventV4CompatibilitySerializer serializer, EventV4CoreCommandRunner coreRunner,
            IReadOnlyList<GameEventDefinition> definitions,
            IReadOnlyList<EventCategoryPolicy> policies)
        {
            if (candidate?.Core?.EconomicV3 == null ||
                !ReferenceEquals(candidate.Core.EconomicV3.Current, candidate.Core) ||
                candidate.Journal == null || candidate.Plan == null)
                throw new ArgumentException("Detached V3/V4 candidate is incomplete.");
            var expected = Prepare(originalV4Envelope, serializer, coreRunner,
                definitions, policies, candidate.Plan.ResolutionCommandId,
                candidate.Plan.TicketId, candidate.Plan.ChoiceId);
            var claimed = candidate.Plan;
            var proof = expected.Plan;
            if (claimed.RunId != proof.RunId ||
                claimed.CoreReceiptCommandId != proof.CoreReceiptCommandId ||
                claimed.EventId != proof.EventId ||
                claimed.OutcomeId != proof.OutcomeId ||
                claimed.CheckedDateIso != proof.CheckedDateIso ||
                claimed.ExpectedCashVnd != proof.ExpectedCashVnd ||
                claimed.ExpectedCareerXp != proof.ExpectedCareerXp ||
                claimed.CashDeltaVnd != proof.CashDeltaVnd ||
                claimed.CareerXpDelta != proof.CareerXpDelta ||
                claimed.CoreRevision != proof.CoreRevision ||
                claimed.GlobalRevision != proof.GlobalRevision ||
                !JsonSaveSerializer.WriteObject(candidate.Core)
                    .SequenceEqual(JsonSaveSerializer.WriteObject(expected.Core)) ||
                !JsonSaveSerializer.WriteObject(candidate.Core.EconomicV3)
                    .SequenceEqual(JsonSaveSerializer.WriteObject(expected.Core.EconomicV3)) ||
                !JsonSaveSerializer.WriteObject(candidate.Journal)
                    .SequenceEqual(JsonSaveSerializer.WriteObject(expected.Journal)))
                throw new ArgumentException("Reward candidate differs from verified V3/V4 receipt replay.");
        }
    }
}

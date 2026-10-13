#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Application;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

namespace StartupLife.Integration
{
    // Evaluates a prospective V3 command with the real GameSession and V3 receipt
    // processor. The temporary store is NEVER authoritative or persisted: only the
    // outer V4 AtomicFileSaveStore may publish the resulting envelope.
    public sealed class EventV4CoreCommandRunner
    {
        private readonly ContentCatalog content;
        private readonly V3SaveCompatibilitySerializer serializer;
        private readonly BusinessEconomicCatalog economics;
        private readonly IHistoricalEconomicRulesResolver archives;
        public ContentCatalog Content => content;

        public EventV4CoreCommandRunner(ContentCatalog content,
            V3SaveCompatibilitySerializer serializer, BusinessEconomicCatalog economics,
            IHistoricalEconomicRulesResolver archives)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.economics = economics ?? throw new ArgumentNullException(nameof(economics));
            this.archives = archives ?? throw new ArgumentNullException(nameof(archives));
        }

        public CommandResult Evaluate(byte[] verifiedV3Envelope, CommandEnvelope request,
            out byte[]? candidateV3) =>
            Evaluate(verifiedV3Envelope, request, out candidateV3, null, null);

        // This journal was already authenticated by the outer V4 cold read.
        // The scoped serializer can replay prior rewarded V3 receipts without
        // permitting independent schema-3 execution/persistence of payouts.
        public CommandResult Evaluate(byte[] verifiedV3Envelope, CommandEnvelope request,
            out byte[]? candidateV3, EventJournalState? verifiedJournal,
            IReadOnlyList<GameEventDefinition>? definitions)
        {
            candidateV3 = null;
            if (verifiedV3Envelope == null || request == null)
                throw new ArgumentNullException("V3 core command input");
            ISaveSerializer active = verifiedJournal == null ? serializer :
                new V4ScopedCoreSerializer(serializer, verifiedJournal,
                    definitions ?? throw new ArgumentNullException(nameof(definitions)));
            var store = new TentativeV3Store(active, verifiedV3Envelope);
            var read = store.Read();
            if (read.Status != LoadStatus.Valid || read.State == null ||
                read.State.SaveVersion != 3)
                throw new ArgumentException("An independently validated V3 source is required.");
            var session = new GameSession(read.State, content, active, store,
                economicRules: economics, archivedRules: archives);
            var inner = new CommandEnvelope(request.RunId, request.CommandId,
                read.State.Revision, request.Command);
            var result = session.Execute(inner);
            if (result.Status == CommandStatus.Committed)
                candidateV3 = session.ExportCheckpoint();
            return result;
        }

        // Staged receipt only. V3 serializer/replay must still reject this
        // until the V4 journal provides an authoritative cross-ledger proof.
        // No disk writer or CAS is exposed by this method.
        public GameState PrepareRewardReceiptCandidate(byte[] verifiedV3Envelope,
            EventRewardSettlementPlan plan, EventJournalState? verifiedJournal = null,
            IReadOnlyList<GameEventDefinition>? definitions = null)
        {
            if (verifiedV3Envelope == null || plan == null)
                throw new ArgumentNullException("Reward source and proof are required.");
            ISaveSerializer active = verifiedJournal == null ? serializer :
                new V4ScopedCoreSerializer(serializer, verifiedJournal,
                    definitions ?? throw new ArgumentNullException(nameof(definitions)));
            var read = active.DeserializeAndValidate(verifiedV3Envelope);
            var candidate = read.State;
            if (read.Status != LoadStatus.Valid || candidate?.SaveVersion != 3 ||
                candidate.EconomicV3?.Current != candidate ||
                candidate.EconomicV3.Activation == null ||
                candidate.EconomicV3.EconomicRecords == null ||
                candidate.Revision != candidate.Receipts.Count ||
                candidate.Revision != plan.CoreRevision - 1L ||
                candidate.RunId != plan.RunId ||
                candidate.Receipts.Any(x => x.CommandId == plan.CoreReceiptCommandId ||
                    x.CommandId == plan.ResolutionCommandId))
                throw new ArgumentException("An independently verified V3 reward frontier is required.");
            var operationId = candidate.NewOperation();
            new SimulationEngine(content, candidate.EconomicV3.EconomicRecords)
                .ApplyPlannedEventReward(candidate, plan, operationId);
            var processor = new V3EconomicReceiptProcessor(content, economics,
                candidate.EconomicV3.Activation.ToAnchor());
            ReceiptCandidateFinalizer.Append(candidate, plan.ToCoreCommand(),
                plan.CoreReceiptCommandId, operationId, plan.MinutesConsumed, processor);
            candidate.EconomicV3.EconomicRecords.Validate();
            return candidate;
        }

        internal byte[] EncodeRewardCandidate(GameState candidate) =>
            serializer.PrepareEmbeddedV4Candidate(candidate);

        // In-memory candidate buffer only. It reuses the authoritative V3 serializer
        // and never implements a second on-disk CAS, backup or replay engine.
        private sealed class TentativeV3Store : ISaveStore
        {
            private readonly ISaveSerializer serializer;
            private byte[] bytes;

            internal TentativeV3Store(ISaveSerializer serializer, byte[] checkpoint)
            {
                this.serializer = serializer;
                bytes = (byte[])checkpoint.Clone();
            }

            public LoadResult Read() => serializer.DeserializeAndValidate(bytes);

            public WriteStatus Commit(byte[] candidate, long expectedRevision)
            {
                var before = Read();
                var next = serializer.DeserializeAndValidate(candidate);
                if (before.Status != LoadStatus.Valid || before.State == null ||
                    next.Status != LoadStatus.Valid || next.State == null ||
                    before.State.Revision != expectedRevision ||
                    next.State.Revision != checked(expectedRevision + 1))
                    return WriteStatus.Failed;
                bytes = (byte[])candidate.Clone();
                return WriteStatus.Committed;
            }
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using StartupLife.Core;
using StartupLife.Infrastructure;
using StartupLife.Simulation;

namespace StartupLife.Integration
{
    // Ephemeral source-only V3 serializer scoped to one cold-verified V4 journal.
    // It reuses the exact existing V3 receipt replay, with the V4 financial
    // proof explicitly supplied. It has NO physical save authority.
    internal sealed class V4ScopedCoreSerializer : ISaveSerializer
    {
        private readonly V3SaveCompatibilitySerializer core;
        private readonly EventJournalState journal;
        private readonly IReadOnlyList<GameEventDefinition> definitions;

        internal V4ScopedCoreSerializer(V3SaveCompatibilitySerializer core,
            EventJournalState journal, IReadOnlyList<GameEventDefinition> definitions)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        }

        public LoadResult DeserializeAndValidate(byte[] bytes)
        {
            var proof = new EventRewardReplayProof(journal, definitions);
            var result = core.DeserializeAndValidateWithFrontiers(
                bytes, _ => { }, proof);
            if (result.Status != LoadStatus.Valid)
                return result;
            try { proof.AssertComplete(); }
            catch (ArgumentException)
            {
                return new LoadResult(LoadStatus.Corrupt, reason: "save.v4_reward_receipt");
            }
            return result;
        }

        public byte[] Serialize(GameState state)
        {
            var candidate = core.PrepareEmbeddedV4Candidate(state);
            var admission = DeserializeAndValidate(candidate);
            if (admission.Status != LoadStatus.Valid)
                throw new ArgumentException("Unverified V3 reward history in scoped V4 candidate.");
            return candidate;
        }
    }
}

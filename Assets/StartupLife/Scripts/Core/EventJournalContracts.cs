#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // Prospective M15 wire contract. This is NOT yet a member of the active GameState save.
    [DataContract]
    public sealed class EventTicketRecord
    {
        [DataMember(Order = 0)] public string Id { get; set; } = "";
        [DataMember(Order = 1)] public string EventId { get; set; } = "";
        [DataMember(Order = 2)] public string CategoryId { get; set; } = "";
        [DataMember(Order = 3)] public string OpenedDateIso { get; set; } = "";
        [DataMember(Order = 4)] public string ChoiceId { get; set; } = "";
        [DataMember(Order = 5)] public string ResolutionCommandId { get; set; } = "";
        [DataMember(Order = 6)] public string OutcomeId { get; set; } = "";
        // Exact immutable offer context: needed to reproduce weighted selection on cold read.
        [DataMember(Order = 7)] public string OriginDateIso { get; set; } = "";
        [DataMember(Order = 8)] public int Stage { get; set; }
        [DataMember(Order = 9)] public List<string> Tags { get; set; } = new List<string>();
        [DataMember(Order = 10)] public string OfferCommandId { get; set; } = "";
        // Inner V3 receipt frontier when the authoritative game state opened this event.
        [DataMember(Order = 11)] public long CoreRevisionAtOffer { get; set; } = -1;

        public EventTicketRecord Copy() => new EventTicketRecord
        {
            Id = Id, EventId = EventId, CategoryId = CategoryId, OpenedDateIso = OpenedDateIso,
            ChoiceId = ChoiceId, ResolutionCommandId = ResolutionCommandId, OutcomeId = OutcomeId,
            OriginDateIso = OriginDateIso, Stage = Stage, Tags = new List<string>(Tags),
            OfferCommandId = OfferCommandId, CoreRevisionAtOffer = CoreRevisionAtOffer
        };
    }

    // One effect receipt for exactly one resolved event ticket. Event-scoped tags
    // affect later eligibility, not the V3 wallet/career/skill ledgers.
    [DataContract]
    public sealed class EventOutcomeReceipt
    {
        [DataMember(Order = 0)] public string Id { get; set; } = "";
        [DataMember(Order = 1)] public string TicketId { get; set; } = "";
        [DataMember(Order = 2)] public string ResolutionCommandId { get; set; } = "";
        [DataMember(Order = 3)] public string OutcomeId { get; set; } = "";
        [DataMember(Order = 4)] public List<string> GrantedTags { get; set; } = new List<string>();

        public EventOutcomeReceipt Copy() => new EventOutcomeReceipt
        {
            Id = Id, TicketId = TicketId,
            ResolutionCommandId = ResolutionCommandId, OutcomeId = OutcomeId,
            GrantedTags = new List<string>(GrantedTags)
        };
    }

    // An evaluated day with no eligible event still needs a durable negative
    // result; otherwise a cold retry can recheck the day with a different state.
    [DataContract]
    public sealed class EventNoEligibleDayRecord
    {
        [DataMember(Order = 0)] public string CommandId { get; set; } = "";
        [DataMember(Order = 1)] public string DateIso { get; set; } = "";
        [DataMember(Order = 2)] public string OriginDateIso { get; set; } = "";
        [DataMember(Order = 3)] public int Stage { get; set; }
        [DataMember(Order = 4)] public List<string> Tags { get; set; } = new List<string>();
        [DataMember(Order = 5)] public long CoreRevisionAtCheck { get; set; } = -1;

        public EventNoEligibleDayRecord Copy() => new EventNoEligibleDayRecord
        {
            CommandId = CommandId, DateIso = DateIso, OriginDateIso = OriginDateIso,
            Stage = Stage, Tags = new List<string>(Tags),
            CoreRevisionAtCheck = CoreRevisionAtCheck
        };
    }

    // Durable batch request. Step receipts stay in the authoritative inner V3
    // receipt stream: no second time engine, no extra copy of each child receipt.
    [DataContract]
    public sealed class EventAdvanceBatchRecord
    {
        [DataMember(Order = 0)] public string RootCommandId { get; set; } = "";
        [DataMember(Order = 1)] public string Mode { get; set; } = "";
        [DataMember(Order = 2)] public string OpenedDateIso { get; set; } = "";
        [DataMember(Order = 3)] public string TargetDateIso { get; set; } = "";
        [DataMember(Order = 4)] public long CoreRevisionAtStart { get; set; } = -1;

        public EventAdvanceBatchRecord Copy() => new EventAdvanceBatchRecord
        {
            RootCommandId = RootCommandId, Mode = Mode,
            OpenedDateIso = OpenedDateIso, TargetDateIso = TargetDateIso,
            CoreRevisionAtStart = CoreRevisionAtStart
        };
    }

    [DataContract]
    public sealed class EventJournalState
    {
        [DataMember(Order = 0)] public string RunId { get; set; } = "";
        [DataMember(Order = 1)] public long Revision { get; set; }
        [DataMember(Order = 2)] public long NextTicketSequence { get; set; } = 1;
        [DataMember(Order = 3)] public uint EventRng { get; set; }
        [DataMember(Order = 4)] public string PendingTicketId { get; set; } = "";
        [DataMember(Order = 5)] public List<EventTicketRecord> Tickets { get; set; } = new List<EventTicketRecord>();
        [DataMember(Order = 6)] public uint InitialEventRng { get; set; }
        // Run-specific V4 activation date from verified V3 migration anchor.
        [DataMember(Order = 7)] public string OriginDateIso { get; set; } = "";
        [DataMember(Order = 8)] public List<EventOutcomeReceipt> OutcomeReceipts { get; set; } =
            new List<EventOutcomeReceipt>();
        [DataMember(Order = 9)] public List<EventAdvanceBatchRecord> AdvanceBatches { get; set; } =
            new List<EventAdvanceBatchRecord>();
        [DataMember(Order = 10)] public List<EventNoEligibleDayRecord> NoEligibleDays { get; set; } =
            new List<EventNoEligibleDayRecord>();

        // Early unshipped V4 checkpoints predate NoEligibleDays. DataContract
        // deserialization skips field initializers, so explicitly initialize
        // ONLY this additive field when absent. An explicit JSON null still
        // overwrites it and fails strict EventDecisionJournal.Validate.
        [OnDeserializing]
        private void InitializeLegacyDailyChecks(StreamingContext context)
        {
            NoEligibleDays = new List<EventNoEligibleDayRecord>();
        }

        public EventJournalState Copy() => new EventJournalState
        {
            RunId = RunId, Revision = Revision, NextTicketSequence = NextTicketSequence,
            EventRng = EventRng, InitialEventRng = InitialEventRng, PendingTicketId = PendingTicketId,
            OriginDateIso = OriginDateIso,
            Tickets = Tickets.ConvertAll(ticket => ticket.Copy()),
            OutcomeReceipts = OutcomeReceipts.ConvertAll(receipt => receipt.Copy()),
            AdvanceBatches = AdvanceBatches.ConvertAll(batch => batch.Copy()),
            NoEligibleDays = NoEligibleDays.ConvertAll(day => day.Copy())
        };
    }

    public sealed class EventCategoryPolicy
    {
        public string Id { get; }
        public int CooldownDays { get; }
        public int WindowDays { get; }
        public int MaxEventsPerWindow { get; }

        public EventCategoryPolicy(string id, int cooldownDays, int windowDays, int maxEventsPerWindow)
        {
            _ = new ContentId(id);
            if (cooldownDays < 0 || windowDays <= 0 || maxEventsPerWindow <= 0)
                throw new ArgumentException("Invalid event category cooldown or frequency limit.");
            Id = id;
            CooldownDays = cooldownDays;
            WindowDays = windowDays;
            MaxEventsPerWindow = maxEventsPerWindow;
        }
    }

    public enum EventJournalStatus { Offered, Resolved, AlreadyCommitted, NoEligibleEvent, Rejected }

    public sealed class EventJournalResult
    {
        public EventJournalStatus Status { get; }
        public string ReasonKey { get; }
        public EventJournalState Next { get; }
        public string TicketId { get; }
        public string OutcomeId { get; }

        public EventJournalResult(EventJournalStatus status, string reasonKey, EventJournalState next,
            string ticketId = "", string outcomeId = "")
        {
            Status = status;
            ReasonKey = reasonKey;
            Next = next;
            TicketId = ticketId;
            OutcomeId = outcomeId;
        }
    }
}

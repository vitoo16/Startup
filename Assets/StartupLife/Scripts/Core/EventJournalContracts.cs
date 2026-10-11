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

        public EventTicketRecord Copy() => new EventTicketRecord
        {
            Id = Id, EventId = EventId, CategoryId = CategoryId, OpenedDateIso = OpenedDateIso,
            ChoiceId = ChoiceId, ResolutionCommandId = ResolutionCommandId, OutcomeId = OutcomeId,
            OriginDateIso = OriginDateIso, Stage = Stage, Tags = new List<string>(Tags)
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

        public EventJournalState Copy() => new EventJournalState
        {
            RunId = RunId, Revision = Revision, NextTicketSequence = NextTicketSequence,
            EventRng = EventRng, InitialEventRng = InitialEventRng, PendingTicketId = PendingTicketId,
            Tickets = Tickets.ConvertAll(ticket => ticket.Copy())
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

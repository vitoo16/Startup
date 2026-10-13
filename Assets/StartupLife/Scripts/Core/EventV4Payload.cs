#nullable enable
using System.Runtime.Serialization;

namespace StartupLife.Core
{
    // Prospective schema 4: the authoritative historical v3 checkpoint is embedded
    // verbatim. Its existing archived v2 prefix is never modified by event migration.
    [DataContract]
    public sealed class EventV4Payload
    {
        [DataMember(Order = 0)] public int WireSchema { get; set; } = 4;
        [DataMember(Order = 1)] public string EmbeddedV3EnvelopeBase64 { get; set; } = "";
        [DataMember(Order = 2)] public string EmbeddedV3EnvelopeSha256 { get; set; } = "";
        [DataMember(Order = 3)] public EventJournalState? Journal { get; set; }
    }
}

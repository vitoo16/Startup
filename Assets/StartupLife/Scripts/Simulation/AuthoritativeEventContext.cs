#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Single deterministic read-only projection of event eligibility from the
    // receipt-verified V3 core; never accepts stage/tags from UI/authoring requests.
    // This is event-eligibility maturity, NOT the final financial 6-stage business model.
    public static class AuthoritativeEventContext
    {
        public static GameEventContext FromCore(GameState core, EventJournalState journal)
        {
            if (core == null || journal == null || journal.OriginDateIso.Length == 0 ||
                core.RunId != journal.RunId)
                throw new ArgumentException("Receipt-verified state and anchored journal required.");
            var origin = DateTime.ParseExact(journal.OriginDateIso, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None);
            var first = new SimDate(origin.Year, origin.Month, origin.Day);
            var tags = new HashSet<string>(StringComparer.Ordinal);
            var stage = 0;
            if (!string.IsNullOrEmpty(core.Name))
            {
                stage = 1;
                tags.Add("character-created");
                if (core.Employment == null) tags.Add("unemployed");
                else
                {
                    tags.Add("employed");
                    tags.Add("career/" + core.Employment.CareerId);
                }

                if (core.Course != null && !core.Course.Completed)
                {
                    tags.Add("course-active");
                    tags.Add("course/" + core.Course.DefinitionId);
                }

                var active = core.Businesses.Where(x => x.IsActive).ToArray();
                if (active.Length > 0)
                {
                    stage = 2;
                    tags.Add("business-active");
                    foreach (var business in active)
                        tags.Add("business/" + business.DefinitionId);
                }
            }
            return new GameEventContext(first, core.Date, stage, tags,
                Array.Empty<EventOccurrence>());
        }
    }
}

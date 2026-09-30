# ADR-004 — Calendar time and activity boundaries

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: The same career work must earn the same result at any visual speed. Employment, study, founder time, and interruption share one calendar.

Decision: Use Gregorian dates, integer minutes, and half-open intervals. AdvanceDay targets next midnight; AdvanceMonth targets next calendar-month start. Advance by committed activity boundaries and pause for choices. Employed shifts derive from definitions, including weekends; founder/unemployed schedules release work reservation. New employment starts at the next full eligible shift. Resignation takes effect at the next boundary. Completed scene XP persists; incomplete workdays do not earn completed-shift salary.

Consequence: Work fast-forward is playback of authoritative outcomes. Tenure uses calendar service anniversaries, independently of completed work. Suspension saves boundary and cursor; wall time creates no offline advancement. At midnight settle ending activities/business, then new-date payroll, living obligations, tenure milestones, and choices in fixed order.

Verification required: leap years, Jan-31 anniversaries, shift/weekend variants, work interruption, founder availability, choice stops, and identical resume after arbitrary real-world absence. See [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). No gameplay execution claimed.

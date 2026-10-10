# Pre-fix v3 compatibility fixture

`pre-fix-v3-pending-business.json` was produced by the real `GameSession` and
`AtomicFileSaveStore` on source baseline `7c3c581557040fc38153a83da7b8d036323b24dc`,
before the receipt-finalization correction. It contains character creation,
online-store launch, next-day premium pricing, pause, and pending next-day resume.
Its business economics and receipt history already passed strict v3 replay.

SHA-256: `0f4354339c7465671a55ecb80dbb56175aedab323ccc938b338fd915dbfe65c7`.

The regression loads these immutable bytes with the production v3 serializer,
validates the pending policy state, and requires byte-identical serialization.
Do not regenerate this fixture from the corrected implementation: it proves
compatibility with previously valid saves, not a new-save-only round trip.

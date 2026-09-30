# ADR-005 — Whole-VND money, exact salary accrual, and arrears

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Calendar-dependent salary rounding and resignation must not duplicate or delete earned pay. Losses must allow recovery.

Decision: Money is checked signed Int64 whole VND; multipliers are fixed-point. Each completed shift earns monthly salary divided by scheduled workdays in that whole calendar month under the applicable rate/schedule revision. Preserve exact fractional-VND claims; payday aggregates due claims, pays whole VND, and carries the remainder. Rate changes affect future work only. Payday occurs at the start of its clamped date and excludes work completed later that date. Resignation preserves all earned claims.

Cash is nonnegative. Obligations spend available cash and create arrears for the balance. Income settles oldest arrears before adding spendable cash. No loans/interest; courses and investment/reinvestment reject during arrears. Ledger entries distinguish accrual from payment and preserve attribution.

Consequence: Exact rational intermediates may use BigInteger; serialize validated fractional numerator/denominator strings rather than relying on serializer-specific BigInteger reflection. Whole-VND conversion remains checked. This precision choice requires IL2CPP validation. Overflow rejects the whole transaction.

Verification required: 22-day and leap-month full salary, partial employment, payday replay, promotion, resignation, mixed-denominator carry, oldest-arrears settlement, and ledger conservation. Worked numbers are in [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md); runtime tests remain pending.

# 09 — Per-currency minor units

**What to build:** Stripe charges in minor units, but not every currency divides into 100. The checkout service converts amounts per-currency (zero-decimal currencies like JPY multiply by 1, standard currencies like PLN by 100) and fails explicitly on unsupported currencies instead of silently producing a wrong amount.

**Blocked by:** 08, 05, 01 — shares the Stripe payment service and the payments controller switch with 08/05; needs `UnsupportedCurrencyError` from 01.

**Status:** ready-for-agent

- [ ] The service keeps a known currency→minor-unit-divisor lookup covering Stripe's currency list (zero-decimal → 1, otherwise → 100).
- [ ] Unknown currency returns a failed `UnsupportedCurrencyError` result.
- [ ] `UnsupportedCurrencyError` maps to 400 in the payments controller error switch.
- [ ] `SessionService` becomes constructor-injectable (defaulting to a new instance) so the service is directly testable.
- [ ] Service tests cover: currency mismatch → `CurrencyMismatchError`, unknown currency → `UnsupportedCurrencyError`, known zero-decimal (JPY) and 2-decimal (PLN) → correct `UnitAmount`.
- [ ] `dotnet build` and all tests pass.

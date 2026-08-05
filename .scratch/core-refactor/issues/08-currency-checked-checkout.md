# 08 — Currency-checked checkout (Result pattern)

**What to build:** The checkout flow refuses to create a Stripe session when the ticket price currency does not match the configured app currency, surfacing a clear currency-mismatch error instead of charging in the wrong currency.

**Blocked by:** 07 — shares the checkout handler and its test; keep the Payment `ExternalId`/provider merge in first.

**Status:** ready-for-agent

- [ ] `IPaymentsService.CreateCheckoutSessionAsync` returns `Result<CreateCheckoutSessionResult>`.
- [ ] The Stripe implementation returns a failed `CurrencyMismatchError` result when the Money currency differs from the configured app currency, before any Stripe call.
- [ ] The checkout handler propagates the service failure into its own failed result.
- [ ] Handler test covers the mismatch-failure propagation path.
- [ ] `dotnet build` and all tests pass.

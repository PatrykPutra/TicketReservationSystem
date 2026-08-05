# 07 — Provider-agnostic Payment entity

**What to build:** The Payment entity no longer knows about Stripe sessions. It stores the payment provider as an enum and the provider's external reference as a generic id, so it is ready for future providers.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] `StripeSessionId` property and `SetStripeSessionId` method are removed.
- [ ] New `PaymentProvider` enum (single `Stripe` member) is added to the domain; Payment takes it as a constructor parameter.
- [ ] New `ExternalId` string property with a `SetExternalId(string)` method (preserving the existing `ModifiedAt` behavior) records the provider's session/reference id.
- [ ] EF mapping updated: ExternalId stored (max length 200), PaymentProvider persisted as a string.
- [ ] Checkout flow creates the Payment with the provider and persists the external id; entity tests updated to `SetExternalId`.
- [ ] `dotnet build` and all tests pass.

# 02 — Checkout session end-to-end

**What to build:** A user-facing endpoint that starts a Stripe-hosted checkout for a ticket the caller has reserved. The endpoint cross-checks the supplied user id against the authenticated identity, requires the ticket to be reserved by that user, blocks starting a second checkout while an active pending payment exists, creates a `Pending` payment, asks the Stripe integration to open a Checkout Session (with the payment id embedded for later matching, success/cancel URLs and currency from configuration, minor-unit conversion handled by the integration), persists the session, and returns the hosted checkout URL, session id, and payment id. The webhook must be able to match this session back to the payment.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `POST` checkout endpoint requiring authentication; body-supplied user id cross-checked against the JWT identity (mismatch fails with an unauthorized result).
- [ ] Precondition: the ticket exists and is reserved by the calling user, and no active pending payment already exists for it.
- [ ] Stripe integration abstraction (interface + implementation + settings) backed by the official Stripe SDK; creates a Checkout Session with `client_reference_id` set to the payment id, success/cancel URLs from configuration, and the amount converted to minor units.
- [ ] Creates and persists a `Pending` payment, stores the returned session id, and returns `{ CheckoutUrl, SessionId, PaymentId }`.
- [ ] Follows the existing command/result dispatch pattern from the controller, mapping error results to HTTP responses consistently with existing endpoints.
# 10 — Dedicated webhooks endpoint + honest failures

**What to build:** Stripe webhook events arrive at a dedicated `webhooks/stripe` endpoint, and the handler reports structural failures honestly so Stripe can retry, while normal idempotent redelivery and unrelated event types still succeed silently.

**Blocked by:** 09, 01 — shares the payments controller (old webhook route + error switch) with 09; needs `PaymentProcessingError` from 01.

**Status:** ready-for-agent

- [ ] New anonymous webhooks controller exposes `POST webhooks/stripe` with Stripe signature verification.
- [ ] The webhook handler returns a failed `PaymentProcessingError` result when: the event object is not a Stripe Session, the `ClientReferenceId` isn't a parseable payment id, or no matching payment exists.
- [ ] Idempotent no-ops (payment not pending) and unknown event types still return success.
- [ ] The controller returns 500 on handler failure (so Stripe retries) and 200 on success.
- [ ] The old `api/payments/webhook` endpoint is removed from the payments controller (along with its now-unused Stripe imports).
- [ ] Handler tests cover the three failure paths plus the unchanged success paths.
- [ ] `dotnet build` and all tests pass.

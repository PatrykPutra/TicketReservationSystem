# 03 — Webhook + completion

**What to build:** The Stripe webhook endpoint that receives payment events and, on completion, verifies the ticket and moves the payment to `Completed`. The endpoint accepts Stripe's signed webhook calls (no user authentication), verifies the signature against a shared secret (returning a bad-request result on verification failure), and hands the event to a command handler as a normal part of the command/result dispatch flow. On the completion event, the handler marks the payment `Completed` and confirms the reserved ticket owned by the payment's user. Re-delivered or already-handled events and unrecognised event types are no-ops so confirmations stay idempotent.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] Webhook endpoint that reads the raw request body and the Stripe signature header and verifies the signature against the configured secret, returning a bad result on failure.
- [ ] The event is dispatched as a command through the existing command/result pipeline, decoupled from the HTTP response body.
- [ ] On the payment-completion event: mark the payment `Completed` and confirm the ticket owned by the payment's user.
- [ ] Re-delivered/duplicate and unrecognised event types are harmless no-ops; already-completed payments are not double-confirmed.
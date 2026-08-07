# 04 — WebhooksController tests

**What to build:** Tests covering the StripeWebhook action, including signature
verification, so forged deliveries are rejected and valid ones map correctly.

**Blocked by:** None — can start immediately

**Status:** done

- [x] StripeWebhook returns 400 when the Stripe-Signature header is invalid or missing
- [x] StripeWebhook returns 200 for a validly signed payload when the dispatched command succeeds (signature computed as a real HMAC-SHA256 header: t=<unix-seconds>,v1=<hex> over the payload)
- [x] StripeWebhook returns 500 when the dispatched command fails
- [x] Test names follow the MethodName_Scenario_ExpectedResult convention

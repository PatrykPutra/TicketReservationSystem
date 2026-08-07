# 02 — AuthenticationController tests

**What to build:** Tests covering the SendCode and Token actions so the controller maps
each dispatcher outcome to the correct HTTP status.

**Blocked by:** None — can start immediately

**Status:** done

- [x] SendCode returns 200 for a successful dispatch
- [x] SendCode returns 404 when the dispatcher reports a user-not-found error
- [x] SendCode returns 429 when the dispatcher reports a rate-limited error
- [x] SendCode returns 500 for any other dispatch failure
- [x] Token returns 200 with the issued token and expiry in the body for valid credentials
- [x] Token returns 401 for invalid credentials
- [x] Test names follow the MethodName_Scenario_ExpectedResult convention

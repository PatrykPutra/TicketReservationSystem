# 06 — UserController.AddUser tests

**What to build:** Tests covering the AddUser action so registration maps each command
outcome to the correct HTTP status.

**Blocked by:** None — can start immediately

**Status:** done

- [x] AddUser returns 200 with the created user id for a successful dispatch
- [x] AddUser returns 409 when the email already exists
- [x] AddUser returns 500 for any other dispatch failure
- [x] Test names follow the MethodName_Scenario_ExpectedResult convention

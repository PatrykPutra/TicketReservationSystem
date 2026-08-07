# 01 — ExceptionHandlingMiddleware behavioral tests

**What to build:** Behavioral tests for the exception-handling middleware so that
unhandled exceptions consistently produce a 500 problem+json response and healthy
requests pass through untouched.

**Blocked by:** None — can start immediately

**Status:** done

- [x] Request passes through untouched (status unchanged, next delegate invoked) when no exception is thrown
- [x] A thrown downstream exception sets response status 500 and content type application/problem+json
- [x] The response body is a ProblemDetails JSON containing the title "An unexpected error occurred"
- [x] The exception is logged via the middleware logger
- [x] Test names follow the MethodName_Scenario_ExpectedResult convention

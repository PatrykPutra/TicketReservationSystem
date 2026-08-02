# 02 — Unify result pattern in Authentication module

**What to build:** Refactor AuthenticationResult, GenerateTokenResult, SendAuthenticationCodeResult to inherit Result<TPayload>. Replace custom IsSuccess flags and string ErrorCode with the Error hierarchy. Refactor AuthenticationController to check esult.IsFailure once and switch on esult.Error.

**Blocked by:** 01 — Enable Result subclassing + add Error types + DTO records

**Status:** ready-for-agent

- [ ] AuthenticationResult : Result<AuthenticationResponse> — success ctor takes AuthenticationResponse, error ctor takes Error
- [ ] GenerateTokenResult : Result<TokenResponse> — replace InvalidCredentials() factory with 
ew GenerateTokenResult(new InvalidCredentialsError(...))
- [ ] SendAuthenticationCodeResult : Result<SendAuthenticationCodeResponse> — replace UserNotFound()/RateLimited() factories with direct error ctor calls
- [ ] Refactor AuthenticationHandler — return 
ew AuthenticationResult(new UserNotFoundError(...)) instead of 
ew AuthenticationResult(false, null)
- [ ] Refactor GenerateTokenHandler — return Result.Failure instead of factory methods
- [ ] Refactor SendAuthenticationCodeHandler — return Result.Failure instead of factory methods
- [ ] Refactor AuthenticationController — replace IsAuthenticated/IsSuccess/ErrorCode checks with single IsFailure + error-type switch

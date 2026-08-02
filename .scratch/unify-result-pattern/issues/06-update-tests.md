# 06 — Update AuthenticationHandlerTests

**What to build:** Update existing test assertions to match the new result pattern. Replace esult.ErrorCode string assertions with Assert.IsType<XxxError>(result.Error). Replace esult.Token with esult.Value.Token. Remove references to removed factory methods.

**Blocked by:** 02 — Unify result pattern in Authentication module

**Status:** ready-for-agent

- [ ] SendAuthenticationCodeHandler_user_exists_saves_code_and_returns_success — update if assertion on esult.IsSuccess or payload changed
- [ ] SendAuthenticationCodeHandler_user_not_found_returns_user_not_found — replace esult.ErrorCode with esult.Error type assertion
- [ ] SendAuthenticationCodeHandler_rate_limited_returns_rate_limited — replace esult.ErrorCode with esult.Error type assertion
- [ ] GenerateTokenHandler_valid_code_returns_token_and_marks_code_used — replace esult.Token/esult.ExpiresAt with esult.Value.Token/esult.Value.ExpiresAt
- [ ] GenerateTokenHandler_invalid_code_returns_invalid_credentials — replace esult.IsSuccess + esult.Token with esult.IsFailure + esult.Error type
- [ ] GenerateTokenHandler_used_code_returns_invalid_credentials — same assertion pattern update
- [ ] GenerateTokenHandler_expired_code_returns_invalid_credentials — same assertion pattern update

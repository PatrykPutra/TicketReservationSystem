# 01 — Foundation

**What to build:** Add NuGet packages (MailKit, MimeKit, JwtBearer, IdentityModel.Tokens.Jwt). Create `EmailVerificationCodeId` value object, `EmailVerificationCode` entity, `IEmailVerificationCodeRepository` interface, and `AuthenticationCodeGeneratedEvent` domain event. Configure the entity in `ApplicationDbContext` and register the repository in DI.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] NuGet packages added to csproj
- [ ] `EmailVerificationCodeId` value object created
- [ ] `EmailVerificationCode` entity created with all fields
- [ ] `AuthenticationCodeGeneratedEvent` domain event created
- [ ] `IEmailVerificationCodeRepository` interface created
- [ ] Entity configured in `ApplicationDbContext`
- [ ] Repository + DI registered

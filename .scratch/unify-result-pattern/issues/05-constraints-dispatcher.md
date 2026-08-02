# 05 — Tighten interface constraints + simplify dispatcher

**What to build:** Change ICommand<TResponse>, ICommandHandler<TCommand, TResponse>, and ICommandDispatcher constraints from 
otnull to Result. Simplify CommandDispatcher to a pure pass-through that calls mediator.Send() directly with no try/catch.

**Blocked by:** 02 — Unify result pattern in Authentication module, 03 — Unify result pattern in Tickets module, 04 — Unify result pattern in Users module

**Status:** ready-for-agent

- [ ] ICommand<out TResponse> — change where TResponse : notnull to where TResponse : Result
- [ ] ICommandHandler<in TCommand, TResponse> — same constraint change
- [ ] ICommandDispatcher — change to Task<TResponse> DispatchAsync<TCommand, TResponse>() where TResponse : Result
- [ ] CommandDispatcher — remove try/catch, call mediator.Send(command, token) directly
- [ ] Verify build passes with all new constraints

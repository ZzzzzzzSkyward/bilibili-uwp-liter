# QF1 API01 compatibility-wrapper ownership amendment

Status: **READY FOR INDEPENDENT REVIEW**. This is a narrowly scoped correction to the requirements/dispatch, not a new product change or a test-pass claim.

The executor's [ownership-conflict.md](ownership-conflict.md) provides compiler evidence that the earlier namespace-only repair instruction was mistaken. Direct source inspection confirms `Api/ApiHelper.cs:9–23` declares the real global `ICookieService` and **global `CookieService : BiliLite.Services.CookieService` compatibility wrapper**. `ApiHelper.Cookie` at line 28 is explicitly that derived global type. Thus the two original `new global::CookieService(...)` constructions in API01 are valid actual-product wrappers, not nonexistent/fake globals. Changing them to the namespaced base class causes CS0266 when assigning ApiHelper.Cookie.

## Replacement contract and exclusive ownership

- QF1 executor retains its existing `plan/tests/API01.cs` ownership, but **must preserve/revert to the two original real global-wrapper constructions at baseline lines 147 and 232**. Do not introduce a new fake compatibility service or edit ApiHelper/product types.
- The stronger reviewed same-owner replacement-cookie/phase/CSRF/token fixtures remain in scope for the three already named refresh tests. Where the production wrapper is assigned to ApiHelper.Cookie, use the wrapper's actual supported constructor; use an isolated fixture LocalFolder-backed state store when its constructor does not accept the pure service's extra store argument.
- Other fixture-only refresh cases may instantiate the real namespaced base service directly when not assigning it to the global-wrapper field. That does not grant product/transport changes or blanket expected-value updates.
- Preserve every unaffected assertion and the prior requirements' per-intention mapping, rollback/provenance/WBI/session/cancellation requirements and regression gates. A current empty-store fixture is not declared invalid solely from constructor inspection; the stronger replacement-jar coverage is deliberate added protection.
- All other QF1 ownership, QF2 plan-only scope, comments/c1/season1 exclusion, Windows limits and commit-window coordination remain unchanged.

## Acceptance

Independent reviewer reads actual header/field/type/constructor, the precise API01 assignments and compile-error evidence; confirms the amended construction binds to actual product code, retains intended fixture phases and introduces no fake success seam. After PASS, the orchestrator updates controlling requirements/dispatch and quality executor re-runs independent API01, auth1/api02 and other previously assigned credential gates. Actual test results belong in execution evidence, not this amendment review.

Review output: `verification/amendment-review.md`. No product/test/main-doc write or commit permission for the amendment reviewer.

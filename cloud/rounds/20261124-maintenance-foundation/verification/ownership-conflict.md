# QF1 implementation finding requiring corrected dispatch

Status: OPEN — exact API01 namespace correction cannot compile against actual source.

`src/BiliLite.UWP/Api/ApiHelper.cs:9–28` actually declares the real global `ICookieService` and `CookieService : BiliLite.Services.CookieService`; `ApiHelper.Cookie` is specifically `global::CookieService`. Both originally assigned API01 references to `new global::CookieService(...)` therefore use the real production compatibility wrapper and are correct. Changing them to `new BiliLite.Services.CookieService(...)` produces CS0266 at API01 lines 161 and 232: base service cannot assign to derived global wrapper. No fake global service is present in the harness.

Command after restoring pure dependencies and applying the dispatched type-only correction: `bash plan/tests/run-api01.sh` (scratch snapshot, SDK 8.0.427), exit 1; both assignments report CS0266. Raw log: `/workspace/work/maintenance-foundation/quality-execution/logs/compile-run-api01.sh.log`.

Requested narrowly corrected ownership: retain the two original `global::CookieService` references, instantiate the actual production compatibility wrapper at those assignment sites, preserve the real ApiHelper source and all unrelated assertions. For stronger fixture-only phase/state testing, exercise the real namespaced service directly in the other two named refresh cases; the wrapper fixture uses its real currently-supported constructor plus a case-isolated LocalFolder store. No product or assertion weakening is proposed.

Other assigned harness work continues independently. Independent requirements amendment/review and revised dispatch needed before final API01 disposition.

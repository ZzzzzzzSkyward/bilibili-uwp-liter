# Search request evidence — completed requirements baseline

Status: REQUIREMENTS READY FOR INDEPENDENT REVIEW. The priority quality revision is finished; search preparation resumed and is complete. Search product/tests are not implemented or approved. Source: `dev`, `41dc562fba6e45896b0c4ba604f9ec38f4661e73`.

The next-wave plan and required preceding requirements/baseline were read; actual `SearchAPI.cs`, `ApiHelper.cs`, `WbiKeyService.cs`, `HttpHelper.cs`, `Utils.cs`, SearchVM callers and existing API/auth probe inputs were inspected. Scratch is `/workspace/work/maintenance-foundation/search/`; no product/test/doc/build file was changed.

A scratch actual-source reproducer now exists at `probe/SearchRequestProbe.csproj`, with preparation script `prepare-probe.py` and raw logs `logs/repro-expanded.txt`. It links the unchanged real SearchAPI, ApiHelper, WbiKeyService, CookieService and current refresh dependencies. It extracts the exact `Utils.ChooseProxyServer` and `HttpHelper.GetWithWebCookie` method boundaries into wrappers; substitutes only platform cookie/storage/transport/settings primitives. The network terminates at a capturing stub; no service request, real identity or account action occurs. Fake cookie values exist only as local test fixtures. Source SHA-256 inputs are captured in `logs/source-inputs.json`; end-of-run recheck found zero mismatches.

Command with the preceding baseline's scratch SDK/cache environment:

```bash
dotnet run --project /workspace/work/maintenance-foundation/search/probe/SearchRequestProbe.csproj -c Debug
```

Result: exit **1**, `PASSED=523 FAILED=62` (585 checks). These are red requirements checks, not implemented fixes:

- 42 failures: each of seven web builders overwrites identity in three no-area cases (default API empty area, default API null area, configured API empty area), checked both through metadata and through the exact extracted cookie merge.
- 7 failures: no saved BUVID is replaced with `temp` by each direct builder.
- 7 failures: an explicit Cookie-header identity is overwritten by each direct builder.
- 6 failures: nonempty app orders `view`, `pubdate`, and an encoded punctuation/Unicode fixture are omitted, in guest and logged-in cases.

Controls pass: every builder's selected route/type/page/keyword, existing type-specific filters, explicit cn/hk/tw/unknown-area proxy selection and `temp` compatibility, real WBI signatures over all returned fields, known independent WBI vector, WBI fetch-error propagation, unrelated cookie preservation/input dictionary immutability, app route/auth/timestamp/optional filters and signature consistency. Real WBI/app signing bodies are compiled; only crypto/time primitives already used by the existing API01 stubs are substitutes. WBI uses actual service time except the independent fixed vector.

Source diagnosis: seven unconditional assignments at `Api/SearchAPI.cs:63,85,107,129,151,173,195`; extras override saved/header cookies in `Helpers/HttpHelper.cs:216–220`. App `SearchAPI.Search` appends order only when null/empty (`:29–31`), before signing (`:41`). Existing SearchVM uses web builders; no active direct `.Search(...)` caller of the app builder was found in the targeted source search. No current UI-order or live-server failure is claimed.

Final contracts and proposed ownership are in [requirements.md](requirements.md): explicit nonempty area remains the compatibility discriminator; no-area configured API retains identity; unknown-area fallback remains; existing user-order/type fragments stay fragments; app order is escaped once before signing, including explicit empty defaults. Existing api02 owns new real-source checks, subject to release of shared API01Stubs ownership from QF1. No API/helper/transport redesign or BUVID acquisition is authorized. Current HEAD `3317305f5c606cd4ca33f21a611c23f15a18dada` is a metadata-only checkpoint; source-input hash recheck continues to match the original baseline.

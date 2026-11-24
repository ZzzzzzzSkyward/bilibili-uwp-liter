# Search execution — COMMITTED / PUSHED / OWNERSHIP RELEASED

Owner: `search_executor`. U5/U8 implementation and owned verification are complete. `search/commit-window.md` granted the shared-index commit/push window after U9/E1 release. Source/test commit `f594c9b58f6ed2e086329b1d2a221b1bc0fdf95e` is pushed and verified on origin/dev. The exact verified file hashes are unchanged. Source/test and shared API01Stubs ownership are now released. This is not an implementation-review or Windows/session PASS.

## Inputs and ownership

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, `cloud/memory/INDEX.md`, round plan/dispatch and search requirements/review/evidence. Dispatch explicitly releases prior QF1's unchanged shared stub and grants one current writer. Started after `git pull --ff-only` reported Already up to date; unrelated active orchestration artifacts preserved. Verification HEAD is `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c` (another owner's favorite slice advanced the shared checkout during disjoint work); the original search source matches the unchanged requirements baseline.

Only assigned files changed:

- `src/BiliLite.UWP/Api/SearchAPI.cs`: move each of seven request-local `buvid3=temp` assignments into its existing nonempty-area branch; always include one escaped app order before signing (`null` becomes the existing empty default).
- `plan/tests/api02/API02.csproj`: compile the actual SearchAPI.
- `plan/tests/api02/Program.cs`: retain all original assertions and invoke additional search checks after them.
- `plan/tests/api02/SearchRequestChecks.cs`: new actual-builder matrix and real signer checks, grouped U5/U8/control failures.
- `plan/tests/API01Stubs.cs`: primitive proxy-selector callback and input capture only. It does not implement mode selection, cookies, request building or signing.
- This `search/execution.md` artifact.

No other source, test, transport, helper, main docs, package/build configuration or root files were edited by this executor. No live request/account operation, synthetic credential transmission, manifest bump, release or master operation.

## Verification and genuine before/after

SDK **8.0.427**, runtime/ref pack **8.0.33**, isolated CLI/cache environment in `/workspace/work/maintenance-foundation/search-execution/env.sh`. Every command ran from the repository root unless an absolute project path is shown. Raw logs remain scratch-only under `/workspace/work/maintenance-foundation/search-execution/logs/`.

| Command / evidence | Original real SearchAPI | Repaired real SearchAPI |
| --- | --- | --- |
| `bash plan/tests/api02/run.sh` (`api02-red.txt`, `api02-green.txt`) | Exit **134**: original **12/12** assertions PASS; search **1,222 PASS / 36 FAIL** = **28 U5 / 8 U8 / 0 controls** | Exit **0**: original **12/12** PASS; search **1,258 PASS / 0 FAIL**, U5/U8/control failures all zero |
| `dotnet run --project /workspace/work/maintenance-foundation/search-execution/probe/SearchRequestProbe.csproj -c Debug` (`extracted-red-expanded.txt`, `extracted-green-expanded.txt`) | Exit **1**: **805 PASS / 76 FAIL** = 56 no-area metadata/actual-merge identity failures + 7 missing-identity + 7 explicit-header + 6 app-order failures | Exit **0**: **881 PASS / 0 FAIL** with the identical expanded harness and extracted methods; only the linked SearchAPI input changed |
| `bash plan/tests/auth1/run.sh` (`auth1.txt`) | Existing foundation release is baseline | Exit **0**, **35 assertions PASS** after shared-stub edit |
| `bash plan/tests/run-api01.sh` (`api01.txt`) | Existing foundation release is baseline | Exit **0**, **76 actual-source assertions PASS** after shared-stub edit |
| `git diff --check` | — | Exit **0** |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` (`windows-build.txt`) | — | Exit **127**, `/bin/bash: powershell: command not found` |

Permanent assertions were added and run before product repair; they were **unchanged** for green. The red process first prints the original 12 PASS; its nonzero exit is solely the 36 classified search assertion failures. No signature/control failure is hidden by an expected aggregate red. The initial scratch extraction run was 658/76, then the harness was expanded to assert saved/extras immutability and direct/area missing/header identity; the final expanded **805/76 → 881/0** pair links saved original SearchAPI bytes versus actual repaired source. An initial scratch csproj duplicate-Compile setup error was fixed before these behavioral runs; no assertion was removed or weakened.

Permanent coverage: seven web builders × default/configured API bases × null/empty/cn/hk/tw/unknown/whitespace areas (98 signed requests), exact selector call counts/inputs, cookie flag and case-insensitive no-area BUVID absence, explicit-area single temp compatibility, host-independent mode (including official-host selected proxy), routes/types/page, common and type-specific filters, user query fragments, punctuation/Unicode keyword and existing WBI sanitizer, exactly one delimiter/area/WBI field, known independent WBI vector and real signatures over every final field. WBI fetch failure propagates before seeding a cache. Guest/logged-in app checks cover null/empty/view/pubdate/punctuation+Unicode/unknown orders, injection boundaries, exact decoded inputs, optional/default filters, flags, auth/timestamps and real signatures.

## Actual-source extraction, inputs and limits

The scratch `prepare-probe.py` links actual SearchAPI, ApiHelper, WbiKeyService, CookieService and current credential dependencies. It copies **complete exact** `Utils.ChooseProxyServer` and `HttpHelper.GetWithWebCookie` method text into wrappers, verified again by exact text containment. The final source manifest is `logs/final-inputs.json`; red/green input manifests are `logs/source-inputs-red.json` and `logs/source-inputs-green.json`.

Key SHA-256 inputs:

| Input | SHA-256 |
| --- | --- |
| Original SearchAPI (saved `SearchAPI.old.cs`) | `44eff327d6bc9323f9bc82ec1715636cc29efa9472c0349f1de7734e69c7ebe0` |
| Repaired SearchAPI | `9f9b00550b5575be6a137c7cf7f696fe66fa497c68cceed8a51a8da3c8d7735b` |
| Permanent SearchRequestChecks | `fe366983030cb255dca05b9dfdf61c029dd687d44260a56118e16ab4a2864584` |
| Shared API01Stubs | `034c9d0362e8866b39f252bf0c0420901963b1a31b8d115eba0189d620ca568d` |
| Actual ApiHelper | `dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9` |
| Actual WbiKeyService | `2420c42dbffcb6eec8293fbd0580ddb76ab80a51ebf1a60ef58d57e819ebb32d` |
| Utils full source | `55c71e2fd7df5ed1046f994ae38b22ddb25c12190d3f7d07e02eb0684ba295e3` |
| Exact ChooseProxyServer method | `7ca241fdeaf861bed7d986b7168c9b6f67e0ac21521930c88a30cca28800cbf5` |
| HttpHelper full source | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| Exact GetWithWebCookie method | `2e9bbd3d86ebaff0c6c2599856ad9bac10f33efa295673a1cad50550dd1dcccd` |
| Exact-method primitive proxy wrapper | `ca8649a8c657ff3a43485caab27599534a35dfa1d10eb885de7f6ab9c29540fe` |
| Exact-method primitive transport wrapper | `9018e969cdc3985295091afe973b8b813560830c1c6c669aa63d59c9a63698ce` |
| Identical expanded scratch Program | `62a0725812d2b74a64cfec37635b3ddb94238863b6bcd3135d1862f980a5adfe` |

Scratch exact proxy tests include all regions and empty regional-server fallback; exact transport tests retain saved identity on every no-area/default/configured/null path, omit fabricated identity from a nonempty jar missing BUVID, preserve explicit Cookie-header identity and unrelated cookies/dictionaries, retain explicit-area temp behavior with saved/missing/header identity, and leave saved BUVID and extras untouched. Only settings/storage/WinRT cookie collection/network capture/time/MD5 primitives are substituted; real merge/proxy methods and real app/WBI signing bodies execute. No manually rewritten merge or signer is used.

Mutable public base/login/key/hook/capture settings are restored in `finally`; permanent WBI failure is isolated before first seeding in this console process. The real ApiHelper's private WBI cache lives only until the process exits and is not a simulated production session. The shared stubs still compile original auth/API controls. Scratch cookies lack actual WinRT domain/path/store/network semantics; a missing BUVID fixture intentionally has another saved cookie, so it does not redefine the existing completely empty-jar bootstrap policy.

## Remaining gates and handoff

- **COMMITTED / PUSHED**: only the five assigned source/test files and this evidence artifact were staged. Conventional commit: `f594c9b58f6ed2e086329b1d2a221b1bc0fdf95e` — `fix: preserve search identity and signed app ordering`. `git push origin dev` exited 0; `git ls-remote origin refs/heads/dev` returned exactly that SHA immediately afterward.
- **OWNERSHIP RELEASED**: SearchAPI, api02 owned files, and shared API01Stubs are released for read-only implementation review and later explicitly assigned work. The serialized index/commit window is released after publication of this evidence follow-up. Other owners' source/build/orchestration files were preserved. `git pull --ff-only` before staging reported Already up to date; all seven verified source/test input hashes still matched, so no retest was needed or run.
- **Windows build FAILED/BLOCKED** by missing PowerShell/VS UWP/Windows SDK in this Linux environment. Actual WinRT cookie installation/wire behavior, real Bilibili sessions, configured proxy connectivity, rendered search UI/device acceptance are **BLOCKED/unverified**. Offline green is not live/session/device/release readiness.
- Independent implementation review and the later one-owner main-document disposition remain outside this executor's current scope.

# U9 favorite-folder implementation review

Status: **PASS** — immutable source/test commit `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c` satisfies the reviewed U9 contract. **No P0/P1/P2 implementation finding.** This accepts the local request construction and offline regressions; Windows/session/service persistence gates remain BLOCKED or unverified.

Read applicable repository/cloud instructions, memory INDEX, this round's implementation/execution dispatches, favorites requirements/review/evidence/execution and the execution index. Reviewed the immutable commit and parent rather than the concurrently edited build/runner work. This reviewer owns only this file; no product, test, main-documentation or history change, commit/push or live request was performed.

## Source and contract assessment

The complete product diff adds exactly one `&` before `privacy` in each of `FavoriteAPI.cs:122,141`. A byte comparison confirms the committed product file is exactly the parent file with those **two separator insertions**, with no other product change. The previously contaminated timestamp and missing privacy field are corrected before the existing signing call.

- Create/edit remain POSTs to the configured `API_BASE_URL` plus the existing add/edit routes. Feature fields remain in the body and in the same order; query/cookie metadata defaults are unchanged. Actual `ApiModel` defaults agree with the probe's limited substitute.
- Both common-parameter and signing calls retain **AndroidKey**, and common parameters retain `needAccesskey=true`. The final complete encoded unsigned body is supplied to the existing signer once, then its suffix is appended once. The shared ApiHelper is byte-identical to the parent and is outside this diff.
- `isOpen=true → privacy=0`, false → 1 is preserved. Inspected the bound create/edit dialog callers and XAML: raw textbox values and the **公开收藏夹** checkbox still feed these arguments unchanged. No checkbox inversion or dialog change occurred.
- Existing `Uri.EscapeDataString` calls remain exactly where they were. Raw Unicode/emoji, whitespace, reserved delimiters, literal percent text and newline inputs retain one-escape semantics; empty title/intro remain explicit values. Edit `media_id` remains the supplied string, retaining high decimal IDs and leading zeroes; create adds no ID.
- No helper/credential/transport, other favorite operation, endpoint, dependency, manifest, asset, build or main-documentation change is included. The commit contains only FavoriteAPI, the three owned c2 files and stream execution evidence.

## Test fidelity and preservation

`c2/C2Probe.csproj:19` links the **complete actual FavoriteAPI.cs**. `Program.cs:137–185` tests 16 cases: create/edit × public/private × guest/logged-in × reserved-text/empty-text. The recording common helper ends at decimal `ts=1700000000` without a trailing separator and cannot repair the defect. The signing recorder observes input/key/call count and returns only a deterministic suffix; this is caller-order verification, not a claim to test real cryptography.

Multiplicity is preserved by parsing form pairs into a list. Exact key counts/sets and individual value multiplicity expose duplicate/missing fields and separator injection. One decode must recover the original text; an additional raw encoded-suffix assertion pins escaping and feature-field order before signing, including edit ID. The fixture host includes a nondefault `/api` prefix, and both alternate keys are distinct objects, exposing host/key substitution. Per-case recorder reset preserves call-count independence. No new test sends a request; existing MediaList fake transport queues are untouched.

An independent byte comparison removed only the new method, its invocation and two imports from the committed Program and obtained the **exact parent Program bytes**. The original C2Stubs content is an unchanged prefix; the project only adds the source link. Thus all original **162** parser/service/retry/paging/cursor/single-flight/source-wiring assertions remain, with **289** added checks for **451 total**. No assertion was weakened or deleted.

## Independent immutable-source verification

Reviewed executor logs `u9-red.log` and `u9-green.log`, then freshly rebuilt/reran isolated snapshots exported from Git under:

`/workspace/work/maintenance-foundation/quality-amendment-review/u9-l09kzywd/`

Each snapshot contains the same 22 required immutable files. Green uses commit `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c`; red substitutes **only** the parent FavoriteAPI blob, with permanent project/stub/Program assertions otherwise byte-identical. The product source checks also read that snapshot's actual post-PB1 sources. No shared checkout build outputs or other owner's source were used or altered.

Command in each snapshot:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/quality/cache/dotnet-cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
bash plan/tests/c2/run.sh
```

| Snapshot | Exit | Fresh observed result |
| --- | --- | --- |
| Original builder + permanent committed assertions | **1** | Compiles and executes; **64** failures, four per case: exact field set/count, contaminated timestamp, missing privacy, encoded feature boundary before signing. Ends `FAIL favorite forms preserve fields/signing (failures=64)`. |
| Corrected immutable commit, unchanged assertions | **0** | `PASS 451 assertions`, including every original c2 check. |

Corrected FavoriteAPI SHA-256 is `1e1d56bdeb5b9716e49c6160e92a3a31bcd17362121db0a80bbd7717bb81105d`, matching executor evidence. Permanent Program SHA-256 is `b7374256690cbecb28d9254ff4d34451137070b733b28dfb82612a5dfd08b456` in both snapshots. `git diff-tree --check 7ff548b^ 7ff548b` exits **0**. The red is behavioral, not compilation failure or a source-text surrogate, and independently reproduces the executor's reported red/green.

## Structure and remaining limits

**Source/file structure: PASS.** The correction stays at the two affected builder boundaries; the existing owning c2 batch gains a focused real-builder method and thin request-only seams. No duplicate product algorithm, new production helper, dead/orphan product file or unnecessary dependency is introduced. Existing parser/service/paging ownership remains clear.

The executor's Windows compile attempt returned **127 / powershell: command not found**, before compilation. This review does not promote local .NET console execution or separately installed Linux PowerShell Core into Windows UWP/XAML/WinRT, outgoing cookie, installed-app, folder persistence or device evidence. No live favorite create/edit/delete/cleanup validation is permitted or was performed. Whole-suite, release and later shared documentation disposition remain separate from this targeted PASS.

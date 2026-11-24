# U1 execution

Status: **IMPLEMENTED + OFFLINE VERIFIED; Windows/session gates BLOCKED; independent implementation review pending.**

Owner: source_executor. Started on `dev` at `3317305f5c606cd4ca33f21a611c23f15a18dada`; `git pull --ff-only` returned Already up to date. Other owners' cloud/test changes were present and preserved. Only this slice's explicit files are staged.

## Files and behavior

- `src/BiliLite.UWP/Api/CommentApi.cs`: Like, ReplyComment and AddComment append an explicit `&csrf=` field; all four actions URI-escape the existing raw CSRF token once, including DeleteComment. Empty token remains an explicit empty field. Routes, methods, cookie metadata, opaque IDs, action/type and caller message conventions remain unchanged. ApiHelper/cookie/transport/UI files are untouched.
- `plan/tests/comments/CommentsProbe.csproj`: links the real product CommentApi.
- `plan/tests/comments/CommentRequestStubs.cs`: minimal RestSharp enum, request metadata and ApiHelper seams. The configurable CSRF is raw, matching actual ApiHelper.cs:70 and its CookieService return contract. No builder implementation or transport is copied. Unused WBI signing throws rather than pretending success.
- `plan/tests/comments/Program.cs`: preserves all original 99 assertions, adds 757 request assertions across ordinary/reserved/non-ASCII/empty dummy token, types 1/17/12, like action 0/1, direct/child replies, all four builders, IDs above 2^53 and raw Add/pre-escaped Reply text with emoji/Chinese/newline/reserved/literal-percent content. Form parsing retains literal-key multiplicity, checks exact field sets and decodes once.

## Reproducible red/green

Run from repository root, with SDK 8.0.427/runtime 8.0.33 already installed by quality:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/quality/cache/dotnet-cli NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget bash plan/tests/comments/run.sh
```

1. Added permanent request assertions/project/stub before modifying the real builder. The command compiled, executed and returned **1**. Output included `FAIL request: /x/v2/reply/action single literal csrf key`, `exact decoded type value`, add/reply missing-key/message failures and delete reserved-token failures; terminal output: `FAIL request forms: all raw-token, multiplicity, field and text checks (failures=174)`. This is genuine request behavior failure, not harness compilation failure.
2. Changed only the owned builder source; same permanent assertions unchanged. Command returned **0**, terminal **PASS 856 assertions**. Original parser/mapper/silent/cursor/dead-path assertions also passed. Raw complete logs: `/workspace/work/maintenance-foundation/source-execution/u1-red.log` and `u1-green.log` (untracked scratch).
3. `git diff --check`: **0**, no output. Explicit Bash is required on this Linux host; published `sh` commands invoke dash and are known incompatible with BASH_SOURCE.
4. `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`: **127**, `/bin/bash: powershell: command not found`. UWP compilation never started. Windows/VS/UWP SDK and controlled WinRT/authenticated session journeys remain **BLOCKED**, no runtime/session/release-ready claim. No live account action occurred.

## Commit and handoff

The U1 source/test/evidence commit is the commit introducing this file (`fix: encode explicit csrf fields in comment actions`). Commit/push and remote verification will be recorded in the PB1 index after the sequential U1 commit has a stable SHA. Shared/main documentation remains reserved for its later owner; E1 dialog recovery remains separate.

U1 commit/push completed: `5eec54ecd094bdb51ec43b42d9f901d8fae7c993`; `git push origin dev` exited 0; immediately afterward both `git rev-parse HEAD` and `git ls-remote origin refs/heads/dev` returned that exact SHA. PB1 started only after this verification.

# PB1 execution

Status: **IMPLEMENTED + OFFLINE VERIFIED; Windows compile BLOCKED; independent implementation review pending.**

U1 committed/pushed first as `5eec54ecd094bdb51ec43b42d9f901d8fae7c993`; exact remote equality verified. PB1 startup `git pull --ff-only` returned Already up to date. Other-owner in-flight changes are preserved.

Permanent comments/c1 assertions were added before any parser source correction. Against unchanged actual parser sources: comments exited 1 with 12 exceptions; c1 exited 1 with 4 exceptions. All four readers threw OverflowException at `2147483648`/`-2147483649` and InvalidCastException at `9223372036854775808`/`-9223372036854775809`. Both runs compiled and exercised remaining boundary/payload controls before reporting exceptions. Raw logs are in `/workspace/work/maintenance-foundation/source-execution/pb1-comments-red.log` and `pb1-c1-red.log`.

An initial c1 invocation without PATH returned 127 (`dotnet: command not found`); c1 uses PATH directly. Adding the installed SDK directory to PATH produced the genuine red run above. No harness/assertion was changed to address this environment prerequisite.

## Source and assertions

- `src/BiliLite.UWP/Parsing/ApiEnvelope.cs`: one internal `TryReadBusinessCode` reader, shared by Read/ReadResult, VideoEnvelope and WebCommentActionParser. Rejects missing/null/noninteger tokens before conversion. Explicit BigInteger and UInt64 bounds guard those representations, with remaining primitive integers read losslessly as Int64 and bounded before narrowing. No widening BusinessCode, string/float coercion, exception catch, clamp or wrap; invalid codes return existing Envelope / `envelope.code` / zero (unspecified) BusinessCode.
- `Parsing/Video/VideoEnvelope.cs`, `Parsing/Comment/WebCommentActionParser.cs`: replace unchecked Int32 reads with the shared guard only. Existing message decoding/fallback, in-range rejection before payload and all per-endpoint payload rules remain intact. No parser migration or new product file/project/dependency.
- `plan/tests/comments/Program.cs`: 61 additional permanent assertions covering ApiEnvelope.Read, ReadResult and comment actions.
- `plan/tests/c1/Program.cs`: 25 additional assertions calling actual internal VideoEnvelope in the linked assembly. Existing comments/c1 assertions unchanged; season1 source/test inputs unchanged.

Every entry point gets both sides of Int32 overflow, both sides beyond Int64, null/missing/string/float/bool codes, Int32 extrema/-404 exact rejection/message without data, failed/null transport and malformed JSON. Payload controls separately preserve required object data/result, raw/absent video data, action success without data, and existing message fallback. All parsing remains silent as asserted by the existing batches.

## Commands and green evidence

From `/workspace/bilibili-uwp-liter`:

```bash
export PATH=/workspace/work/maintenance-foundation/quality/dotnet:$PATH
export UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality
export DOTNET_ROOT=$UPSTREAM_UNIT_TOOLS/dotnet
export DOTNET_CLI_HOME=$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli
export NUGET_PACKAGES=$UPSTREAM_UNIT_TOOLS/cache/nuget
bash plan/tests/comments/run.sh
bash plan/tests/c1/run.sh
bash plan/tests/season1/run.sh
git diff --check
rg -n 'codeToken.Value<int>|TryReadBusinessCode' src/BiliLite.UWP/Parsing
```

After the product correction, **the unchanged permanent assertions** returned: comments **0 / PASS 917 assertions**, c1 **0 / PASS 186 assertions**, season1 **0 / PASS 117 assertions**. Raw full outputs: scratch `source-execution/pb1-{comments,c1,season1}-green.log`. No compile failures or behavioral failures remain in these three affected batches. `git diff --check` returned **0**, no output. Targeted search returned the single helper declaration and its three call sites, with **no remaining `codeToken.Value<int>`** under Parsing. No unrelated dynamic/playlist source change warranted expanded per-slice runs; quality owns separate whole-suite verification after this source owner finishes.

The earlier U1 Windows command returned **127 / powershell: command not found**. The same Linux host still cannot compile UWP or run the application. Windows/VS/UWP SDK build remains **BLOCKED**, and no live malformed server response, app crash, UI/session/device pass or release-readiness is claimed. No manifest, generated wire contract, assets, account migration, main docs or quality-owned files were edited by this executor.

## Commit ownership

Only the three product parsers, two probe programs, updated U1 evidence, this evidence and the source-stream index belong in the PB1 commit (`fix: reject out-of-range api business codes`). Exact stable source/test commit SHA and remote confirmation are appended after committing/pushing; an evidence-only follow-up records those immutable hashes. Other-owner working-tree changes remain unstaged and preserved.

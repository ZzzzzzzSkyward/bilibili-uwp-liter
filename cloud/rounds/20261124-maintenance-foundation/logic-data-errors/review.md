# 审查：Logic/data/error requirements — U1 and PB1

- 轮次：`20261124-maintenance-foundation`
- 审查类型：需求
- 审查员：`logic_review`，独立需求审查
- 结论：**PASS**
- 审查基线：`dev`，`41dc562fba6e45896b0c4ba604f9ec38f4661e73`，2026-11-24 UTC
- 批准范围：`requirements.md` §4 的 U1，然后 §6 的 PB1；两个独立切片按顺序执行，共用一个排他的 comments 测试 owner。此 PASS 不批准其余分诊项直接实施，不是实现、Windows 编译、设备或发布 PASS。

## 审查范围与证据

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, memory `INDEX.md`, referenced decisions/run record, round `plan.md`, `intake.md`, `review-dispatch.md`, `parser-boundary-input.md`, all three final requirements artifacts, logic `source-evidence.md` and verification `evidence/baseline.md`. Reviewed actual source and existing comments/c1/season1 compile inputs and relevant assertions, rather than accepting the requirements' defect descriptions alone.

Also read the subsequently supplied draft `maintenance-plan.md`. Its U1→PB1 sequence, shared comments ownership, request/parser invariants, separate next-wave requirements and explicit platform limits agree with this logic approval. The two synthesis corrections recorded below concern later work and do not block either approved source slice; they should be reconciled before treating the draft as the canonical accepted plan.

Confirmed boundaries:

- **U1 is a source-proven request defect.** `src/BiliLite.UWP/Api/ApiHelper.cs:70` returns a raw token for either boolean argument. `Api/CommentApi.cs:90,104,128` concatenate it into the preceding type/message field; `:116` already has an explicit csrf key but leaves the raw value unescaped. A local fix covering all four action builders is appropriate. Search finds only three active `GetCSRF(true)` calls, all here; changing the shared helper is unnecessary and remains prohibited.
- **The asymmetric text interface is real.** `Controls/CommentControl.xaml.cs:523,569` call ReplyComment with `Uri.EscapeDataString(...)`; `Dialogs/SendCommentDialog.xaml.cs:38–39` passes plain text to AddComment. §4.1 preserves those interfaces. Encoding replies again would break existing callers. IDs arrive as strings and must not be converted through floating point.
- **Cookie/transport claims are bounded correctly.** `Services/CookieService.cs:195–212` prefers the cookie jar, mirrors settings and returns an empty string when neither source supplies CSRF. `Helpers/Utils.cs:35–48` routes POST without consulting `need_cookie`/`ExtraCookies`; `Helpers/HttpHelper.cs:394–413` creates WinRT HttpClient and sends UTF-8 form content. Neither fact proves the absence or correct installation of session cookies. Builder fixtures prove form construction only.
- **PB1 affects exactly three current integer-code conversion sites.** Targeted search `rg -n 'codeToken.Value<int>' src/BiliLite.UWP/Parsing` returns `Parsing/ApiEnvelope.cs:49`, `Parsing/Video/VideoEnvelope.cs:40`, `Parsing/Comment/WebCommentActionParser.cs:50`. ApiEnvelope exposes separate data/result entry points, giving four required test paths. The proposed single internal bounded reader, reused at these sites, preserves the Int32 BusinessCode contract without a new public type, dependency or parser migration.
- **Payload policies differ intentionally.** `ApiEnvelope.cs:60–70` requires the selected object payload; `VideoEnvelope.cs:50` returns the raw/absent data token for its feature parser; `WebCommentActionParser.cs:52–73` decodes the existing code/message DTO and requires no data. PB1 retains these rules and existing message/fallback behavior. `Models/Api/Comment/WebCommentActionResultDto.cs:10–16` confirms Code is Int32. Code validation must precede DTO conversion so an oversized integer cannot merely move its exception into deserialization.
- **Existing probe inputs are suitable.** Comments links ApiEnvelope, action/page parsers and DTOs, but not CommentApi. c1 links ApiEnvelope and `Parsing/Video/*.cs`; season1 links ApiEnvelope and real result-envelope consumers. Existing comments assertions cover missing/string code, transport/JSON failure, rejection, no-data action success, empty lists, cursor-source separation, pinned deduplication and silent parsing. New range fixtures were absent in the inspected programs. Extending these owners is preferable to a parallel imitation of the product.

The source hashes of all five parser/DTO files listed in `source-evidence.md` match the current checkout. Additionally compared those five files and the comments/c1/season1 Program/project files with the quality baseline snapshot: **11/11 MATCH**, no mismatches.

Cross-stream reconciliation:

- UI/UX §4 agrees on the existing Add/raw and Reply/escaped interfaces, exact user text, one csrf field, and separate dialog recovery work. Logic §4 supplies the explicit empty-token and reserved-token behavior, including DeleteComment escaping. Keep those stronger concrete cases in implementation; “delete regression” does not exempt delete from the same value-encoding contract.
- Verification §4 agrees that tests must compile real CommentApi and establish red/green before/after evidence. No auth expansion is required merely for a local builder change when the shared helper/cookie services remain untouched; shared-helper changes would exceed the approved U1 scope and require review.
- `review-dispatch.md` resolves shared documentation ownership: a later single documentation owner updates PLAN/ROADMAP/VALIDATION/DEVELOPMENT. The logical documentation requirements in the streams do not grant concurrent write ownership. Quality must not modify comments/c1/season1 during the source executor's ownership.
- Historical accepted device rows stay accepted under `plan/VALIDATION.md:6–12`. New behavior has no fresh Windows/session evidence. UX's optional maintainer confirmation wording does not authorize reporting changed behavior runtime-verified; the logic artifact explicitly requires evidence before that claim.

## P0（必须修复，阻断）

No unresolved **requirements-review** P0 finding. The existing U1 product defect remains P0 and is the approved first correction; this PASS does not mark it fixed.

U1 implementation must fail review if any action still lacks a singular explicit csrf key, alters another field with the token, corrupts user text, or changes the global GetCSRF contract. Verify those properties using the linked actual builder and multiplicity-preserving decoded form assertions.

## P1（必须修复，可本轮闭环）

No unresolved **requirements-review** P1 finding. PB1 is a reproduced product boundary defect with sufficient scope and acceptance to implement after U1.

Retain the following required implementation gates; they are specified acceptance, not deferred defects in these requirements:

1. U1 new assertions must execute and fail against the old real builder for the malformed request, rather than fail to compile due to incomplete stubs. Capture the same assertions passing after the fix. Existing assertions remain intact.
2. PB1 must reject both sides of Int32 and integer tokens represented as BigInteger beyond Int64 without throwing or succeeding. Guard token type before conversion; never accept string/float/bool or wrap/truncate/clamp into zero.
3. PB1 permanent assertions must show red against the current real readers and green unchanged after correction. Preserve Int32.MinValue/MaxValue and ordinary rejection codes/messages, rejection before payload validation, transport/JSON categories, and each reader's payload policy.
4. U1 and PB1 must be separate sequential slices with per-slice evidence and commits. Their overlapping comments program and main documentation cannot have concurrent writers. The orchestrator assigns PB1's separate execution/final-review evidence path before dispatch.

U9 favorite separators, U5/U8 search identity/order, E1/UX-02 dialog lifetime/retry and U2 live authentication remain follow-ups. Source spot checks support their classification: FavoriteAPI `:122,141` concatenate directly after MustParameter's timestamp (`ApiHelper.cs:179`); SearchAPI `:29–31` omits nonempty order and has seven `buvid3=temp` assignments; SendCommentDialog `:28–58` uses legacy code and incomplete async event/retry handling; active LiveRoomVM `:732` calls LiveMessage, whose `:187–191` join lacks discovery and `:235–237` accepts operation 8 without result validation. **None is implementation-approved by this review.** Require a detailed owned contract and its own requirements review before changing those paths. Preserve the current account hold and measurement prerequisites.

## P2（记录到 ROADMAP）

No new P2 defect requires expansion of U1/PB1. Two corrections apply to the later supplied synthesis artifact, owned by the orchestrator:

1. **SYN-01 — preserve the reviewed layout target.** `maintenance-plan.md:49` introduces `320/480/800+ width`, while UI/UX requirements §3 UX-05 and §6 specify the observed smallest usable Windows window plus 800×600/1280×720 effective-pixel samples, and expressly reject inventing a mobile/320px target. Replace the synthesis's width list with that reviewed matrix and retain 100/150/200% text scale. Verification: the final plan and UX requirement name the same supported-window/sample contract, without an unsupported mobile requirement. This is a plan consistency correction, not evidence that current UI clips at any width.
2. **SYN-02 — retain QF3's before-release severity/deadline.** `maintenance-plan.md:51` labels QF3 P2 without carrying verification requirements §3's `P1 before next release` gate. The layout-guard fixture repair may remain a later bounded tooling slice, but the canonical plan must state that successful Windows compilation/full packaging/layout/install/launch evidence is P1 before the next authorized release, currently BLOCKED here. Verification: its QF3 row or explicit linked prerequisite retains that deadline and does not imply the gate can be deferred through a release. This correction authorizes no release or Windows claim.

Existing documentation/maintenance drift is already identified in the input artifacts: obsolete `sh` invocation, historical clone assumptions, source-versus-device distinctions and shared ownership. Carry those to the designated documentation/quality owner rather than changing root documents during this review or bundling unrelated cleanup into U1/PB1.

## 验收标准核对

| Area | Reviewed executable acceptance | Result |
| --- | --- | --- |
| U1 routes/fields | POST action/add/del unchanged; exact field sets; unchanged oid/rpid/type/action/root/parent; like action 0/1 and types 1/17/12; IDs above 2^53 retain exact text | PASS — §4.1/4.3 complete |
| U1 CSRF | Exactly one literal csrf field in all four builders; ordinary, reserved/non-ASCII and empty raw tokens; decoded value exact; no injected field; delete included | PASS — multiplicity-preserving parser explicitly required |
| U1 text | Add raw input and Reply pre-escaped input each decode once to original Unicode/emoji/newline/reserved text; literal percent sequences remain exact | PASS — matches actual callers |
| U1 test seam | Link real CommentApi into existing comments batch; stub only platform/helper/enum dependencies with configurable raw token; no builder implementation copied into stub | PASS — narrow dependency contract |
| U1 regressions | Existing 99 parser/mapper assertions preserved; red old builder, green corrected builder; Bash runner and diff check | PASS — actual red/green still executor work |
| PB1 code bounds | All four entry points: ±Int32 overflow and ±beyond Int64 return Envelope at envelope.code, no exception/success/invented code; reject missing/null/string/float/bool | PASS — both ordinary integer and BigInteger representations addressed |
| PB1 boundaries/rejection | Int32 extrema and -404 preserve Rejected/code/message, including absent data; code check before payload check; existing message fallback unchanged | PASS — preserves typed result semantics |
| PB1 payloads | data/result selected explicitly; ApiEnvelope absent/null = Envelope, nonobject = Payload; Video raw/absent data preserved; comment action success does not require data | PASS — no unification of incompatible endpoint policies |
| PB1 verification | Permanent real-source red/green assertions in comments/c1; run comments+c1+season1 and diff check; preserve existing behavior and silence | PASS — owning projects already compile required readers |
| Ownership | U1 product: CommentApi only; comments project/program/stub. PB1 product: ApiEnvelope, VideoEnvelope, WebCommentActionParser; comments/c1 programs. Sequential shared owner; later single docs owner | PASS — no shared helper/transport/account/player/project/dependency expansion authorized |
| Platform claims | Offline result may be implemented/offline-verified; Windows compile/session remain explicitly BLOCKED/FAILED; no release-ready/runtime claim | PASS — no missing platform evidence hidden |

## 测试/构建证据

Reviewer independently replayed the existing scratch executables with `/workspace/work/maintenance-foundation/quality/dotnet/dotnet` from the repository root. These were previously compiled by the requirements/quality tasks; **this replay is not a fresh compile**. Inspected their current source/project inputs and verified the hashes noted above.

| Command argument to the scratch dotnet executable | Exit | Observed result |
| --- | --- | --- |
| `/workspace/work/maintenance-foundation/logic/parser-boundary/bin/Debug/net8.0/ParserBoundaryProbe.dll` | 1 | `CONTRACT_VIOLATIONS=16`; each of four readers throws OverflowException at 2147483648/-2147483649 and InvalidCastException at ±beyond Int64. In-range rejection/message and zero-object controls pass; comment action without data succeeds. |
| `/workspace/work/maintenance-foundation/quality/baseline/plan/tests/comments/bin/Debug/net8.0/CommentsProbe.dll` | 0 | `PASS 99 assertions` |
| `/workspace/work/maintenance-foundation/quality/baseline/plan/tests/c1/bin/Debug/net8.0/C1Probe.dll` | 0 | `PASS 161 assertions` |
| `/workspace/work/maintenance-foundation/quality/baseline/plan/tests/season1/bin/Debug/net8.0/Season1Probe.dll` | 0 | `PASS 117 assertions` |

The U1 request builder has not yet been compiled/probed in this review; current red evidence is source construction. Permanent old-builder red/new-builder green is required at execution. Existing parser passes cannot close U1.

The broader verification baseline remains **FAILED** (41/43 directory batches pass; single-file stages include compile failures and an unexecuted AUTH01B tail). This review does not convert unrelated harness failures to PASS. The actual-source malformed-code failure was independently reproduced by executable replay; it is not evidence that a live server emits those values or that a device crash occurred.

Required later commands are `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/comments/run.sh` for U1, then the same Bash invocation for comments, c1 and season1 for PB1, with `git diff --check` per slice. Use the declared Bash interpreter; explicit `sh` on this host invokes dash and is a demonstrated harness prerequisite failure.

Windows compile command remains `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`. Prior baseline exit was 127 (`powershell: command not found`); VS/UWP/Windows SDK/runtime are unavailable here. No new Windows compile, transport fixture, authorized comment session, installed launch or device test was performed. Keep those gates BLOCKED/FAILED until their actual environment and evidence exist. No product, probe, build, main documentation, dependency, asset, credential or branch mutation was made by this reviewer; the only owned deliverable is this file.

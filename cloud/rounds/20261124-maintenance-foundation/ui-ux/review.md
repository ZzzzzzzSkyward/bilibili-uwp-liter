# 审查：UI/UX maintenance policy and U1 user acceptance

- 轮次：`20261124-maintenance-foundation`
- 审查类型：需求
- 审查员：`ux_review`（independent requirements reviewer）
- 结论：**PASS** — UI requirements/U1 UX acceptance and the revised canonical plan are consistent; UXR-01 is closed on re-review.
- 审查基线：`dev` / `41dc562fba6e45896b0c4ba604f9ec38f4661e73`

## 审查范围与证据

The UI requirements and revised `maintenance-plan.md` approve the maintenance policy and U1 request-construction slice's user acceptance. The original UXR-01 plan consistency finding is closed below after the orchestrator's requested re-review. This is not implementation approval, a new device pass, or authorization to implement the other UI backlog rows. `review-dispatch.md` explicitly limits this pass to the policy/U1 scope; UX-02–07, the logic stream's E1/dialog work and other feature changes require their own scoped requirements/review and file assignment.

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, memory INDEX and the active run entry, round plan/intake/review dispatch, all three final requirements artifacts, UI evidence, quality baseline evidence and the actual comments baseline log. At the orchestrator's additional dispatch, also reviewed the draft `maintenance-plan.md`. No requirement, product, test, build or main documentation file was changed. This review owns only this file.

Independent source checks used `rg -n`, `rg --files` and `nl -ba … | sed -n …` against the working tree. In particular:

| Subject | Actual source/document checked | Review result |
| --- | --- | --- |
| Main user needs | `README.md:13–23`, `docs/FEATURES.md`, `Pages/HomePage.xaml:7–130`, `MainPage.xaml:18–22` | The journey inventory covers browsing/search, playback/queue/resume, comments, live, account/session, downloads/local media, history/favorites/later, settings and diagnostics. It does not invent endpoint parity as a user requirement. |
| U1 and encoding | `Api/ApiHelper.cs:70`, `Api/CommentApi.cs:83–130`, `Controls/CommentControl.xaml.cs:523,569`, `Dialogs/SendCommentDialog.xaml.cs:38–40`, `Services/CookieService.cs:195–212` | The raw-token concatenation is confirmed. Reply callers already escape text; AddComment receives raw text. The proposed interface preserves both. Empty-token behavior is owned by the existing cookie service. |
| UI mutation/failure boundaries | `CommentControl.xaml.cs:368–401,509–585,608–641`; `SendCommentDialog.xaml.cs:28–58`; `CommentControl.xaml.cs:671–675` | Like/reply/delete check the action parser before successful mutation. Top-level send uses the older decode and has distinct event-lifetime/retry risks. UX-02/E1 correctly remain separate from U1. The audit does not claim the top-level send path has the same login guard as those three actions. |
| Existing bulk/import/recovery | `Pages/DownloadPage.xaml:141–199`, `.xaml.cs:64–162,284–303,344–361`, `Modules/DownloadVM.cs:624–714`, `Pages/User/FavoriteDetailPage.xaml:64–85` | Bulk start/pause/delete, local import/reimport and favorite selection/delete/move/copy exist. Completed-file deletion already warns of permanence. The in-progress confirmation and swallowed errors are distinct source gaps, not evidence that all destructive actions lack confirmation. |
| Empty/error/retry | `Pages/SearchPage.xaml:19–55`, `Pages/Home/RecommendPage.xaml:142–188`, `Modules/Home/RecommendVM.cs:50–122`, `Modules/Home/DynamicVM.cs:85–131,418–428` | Search has loading/empty/refresh elements. Recommend lacks persistent initial empty/error presentation. Dynamic append failure retains the pinned source and supports an explicit retry path. The requirements preserve these differences instead of mandating a global UI rewrite. |
| Keyboard/accessibility/theme | `MainPage.xaml:18–22`, `Controls/PlayDirectoryControl.xaml:32–138`, `Controls/PlayerControl.xaml.cs:524–546`, `Dialogs/CommentDialog.xaml.cs:37–38`, `Controls/RoundButton/RoundButton.cs:6–10` and `.xaml:14,67`, `HomePage.xaml:13,116,118,130` | Current shortcuts, directory names/focus routing and popup theme fix are real. Missing explicit action names are appropriately described as source observations requiring UIA inspection. No rendered focus, screen-reader, contrast or clipping pass is inferred. |
| Live/login limits | `Modules/Live/LiveMessage.cs:88–170`, `Modules/User/LoginVM.cs:609–674` | Existing reconnect supervision/backoff is separate from U2 discovery/authentication. The QR poll source risk is accurately identified without claiming a reproduced outage. |
| Diagnostics/import/export | `Pages/SettingPage.xaml:360–368,583–598`, `.xaml.cs:976–1044`, `document/DIAGNOSTICS.md`, `Services/DiagnosticBundle.cs` | Rule import/export and diagnostic export already exist; export cancellation, truncation and success/failure paths are implemented. Busy/duplicate activation and horizontal action reachability remain bounded checks. No automatic sharing is proposed. |

Product shorthand paths in this review resolve under `src/BiliLite.UWP/` as in the requirements. Line references are to the reviewed baseline, not a future implementation.

## P0（必须修复，阻断）

No P0 requirements defect found. The existing product U1/P0 is correctly prioritized and has an executable, bounded acceptance contract. This PASS does not mean U1 has already been fixed.

## P1（必须修复，可本轮闭环）

**UXR-01 — CLOSED on re-review, 2026-11-24.** The original draft's `maintenance-plan.md:49` changed UI/UX follow-up acceptance to “320/480/800+ width”, contradicting UI requirements §3 UX-05 and §6 and expanding future layout work to unestablished product targets.

- **Owner/fix completed:** orchestrator changed the UX-03–06 queue row to the actual minimum usable Windows window plus 800×600/1280×720 effective-pixel samples and retained 100/150/200% text, keyboard and Narrator evidence. The added paragraph immediately after the work queue links UI requirements §6 and explicitly retains essential actions, complete-title access without overlap, light/dark/follow-system/high contrast, focus restoration and Narrator/UIA names/states. It states that no mobile-width support target is added.
- **Verification:** reread the revised plan and compared its UX-03–06 row and matrix paragraph with unchanged UI requirements §3 UX-05 and §6. The unsupported `320/480` support claim is absent and the requested dimensions/accessibility contract is retained. This closes the documentation consistency issue without changing requirements or product acceptance. No additional device result is claimed.

No unresolved P1 requirements defect remains within the approval scope. The canonical plan's U1/PB1 order, E1/UX-02 unified dialog ownership, disjoint QF1 work, historic acceptance and explicit platform blockers agree with the reviewed streams.

UX-02 and logic E1 describe the same later dialog area and must use one exclusive owner. Current source confirms the missing event deferral/cancel handling, incomplete re-enable paths and legacy success decoding; the exact visible closure/draft-loss behavior remains untested on Windows. A later reviewed dialog slice must cover blank input, delayed success, rejection, malformed/missing code, transport failure, repeated activation and dismissal during an in-flight write. Failure must retain the draft and restore retry; only a parsed success or explicit user dismissal closes. These are recorded follow-up obligations, not a reason to expand the builder-only U1 change.

## P2（记录到 ROADMAP）

No new P2 requirements defect is required to obtain PASS. Preserve these actionable existing records when the single documentation owner updates the canonical maintenance plan/ROADMAP:

- **UX-03:** recommend empty/initial-error/append-error states must be separately scoped and tested with successful zero rows and failed requests; retain accepted rows/cursor on append failure.
- **UX-04:** the in-progress-download confirmation must accurately describe partial-file deletion, and failed single/bulk operations must show a truthful recoverable result. Existing completed-download warnings remain accepted.
- **UX-05:** measure the favorite empty label's fixed `444` left margin and fixed search/recommend item slots at the supported minimum window and specified text scales before reporting clipping or changing layout.
- **UX-06:** inspect actual UIA names, keyboard focus and export activation before claiming a runtime accessibility/busy-state defect. Short Chinese action names, readable focus and no duplicated spoken icon/text remain the acceptance.
- **UX-07/U2:** preserve their measurement/session/identity prerequisites and concrete next actions. No player rewrite, new BUVID acquisition or duplicate reconnect work follows from this PASS.

New bug records must identify source diagnosis versus a real device reproduction. Accepted shipped rows remain accepted until the repository's new-BR policy applies; do not create fictional user reports or erase these source leads because runtime is unavailable.

## 验收标准核对

### U1 executable contract — PASS

The UI requirements §4 and logic requirements §4 agree. Use the logic stream's more explicit field/multiplicity and absent-token contract as the shared implementation contract; it adds detail without conflicting with the UX acceptance:

1. Link the **real** `Api/CommentApi.cs` into the existing `comments` batch. Stubs may supply UWP-dependent ApiHelper/ApiModel/RestSharp seams and a configurable raw token, but may not recreate the builder. `CommentsProbe.csproj:17–29` currently links parser/DTO sources, so the existing 99 checks do not establish request correctness.
2. Like, reply, add and delete each retain their current POST route and ordinary field set, with exactly one literal `csrf` key. Decode its value once and require equality with the supplied token, including `dummy+&=%中文`; no token may contaminate `type`/`message`, inject another field or create an alias key. Empty token means one explicit empty field, with no fabricated authentication, new builder error or automatic retry.
3. Add receives raw text and escapes it once. Reply receives `Uri.EscapeDataString(rawText)` from the current callers and must not escape again. Once-decoded values must preserve Chinese, non-BMP emoji, newline, reserved punctuation and literal `%` sequences, including text resembling `a&csrf=other+b%`. Form parsing in the probe must retain key multiplicity, so dictionary overwrites cannot hide duplicate keys.
4. Preserve exact opaque ID strings above `2^53`, distinct root/parent/rpid, action 0/1 and representative types 1/17/12. Preserve endpoints, transport/cookie behavior and public method signatures. Do not change global `GetCSRF(bool)` semantics or broaden ownership to dialogs, controls, credentials or transport.
5. Add the relevant request assertions first; record their behavioral failure against the unchanged real builder, then run the same assertions green after the fix. A stub compile failure is not a meaningful red result. All existing parser/mapper assertions stay intact. `git diff --check` and the owning Bash-run batch must pass.
6. Preserve separate statuses for offline form correctness, Windows compile, controlled WinRT form/cookie inspection and authenticated action outcomes. Actual server rejection must not be presented as success. U1 does not claim to repair E1's malformed-response success decode, duplicate activation, draft retention or dialog lifetime.

The section 6 “no duplicate write during pending state” scenario is part of the later UI-state acceptance when that behavior is changed, consistent with §5's touched-operation limitation and the dispatch's narrow approval scope. It does not authorize editing CommentControl/SendCommentDialog during U1 or imply those behaviors passed today.

### Core coverage and platform acceptance — PASS

`plan/VALIDATION.md:6–12` explicitly makes historical shipped device rows default-accepted/optional confirmation. The UI requirements preserve that policy despite stale later “needs device” wording. This is compatible with requiring honest evidence before claiming a **newly changed** behavior freshly verified; default acceptance cannot manufacture a current Windows build/runtime result.

The UI requirements' bounded matrix is adequate for later touched UI work: actual minimum usable window plus 800×600/1280×720 effective-pixel samples; 100/150/200% text; light/dark/follow-system/high contrast; keyboard navigation and focus restoration; Narrator/UIA action/field/state names; empty/loading/error/retry; append preservation; cancel/leave and destructive-file outcomes. It avoids inventing a mobile-width target, arbitrary memory ceiling or measured contrast claim. The revised canonical plan retains this contract, closing UXR-01. A static XAML/probe pass cannot satisfy its rendered observations.

### Ownership and dependencies — PASS

- U1 has one source executor for `src/BiliLite.UWP/Api/CommentApi.cs` and `plan/tests/comments/{CommentsProbe.csproj,Program.cs,CommentProbeStubs.cs}` or one narrowly assigned request-stub file in that batch.
- PB1 follows U1 **sequentially** under the source executor's explicit later ownership because it also changes comments coverage. This UX PASS does not substitute for the logic review of PB1.
- QF1 harness work may run only with disjoint assigned files; quality work must not write comments/c1/season1 while the source executor owns them. No CI publication or dependency migration is implied.
- `plan/PLAN.md`, `docs/ROADMAP.md`, `plan/VALIDATION.md` and `docs/DEVELOPMENT.md` receive the orchestrator's single later documentation owner, as decided in `review-dispatch.md`. The various stream proposals are not concurrent grants to those files.
- All three requirements reviews must pass and exact implementation ownership must be recorded before dispatch. Later UI rows remain triage/scoping input, not approved additional work in U1.

## 测试/构建证据

Reviewer commands confirmed `git rev-parse HEAD` equals the baseline above; `git diff --name-only -- src plan docs` returned no product/test/main-document changes. `uname -s` returned `Linux`; `command -v dotnet powershell pwsh` returned no paths in this review shell. The quality stream's SDK exists separately in scratch; this PATH observation does not negate that installation.

Inspected `verification/evidence/baseline.md` and `/workspace/work/maintenance-foundation/quality/logs/batch-comments.txt`: the unchanged baseline comments run was `bash plan/tests/comments/run.sh`, exit 0, **PASS 99 assertions**. Its actual project/source inputs have no request-builder coverage. This is evidence supplied by the quality owner, not a new reviewer execution or an outgoing-request pass. The complete supplied baseline is still **41/43 directory batches passing**, with g3 and playersess failures; LM05/API01/AUTH01B compilation failures and the unexecuted later AUTH01B stage remain visible in the quality stream. Requirements approval does not change that aggregate to green.

The existing comments runner declares Bash and uses `${BASH_SOURCE[0]}`. Use `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/comments/run.sh` for the U1 gate, with the quality artifact's documented scratch SDK/cache environment as needed. The demonstrated dash/`sh` failure is a portability/documentation issue, not a request-builder product failure.

Windows gate: `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`; zero compiler errors are required when Windows/VS/UWP SDK is available. Quality evidence records exit 127, `powershell: command not found`, so compilation was not reached here. UWP startup, layout, screen-reader, picker, playback and real-session comment-write outcomes remain **FAILED/BLOCKED — required Windows/session environment unavailable**. This reviewer launched no app, ran no live action and performed no build/probe rerun. The allowed U1 completion label remains “implemented + offline verified; Windows/session checks BLOCKED” if the executor supplies its red/green evidence; runtime-verified/release-ready is not supported.

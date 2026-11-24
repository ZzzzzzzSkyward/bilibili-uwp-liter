# 审查：QF2 stage-aware offline runner and CI requirements

- 轮次：`20261124-verification-runner`
- 审查类型：需求
- 审查员：`logic_review`，独立需求审查
- 结论：**PASS — full revised requirements, including comments integration and deadline/signal controls.**
- 范围：具体 runner/selector/CI 核心及最终 comments 集成门禁。原 P1-01/P1-02 均已闭环；仅批准已审需求范围，实际实施仍由调度者明确派发文件所有权与提交窗口。本 PASS 不是实现、CI 运行或 Windows/runtime PASS。
- 审查时间：2026-11-24 UTC。Stable QF1 harness input `ca991490f0826268d22984a2275b3e28190acad8` / evidence `858b971`; comments integration now available at source/test `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`, release metadata `5210148`.

## 审查范围与证据

Read repository/cloud instructions, memory INDEX/current run, this round's plan/requirements/evidence/review dispatch, foundation QF2 requirements/review/amendment and QF1 execution/results, actual `plan/tests/run.sh`, `run-auth01b.sh`, `run-api01.sh`, `run-g1.sh`, stab runner, current comments runner and immutable comments project/Main at `1c7f6e4`. Also read comment-recovery execution/index and package-layout requirements/active ownership. No destructive control or build was run in the shared checkout.

Independently checked the directory inventory: **43** known batch directories, **42** directory csproj files, **42** directory run.sh files. The explicit 43-ID list matches actual paths, including root-runner g1 and shell-only stab. The original single-file chain has five LM blocks plus API01 and two AUTH01B executables: the historical **50 logical / 51 aggregate executable stages** denominator is valid. That historical number must remain distinct from any reviewed current manifest.

The four stable legacy runner hashes match `evidence.md` exactly. The comments runner hash changed from the earlier audit, consistently with its separate owner: the immutable released runner now extracts the complete actual HttpResults class and invokes **one** CommentsProbe program. Its Main runs original/U1/PB1 checks then DialogRecoveryChecks, and the owner's execution/index records **1070 PASS**, one command, release and exact commit. Revised §3a now binds that exact input and preserves read-only consumption; it does not grant comments writes to QF2.

Read the supplied immutable official action READMEs under `/workspace/work/maintenance-foundation/verification-runner/docs/`; all three hashes match the evidence. Their actual passages support checkout v7/full source/credential opt-out, setup-dotnet v6 exact A.B.C versions and explicit install directory/Node24 runner requirement, and upload-artifact v7 retention/missing-file/compression/hidden-file inputs. The requirement author's upstream-tag resolution remains cited evidence; no workflow exists or ran as part of this review. No external runtime/CI claim is inferred from documentation hashes.

Full re-review read the revised requirements/evidence, including resolved §3a, explicit manifest table, active deadline/status rules and controls §7. Independently verified **22/22** bound comments inputs against both their published SHA-256 values and immutable `1c7f6e4` bytes, with **0 mismatches**. Reconstructed the generated response wrapper from the exact released class suffix and runner prefix: SHA-256 `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93`, matching the binding.

Independently parsed/canonicalized the supplied manifest: **51 unique stage IDs / 50 logical IDs**, **0 missing required paths**, all 43 batch IDs in the declared order, and exactly one `batch:comments` row with argv `["bash","plan/tests/comments/run.sh"]`. Its canonical digest is `3b240e48f78bd7020abcb038eaa37a005a198452c44828f3ae0e0fd80ea676e6`, matching requirements/evidence. Recheck HEAD was `6a285d6ae409c16c149b6ef37bc1d909fa3def33`; the bound comments bytes remain identical despite unrelated metadata/other-stream commits. The proposed LM/AUTH argv are reviewed future interfaces, not claims that selectors already exist or ran.

## P0（必须修复，阻断）

None found.

## P1（必须修复，可本轮闭环）

No unresolved P1 requirement finding remains. The original two findings are retained below as closed dispositions.

### P1-01 — CLOSED: exact one-program comments binding

Revised §3a binds source/test commit `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`, release metadata `5210148fb8b3e63b32fb9f1f843ff8b5a063f1a6`, one existing comments command and its real extraction/project/Main inputs. Requirements/evidence now consistently specify **50 logical / 51 executable current stages**, separately identify the historical same-count baseline, and withdraw the 52-stage alternative. Full suite/CLI/CI acceptance uses the current explicit manifest; independent AUTH01B stages remain. The canonical table/digest and the independent checks above make the mapping reconstructible without guessing a split or selector.

The actual-HttpResults/raw-body/dialog checks are inside the same existing comments program; no comments selector/edit or ownership amendment is needed. §8 keeps comments, product, package probe and main docs read-only to this executor. Future input/decomposition changes require rebind/review. The package gate remains NOT_CONFIGURED/excluded. **Closure accepted.**

### P1-02 — CLOSED: active deadline and signal finalization contract

Revised §5 explicitly uses monotonic `min(stage_start+stage_timeout, suite_start+suite_timeout)` with suite time starting before execution preflight/first spawn. A suite-winning/equal deadline yields active TIMEOUT/suite_budget and unstarted selected BLOCKED/suite_budget, no later launch and exit 1. Stage-only timeout may continue only with suite budget remaining. SIGINT/SIGTERM yield active CANCELLED with exact signal reason, unstarted selected BLOCKED/cancelled and exits 130/143. Unselected rows stay NOT_SELECTED. The existing ≤5-second drain/kill/reap allowance and bounded report finalization do not authorize additional execution after exhaustion.

§7 now defines meaningful scratch-only external controls: stage budget 30 versus suite budget 2, and separate readiness-coordinated SIGINT/SIGTERM sent to the aggregate after its fixture spawns a child. Required outcomes include the exact active/unstarted rows, unchanged 50/51 selection denominators, no later fixture launch, parseable graceful-final reports, expected aggregate status and no surviving/delayed descendant marker. These test public orchestration behavior rather than mirror private implementation. Hard external kill remains outside guaranteed report/upload finalization. **Closure accepted as requirements; the future implementation still owes the actual control results.**

## P2（记录到 ROADMAP）

No unresolved P2 requirement finding or scope expansion. Revised §7 now explicitly places fixture/source/extraction/cache roots outside the upload/report subtree, rejects nesting under output and retains that rule on failure. Only bounded synthetic diagnostics/reports/logs may enter `offline-results`. Existing main-documentation work remains with its later single owner.

## 验收标准核对

| Core contract | Review result |
| --- | --- |
| Concrete entry point/flags, argv arrays, cwd-independent source resolution, partial label and strict unknown selection | Sufficiently specified |
| Explicit inventory, g1/stab exceptions, missing-row retention, drift detection and fixed historical/current totals | PASS — current 50/51 manifest and one-program comments binding independently checked |
| Legacy no-argument/all behavior and compile/extraction/assertion preservation | Sufficiently specified; actual blocks and QF1 boundaries inspected |
| Independent LM and AUTH Account/LoginVM selection with real completion markers | Sufficiently specified; LoginVM needs shared ICookieStore setup but must not require successful Account execution |
| Exact SDK/compiler/runtime/ref-pack inputs and PATH/cache setup; no substituted toolchain | Sufficiently specified; missing prerequisites remain visible and stab stays independently runnable |
| Sequential children, stage timeout cleanup, continue after ordinary failure | Sufficiently specified; negative failure/missing/timeout cases are meaningful |
| Whole-suite deadline, active versus unstarted results, SIGINT/SIGTERM cleanup and finalization | PASS — exact outcomes and external controls now specified; implementation results still owed |
| Incremental atomic reports, nonshrinking denominator, bounded head/tail logs, report-write failure | Sufficiently specified; never infer PASS from an absent/unfinished report |
| Separate optional package-layout gate | Correctly NOT_CONFIGURED/excluded. Its actual independently owned path is root-level `plan/tests/package-layout-probe.ps1`, not a new required C# batch |
| CI triggers/permissions/pins/toolchain/timeouts/status capture/artifact retention | PASS — reviewed scope; actual rollout/run evidence remains executor work |
| Controls and suite both run despite a control failure | Explicit captured-status Bash shape preserves both results; upload cannot clear failure |
| Actual pushed-SHA GitHub run versus local fixture/YAML evidence | Correctly separated; absent/unavailable CI is BLOCKED, never assumed PASS |
| Source/selector/workflow/doc ownership | PASS — narrow exact proposal, comments read-only; actual executor assignment and serialized commit window remain required |

## 测试/构建证据

This is a requirements review: aggregate/selector/workflow interfaces do not yet exist, so their fixtures and CI were **not run**, and no new full-suite pass is claimed. QF1's completed 43-directory/eight single-file-executable sweep remains its immutable historical evidence, not proof of a future runner interface. No need exists to rerun destructive controls against current shared files at this stage.

Read-only inventory, canonical manifest, 22-input byte/hash and exact generated-wrapper checks succeeded as described above. Current released comments source was inspected through its immutable commit; its source owner's one-program/1070-check report is a binding input, not a new reviewer run or E1 implementation approval. No runner/workflow/test/product/build/main-doc/history mutation, commit/push, live-service write or credential operation was performed by this reviewer. The only owned output is this file.

UWP/Windows/package installation/device/runtime gates stay outside the offline aggregate and explicitly unavailable/unverified under their owning platform records. Official action/toolchain inputs and successful local controls will not establish a GitHub run or Windows runtime pass. **Full requirements re-review PASS is complete**, including both P1 closures. The orchestrator may now assign the exact reviewed runner/selector/workflow scope under its release/commit discipline; this does not expand ownership or replace implementation review, the final-source full suite, actual pushed-SHA GitHub evidence (or explicit external block), or platform gates.

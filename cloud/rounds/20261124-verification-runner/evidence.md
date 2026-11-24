# QF2 requirements evidence

Date: 2026-11-24 UTC. Read-only source/inventory/documentation audit; the proposed aggregate/selectors/CI do not exist yet, so no new-runner or CI pass is claimed.

## Historical core-audit inputs and prior evidence

Core-audit read HEAD: `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c`. Content hashes below bind the inspected runners independently of concurrent cloud metadata commits.

QF1 `verification/execution.md` records implementation commit `ca991490f0826268d22984a2275b3e28190acad8`, subsequent evidence update and ownership release; its `evidence/qf1-results.md` reports an immutable 918-input snapshot with zero mismatches, 43/43 directory batches and all eight single-file executables passed (50 logical/51 executable). This is QF1 evidence, not a run of the new proposed interface.

Python filesystem inventory observed 43 runnable/project directories: `api02`, `auth1`, `auth2`, `auth3`, `auth4`, `c1`, `c2`, `c3`, `cap`, `cardlayout`, `cm1`, `comments`, `d1`, `d2`, `dirpress`, `firstrun`, `focus`, `g1`, `g2`, `g3`, `g4`, `g5`, `localdir`, `ov`, `playersess`, `playertoolbar`, `season1`, `settings1`, `smart1`, `stab`, `toastalign`, `ux0`, `ux1a`, `ux1b`, `ux1c`, `ux1d`, `ux2a`, `ux2b`, `ux3`, `ux4`, `ux5`, `uxfix`, `webrefresh`. g1 alone lacks a directory run.sh; stab alone lacks a csproj. The complete explicit list/mapping is in requirements §3.

`run.sh` has five embedded LM compile/runtime blocks then API01/AUTH01B under `set -e`. `run-auth01b.sh` has two real executables, Account and LoginVM, also under `set -e`. Current scripts provide no stage-selector interface. A prefix failure masks later execution; invoking AUTH01B independently does not isolate its LoginVM suffix. No product logic was copied or runner edited to establish this.

**Historical core-audit input (superseded by final integration below):** the then-current round plan reserved `comments/run.sh` to comment recovery. Its reviewed requirements mandate actual HttpResults + actual dialog raw-body tests and permit a second strict mechanics run; final runner decomposition is not yet published. Requirements §3a therefore keeps the foundation 50/51 historical, requires the owner’s exact stage/selector contract and another review before implementation, and specifies 50/52 if two required comments executables remain. That hold is resolved by the released binding below; no new-stage omission or competing writer is authorized. Package-layout has no finalized approved invocation here and remains a separate optional NOT_CONFIGURED gate.

## Toolchain observations

`/workspace/work/maintenance-foundation/quality/dotnet/dotnet --version` exit 0: `8.0.427`.

`/workspace/work/maintenance-foundation/quality/dotnet/dotnet --list-runtimes` exit 0: `Microsoft.AspNetCore.App 8.0.33 [/workspace/work/maintenance-foundation/quality/dotnet/shared/Microsoft.AspNetCore.App]; Microsoft.NETCore.App 8.0.33 [/workspace/work/maintenance-foundation/quality/dotnet/shared/Microsoft.NETCore.App]`.

Declared csc, SDK-internal Newtonsoft.Json and exact System.Runtime reference-pack paths all exist: True. This audit reused the previously SHA-512-verified SDK; no install or version substitution. Windows/VS/UWP remains unavailable per foundation evidence.

## Official documentation and action pins

Applied `read-docs` (`skill://plugin_asdk_app_6a314a73f8ac819195b0d55e36b9c609/read-docs/SKILL.md`). Firecrawl developer search and official-page scrapes supplied workflow syntax, exact SDK syntax/install-directory and artifact/checkout inputs. Later pinned-README scrapes were refused by the connected-app daily credit limit; they were not retried. Direct HTTPS reads of the same official immutable README URLs supplied the missing version-specific passages, without authentication or account data. Raw docs are scratch only.

| Source | Verified observation |
| --- | --- |
| [Workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) | push/PR branch filtering; specifying one permission sets unspecified permissions to none; Bash defaults, job timeout and always condition documented. No workflow was executed. |
| [actions/checkout pinned README](https://raw.githubusercontent.com/actions/checkout/3d3c42e5aac5ba805825da76410c181273ba90b1/README.md) | v7; fetch-depth 1 default/full source unless filter/sparse; persist-credentials false opt-out; Node24 lineage. |
| [actions/setup-dotnet pinned README](https://raw.githubusercontent.com/actions/setup-dotnet/0f4a354dd5547a28c70b7853dbd95d5b67886e73/README.md) | v6; A.B.C is exact version, DOTNET_INSTALL_DIR explicit install location; Node24 requires runner >=2.327.1; cache assumes lockfiles when enabled. |
| [actions/upload-artifact pinned README](https://raw.githubusercontent.com/actions/upload-artifact/cf430e030ddbb5b0abf93d22962f4752f3646cd9/README.md) | v7 usage; path, if-no-files-found error, retention-days, compression-level and include-hidden-files false options. |

Read-only `git ls-remote https://github.com/actions/<name>.git refs/tags/v7 refs/tags/v6 refs/tags/v5` exited 0 for each action. Current chosen major refs equal the immutable SHA values in requirements §6. No tag resolution or moving latest ref is needed at CI runtime.

## Input/document hashes

```text
7e5c1bd964c871cea747bda84b5a97774b1bb6656c4047b896199d063d736f73  plan/tests/run.sh
c8c8dbe2cc814983f7371119694c096d3dae4e854051ee8b87c5fa8660263993  plan/tests/run-api01.sh
229a58dce0e1208912c3778da461134893f874c88c55bf01d7ea279292711667  plan/tests/run-auth01b.sh
79fb83bde3c1dff8ea6b3ef3cd3d7ae3e027008f0f2a222fbf3b1ba609b98a77  plan/tests/run-g1.sh
25df17c81a2158f278c97adfdc7af916c1632737452464df139348a1913ebb84  plan/tests/comments/run.sh
3df94d90d0df6df603a549cc779bc6cab4b21f8deef82eb6fef1f4d88b788174  checkout pinned README.md
e81ac936133b6c7612c34c03007f9a293b2e920baf1bac353e4cfb3b50284ec5  setup-dotnet pinned README.md
82e3d7226dabea938b107b93e015aa816150d6894fff631064aa2884c26771f7  upload-artifact pinned README.md
```

The selected runner/workflow inputs had no tracked diff at the final read. Other owners’ source/cloud files may change concurrently; no global clean-worktree claim is made. Implementation-stage acceptance still owes actual failure/missing/stage-timeout/suite-budget/signal controls, selector independence, the bound current-manifest complete suite and pushed-SHA GitHub evidence.

## Final integration / independent-review response

**READY FOR FULL RE-REVIEW. P1-01 resolved in the requirement; P1-02 specified, with execution still gated on independent PASS.** No proposed aggregate, selector, timeout/signal control or CI interface exists yet; those acceptance runs are NOT RUN at requirements stage.

The controlling plan now binds E1 source/test commit `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`, release metadata `5210148fb8b3e63b32fb9f1f843ff8b5a063f1a6` and its execution index's explicit ownership release. The source owner's unchanged-green evidence is **DIALOG_CHECKS=153 FAILURES=0; PASS 1070 assertions**, preserving the previous 917 assertions. Independent implementation review remains distinct; this is source-owner evidence, not a new requirements-owner test run.

Inspection establishes one comments executable: run.sh:10–17 generates the full actual HttpResults wrapper, :33 contains the only `"$dotnet" run`; csproj:19–22 links generated actual response/actual legacy model/dialog/builder; Program.Main:32–55 runs original/U1/PB1 then `DialogRecoveryChecks.Run`. Its method:48–55 covers raw bodies, recovery/lifetime and reports checks inside that same program. No second strict-fixture or raw-body executable remains. **Current manifest is definitively 50 logical / 51 executable stages, including one `batch:comments` → `["bash","plan/tests/comments/run.sh"]`.** No comments selector or file ownership expansion is needed: consume it read-only.

Independent read-only binding command: for every current default comments .cs input, every explicit non-generated csproj Compile input, runner/project, product dialog XAML and actual HttpHelper, compare current bytes to `git show 1c7f6e4:<path>` and SHA-256. All 22 inputs matched. The read HEAD was `01c503f0b878e4949ec0aa61ef441ee2950fea03`; concurrent metadata/other-stream commits do not alter this exact content binding. Generated wrapper was reconstructed in stream scratch from the released complete HttpResults suffix plus the runner's exact namespace/usings prefix; SHA-256 matches the owner's generated-source hash `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93`. No generated source was committed or product/test runner executed/edited for this check.

| Bound comments input | SHA-256 |
| --- | --- |
| `plan/tests/comments/CommentProbeStubs.cs` | `36fbe8c286eb6f6dc0004f40f1e0c9ae2a8cdde8ef393802a72ceea8c8b1377e` |
| `plan/tests/comments/CommentRequestStubs.cs` | `2c5cd01a29d44baa03de8ba06e4b44aca5f35fb58c3fda084c795decddfe0889` |
| `plan/tests/comments/CommentsProbe.csproj` | `7e177f90708ca7b19811868b376a5044970afc34679c91998744d63696fd9bfe` |
| `plan/tests/comments/DialogPlatformStubs.cs` | `73897309ab804f5b486302e4cf8f19e25901f8b88f2dab56e67e46ac2c4935e9` |
| `plan/tests/comments/DialogRecoveryChecks.cs` | `105d587329b3a87662d78460d430e09e12e69179329a90c712436b96546f41f3` |
| `plan/tests/comments/Program.cs` | `d0ffdb212e5d3467f1f5db2e1d3d53240a11430523afc2e76c4cea548fc881e9` |
| `plan/tests/comments/run.sh` | `18881fb069e255e686a2f22ab2b2b38e8e60e37c3dc76cf824b2cf06766094b4` |
| `src/BiliLite.UWP/Api/CommentApi.cs` | `e05ddc67608afb206e285416f1f984bf4c5fb44a2edf17fe204e27dfca713d76` |
| `src/BiliLite.UWP/Dialogs/SendCommentDialog.xaml` | `95c22ee42d86e041aa73414bbe926ea5a4e82fde616aa6ebfb6af035a0ccec90` |
| `src/BiliLite.UWP/Dialogs/SendCommentDialog.xaml.cs` | `71cc068c7e38fc70eb12d9d8e4cc087b4f43a5fc2eafe47a451bd102524ccde6` |
| `src/BiliLite.UWP/Helpers/HttpHelper.cs` | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| `src/BiliLite.UWP/Models/Api/Comment/WebCommentActionResultDto.cs` | `af2a2f3c2ca5f76390d5d21fa1976ed91fea64baa254bb3561fbc77acbd10dc1` |
| `src/BiliLite.UWP/Models/Api/Comment/WebCommentPageDto.cs` | `a4309751bb7accbd31a2a4dd89582d5d32a14e3f1672fd6991b6a280f7ea1003` |
| `src/BiliLite.UWP/Models/Api/Comment/WebCommentReplyDto.cs` | `a922292cb8fb58b4485f780d120baef3bb99caed7016c58c36012e5124d65e57` |
| `src/BiliLite.UWP/Models/Api/Comment/WebCommentReplyListDto.cs` | `492c8ace520c0950e93cc108519223c238a663607b7b7675f5632d7885fe0411` |
| `src/BiliLite.UWP/Models/ApiModel.cs` | `6493168767c4c9a92637b3ac82b9ec7d67e92ac2840cf0680655887996611671` |
| `src/BiliLite.UWP/Models/Common.cs` | `5541f7a9ac31f3c17a0bfde81218c174fecd09ea486e009d0d8593c7af5c7ccf` |
| `src/BiliLite.UWP/Parsing/ApiEnvelope.cs` | `050317cddde277c46c6169f3878cb6c7accef3cec0dbbf018006cbc411948421` |
| `src/BiliLite.UWP/Parsing/ApiParseResult.cs` | `f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab` |
| `src/BiliLite.UWP/Parsing/Comment/WebCommentActionParser.cs` | `ca0e1a7003b85bc29e4b6a9b1960f20c19a59aa70182a9a56b0a99282dae88e2` |
| `src/BiliLite.UWP/Parsing/Comment/WebCommentPageMapper.cs` | `78196b8402677eb0080616d6d4d3e8ec132fc39ab2ff67a85d01c2f2c6fcf714` |
| `src/BiliLite.UWP/Parsing/Comment/WebCommentPageParser.cs` | `291ee74272777a4df09911b690cc8c61228ab191df7228e4b0b532e802184cb7` |

Canonical proposed manifest is retained in scratch `manifest-comments-integrated.json`; SHA-256 **`3b240e48f78bd7020abcb038eaa37a005a198452c44828f3ae0e0fd80ea676e6`** hashes UTF-8 JSON with sorted keys, compact separators and no trailing newline. Each entry contains `id`, `logical_id`, `kind`, `argv`, `required_paths`; array order is the following table. LM/AUTH selector argv are specified future interfaces, not claims of existing execution. All 43 directory paths and the current csproj names are observed source inputs; missing required inputs cannot reduce these fixed counts.

| Stage → logical ID (same unless shown) | Exact command argv (space-separated display) | Required entry/project or probe source |
| --- | --- | --- |
| `batch:api02` | `bash plan/tests/api02/run.sh` | `plan/tests/api02/run.sh`, `plan/tests/api02/API02.csproj` |
| `batch:auth1` | `bash plan/tests/auth1/run.sh` | `plan/tests/auth1/run.sh`, `plan/tests/auth1/AUTH1.csproj` |
| `batch:auth2` | `bash plan/tests/auth2/run.sh` | `plan/tests/auth2/run.sh`, `plan/tests/auth2/AUTH2.csproj` |
| `batch:auth3` | `bash plan/tests/auth3/run.sh` | `plan/tests/auth3/run.sh`, `plan/tests/auth3/AUTH3.csproj` |
| `batch:auth4` | `bash plan/tests/auth4/run.sh` | `plan/tests/auth4/run.sh`, `plan/tests/auth4/AUTH4.csproj` |
| `batch:c1` | `bash plan/tests/c1/run.sh` | `plan/tests/c1/run.sh`, `plan/tests/c1/C1Probe.csproj` |
| `batch:c2` | `bash plan/tests/c2/run.sh` | `plan/tests/c2/run.sh`, `plan/tests/c2/C2Probe.csproj` |
| `batch:c3` | `bash plan/tests/c3/run.sh` | `plan/tests/c3/run.sh`, `plan/tests/c3/C3Probe.csproj` |
| `batch:cap` | `bash plan/tests/cap/run.sh` | `plan/tests/cap/run.sh`, `plan/tests/cap/CapProbe.csproj` |
| `batch:cardlayout` | `bash plan/tests/cardlayout/run.sh` | `plan/tests/cardlayout/run.sh`, `plan/tests/cardlayout/CardLayoutProbe.csproj` |
| `batch:cm1` | `bash plan/tests/cm1/run.sh` | `plan/tests/cm1/run.sh`, `plan/tests/cm1/Cm1Probe.csproj` |
| `batch:comments` | `bash plan/tests/comments/run.sh` | `plan/tests/comments/run.sh`, `plan/tests/comments/CommentsProbe.csproj` |
| `batch:d1` | `bash plan/tests/d1/run.sh` | `plan/tests/d1/run.sh`, `plan/tests/d1/D1Probe.csproj` |
| `batch:d2` | `bash plan/tests/d2/run.sh` | `plan/tests/d2/run.sh`, `plan/tests/d2/D2Probe.csproj` |
| `batch:dirpress` | `bash plan/tests/dirpress/run.sh` | `plan/tests/dirpress/run.sh`, `plan/tests/dirpress/DirPressProbe.csproj` |
| `batch:firstrun` | `bash plan/tests/firstrun/run.sh` | `plan/tests/firstrun/run.sh`, `plan/tests/firstrun/FirstRunProbe.csproj` |
| `batch:focus` | `bash plan/tests/focus/run.sh` | `plan/tests/focus/run.sh`, `plan/tests/focus/FocusProbe.csproj` |
| `batch:g1` | `bash plan/tests/run-g1.sh` | `plan/tests/run-g1.sh`, `plan/tests/g1/G1Probe.csproj` |
| `batch:g2` | `bash plan/tests/g2/run.sh` | `plan/tests/g2/run.sh`, `plan/tests/g2/G2Probe.csproj` |
| `batch:g3` | `bash plan/tests/g3/run.sh` | `plan/tests/g3/run.sh`, `plan/tests/g3/G3Probe.csproj` |
| `batch:g4` | `bash plan/tests/g4/run.sh` | `plan/tests/g4/run.sh`, `plan/tests/g4/G4Probe.csproj` |
| `batch:g5` | `bash plan/tests/g5/run.sh` | `plan/tests/g5/run.sh`, `plan/tests/g5/G5Probe.csproj` |
| `batch:localdir` | `bash plan/tests/localdir/run.sh` | `plan/tests/localdir/run.sh`, `plan/tests/localdir/LocalDirProbe.csproj` |
| `batch:ov` | `bash plan/tests/ov/run.sh` | `plan/tests/ov/run.sh`, `plan/tests/ov/OVProbe.csproj` |
| `batch:playersess` | `bash plan/tests/playersess/run.sh` | `plan/tests/playersess/run.sh`, `plan/tests/playersess/PlayersessProbe.csproj` |
| `batch:playertoolbar` | `bash plan/tests/playertoolbar/run.sh` | `plan/tests/playertoolbar/run.sh`, `plan/tests/playertoolbar/PlayerToolbarProbe.csproj` |
| `batch:season1` | `bash plan/tests/season1/run.sh` | `plan/tests/season1/run.sh`, `plan/tests/season1/Season1Probe.csproj` |
| `batch:settings1` | `bash plan/tests/settings1/run.sh` | `plan/tests/settings1/run.sh`, `plan/tests/settings1/SETTINGS1.csproj` |
| `batch:smart1` | `bash plan/tests/smart1/run.sh` | `plan/tests/smart1/run.sh`, `plan/tests/smart1/Smart1Probe.csproj` |
| `batch:stab` | `bash plan/tests/stab/run.sh` | `plan/tests/stab/run.sh` |
| `batch:toastalign` | `bash plan/tests/toastalign/run.sh` | `plan/tests/toastalign/run.sh`, `plan/tests/toastalign/ToastAlignProbe.csproj` |
| `batch:ux0` | `bash plan/tests/ux0/run.sh` | `plan/tests/ux0/run.sh`, `plan/tests/ux0/Ux0Probe.csproj` |
| `batch:ux1a` | `bash plan/tests/ux1a/run.sh` | `plan/tests/ux1a/run.sh`, `plan/tests/ux1a/Ux1aProbe.csproj` |
| `batch:ux1b` | `bash plan/tests/ux1b/run.sh` | `plan/tests/ux1b/run.sh`, `plan/tests/ux1b/Ux1bProbe.csproj` |
| `batch:ux1c` | `bash plan/tests/ux1c/run.sh` | `plan/tests/ux1c/run.sh`, `plan/tests/ux1c/Ux1cProbe.csproj` |
| `batch:ux1d` | `bash plan/tests/ux1d/run.sh` | `plan/tests/ux1d/run.sh`, `plan/tests/ux1d/Ux1dProbe.csproj` |
| `batch:ux2a` | `bash plan/tests/ux2a/run.sh` | `plan/tests/ux2a/run.sh`, `plan/tests/ux2a/Ux2aProbe.csproj` |
| `batch:ux2b` | `bash plan/tests/ux2b/run.sh` | `plan/tests/ux2b/run.sh`, `plan/tests/ux2b/Ux2bProbe.csproj` |
| `batch:ux3` | `bash plan/tests/ux3/run.sh` | `plan/tests/ux3/run.sh`, `plan/tests/ux3/Ux3Probe.csproj` |
| `batch:ux4` | `bash plan/tests/ux4/run.sh` | `plan/tests/ux4/run.sh`, `plan/tests/ux4/Ux4Probe.csproj` |
| `batch:ux5` | `bash plan/tests/ux5/run.sh` | `plan/tests/ux5/run.sh`, `plan/tests/ux5/Ux5Probe.csproj` |
| `batch:uxfix` | `bash plan/tests/uxfix/run.sh` | `plan/tests/uxfix/run.sh`, `plan/tests/uxfix/UxfixProbe.csproj` |
| `batch:webrefresh` | `bash plan/tests/webrefresh/run.sh` | `plan/tests/webrefresh/run.sh`, `plan/tests/webrefresh/WEBREFRESH.csproj` |
| `LM01` | `bash plan/tests/run.sh --stage LM01` | `plan/tests/run.sh`, `plan/tests/LM01.cs` |
| `LM02` | `bash plan/tests/run.sh --stage LM02` | `plan/tests/run.sh`, `plan/tests/LM02.cs` |
| `LM03` | `bash plan/tests/run.sh --stage LM03` | `plan/tests/run.sh`, `plan/tests/LM03.cs` |
| `LM04` | `bash plan/tests/run.sh --stage LM04` | `plan/tests/run.sh`, `plan/tests/LM04.cs` |
| `LM05` | `bash plan/tests/run.sh --stage LM05` | `plan/tests/run.sh`, `plan/tests/LM05.cs` |
| `API01` | `bash plan/tests/run-api01.sh` | `plan/tests/run-api01.sh`, `plan/tests/API01.cs` |
| `AUTH01B.account → AUTH01B` | `bash plan/tests/run-auth01b.sh --stage account` | `plan/tests/run-auth01b.sh`, `plan/tests/AUTH01B.cs` |
| `AUTH01B.loginvm → AUTH01B` | `bash plan/tests/run-auth01b.sh --stage loginvm` | `plan/tests/run-auth01b.sh`, `plan/tests/AUTH01BLoginVM.cs` |

The previous 51-versus-52 integration alternative is superseded: both historical foundation and current approved manifest are 50/51, but coverage/source identities differ. Package-layout stays separately NOT_CONFIGURED/excluded, and self-controls never count as legacy assertions/stages. A future physical decomposition change requires a new reviewed manifest/ownership decision.

P1-02's controlling requirements now state active-child deadline `min(stage_start+stage_timeout, suite_start+suite_timeout)`, explicit active TIMEOUT/suite_budget versus unstarted BLOCKED/suite_budget, no later launch after suite exhaustion, existing ≤5-second drain/kill/reap allowance, and active CANCELLED/signal versus unstarted BLOCKED/cancelled with exits 130/143. Acceptance adds a shorter-suite-than-stage case and readiness-coordinated SIGINT/SIGTERM runs through the actual future aggregate; both require unchanged 50/51 totals, final parseable reports where graceful cleanup is possible, no later launch and no surviving/delayed child marker. These are defined acceptance protocols, **not executed fixture results**. Hard external kill still cannot guarantee reports/upload.

Fixtures/source/extraction/caches must remain outside the upload/report tree, including failure paths; only bounded synthetic diagnostics are uploaded. No change to official action/core tool pins, product code, QF1 scenarios, tests, workflows, main docs, history or the independently owned package guard occurred in this revision. Whitespace checks over the two owned Markdown artifacts are clean.

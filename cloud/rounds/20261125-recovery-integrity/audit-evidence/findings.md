> Historical initial pre-fix findings/source receipt. Current closure and new findings: [postfix-closure.md](postfix-closure.md) and [../final-audit.md](../final-audit.md).

# Fresh audit — reproduced residual findings

Status: **CHANGES**. Four supported P2 findings against the frozen common-gate inputs; no source/test/main-doc/index/ref mutation. The structural/provenance audit is complete in [review-verification.md](review-verification.md); controlling verdict: [../final-audit.md](../final-audit.md). All915 common source hashes currently match; HEAD is e928bf0 with the documented unpublished recovery bytes. These are actual-source observations, not native/rendering claims.

1. **FA-01 / P2 / storage owner — structurally unusable registry entries escape the typed read boundary.** `LocalMediaLibraryService.cs:201–204` accepts `{"Entries":[null]}`; actual GetSaved at127 and reopen selection at141 throw NullReferenceException. The exact actual DownloadPage.RefreshSavedCollectionsAsync body also faults with zero failure toast, because its catch accepts only LocalMediaLibraryException. `[{}]` is exposed as a saved row with null Token. Keep null/missing Entries defaults, schema and broken-asset entries compatible; detect null/unusable required identity entries within guarded registry reading, report typed registry-read, preserve committed bytes and existing grants, release the gate, and test later valid retry. Do not silently discard malformed entries or introduce a schema migration. File-based requirement amendment before execution; original storage executor owns the repair and LM02 regressions.
2. **FA-02 / P2 / recovery owner — known cancelled cleanup remnants are published as completed library media.** Actual LoadDownloading → DeleteItemAsync cancellation succeeds, permanent deletion fails, observer retires, and CleanupFailures retains the target. A subsequent actual LoadDownloadedAsync accepts the same episode with a nonzero partial file (received1/total10), producing completed_episodes=1 and empty status while cleanup=1 and active=0. Source204–225 only rejects missing/zero files; retained cleanup state is not consulted. A zero-length control is rejected. Prevent known outstanding cancelled-cleanup paths from being advertised as completed; cover episode/title subtree identity and retry/new-generation ordering using existing coordinator ownership, without a new format or a claim that all nonzero files are unplayable. Preserve unrelated valid siblings and reconfirmed cleanup. File-based requirement amendment; original recovery executor owns VM/owning batch.
3. **FA-03 / P2 / recovery owner — last recommendation action removal leaves contradictory persistent state.** Load an ordinary one-card real feed (idx10), then complete actual Dislike with code0. Items becomes empty, StatusText remains empty and AutoLoad remains true; actual LoadMore immediately sends no request because its empty guard returns. The page binds these properties directly and shows no empty message. Recompute empty/status/automatic eligibility after accepted removal on the current visible collection, preserving the existing retry/refresh and no-progress contracts and handling an intervening refresh safely. Add real feed/action regression for dislike and feedback rather than a fabricated background call. Scope remains RecommendVM and recovery batch.
4. **FA-04 / P2 / recovery owner — unknown initial download total publishes NaN progress.** Actual private GetProgress(0,0) via reflection returns NaN (total>=progress then0/0); LoadDownloading and DownloadProgress publish this value to bound Progress. Ordinary0/10=0,5/10=50,10/10=100 and20/10=0 controls are finite. Guard zero total before division and require a finite bounded result using the existing real-method seam. This establishes an invalid source value, not a native ProgressBar exception or device crash. Scope remains DownloadVM and recovery batch.

Bounded probe commands (no complete-suite rerun):

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/fresh-audit/cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/fresh-audit/RecoveryAudit.csproj -c Debug
# same environment, RegistryAudit.csproj
```

RecoveryAudit links the full actual recovery source closure and the existing exact generated source/stubs; its additional assertions are observations only, with cancellation observer continuations explicitly pumped to completion. RegistryAudit links the full real service/models/validator/loader plus existing primitive storage/FAL stubs, and the exact actual page refresh body (unique body/hash binding); fixture directories are removed in finally. No product algorithm is copied into a substitute. Programs exit0 because they record observations, not because repair acceptance passes.

Raw scratch `/workspace/work/recovery-integrity/fresh-audit/`; SHA256:

| Artifact | SHA256 |
| --- | --- |
| RecoveryAudit.cs | 2f06209247e75d02790eecf5c833f066eb128bbdfa7f4147340d4839e6a29d97 |
| RegistryAudit.cs | 2154b37c5b0569ad84826c82f9ce87f06d751c3ac76d4a3620127ab8c34f1bbb |
| RecoveryAudit.csproj | 7bd89bbae5c58c38e87ce2260a69bd76dfc9bad9d7cf430a101ad64aae197f74 |
| RegistryAudit.csproj | aec77ff971c5cef038f7bc5561ccc5ddc7721c9f935849eb1e724d400acb2287 |
| recovery-audit.log | 4e9c70db3e39201e627a49a44391f68a6ecedb8869f35f300e67165f433d3fcb |
| registry-audit.log | 01e3400c06da0ab14674523c16dc099edaff6dcb900de7222d3ef78ab07ef589 |
| input-binding.json (915 inputs / zero mismatches) | db8ceb795caee9308d56914bea17c9617b3ce6d42db6eaf2cfd455e09d918793 |
| registry-inputs.json | c5d3b6f6214d6073a341c608cc63b2d3efd71974602bcd44f046d72990282177 |

Reviewed LocalMediaLibraryService SHA256335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0; exact recovery products are the18-input receipt in verification/evidence/recovery-inputs.md. Full integration binding is source-after.json SHA25642a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e.

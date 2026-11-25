# Current portable-check evidence — PASS / FROZEN / RELEASED

Authority: [interpreter-provenance-dispatch.md](../../interpreter-provenance-dispatch.md). Private immutable outputs/snapshots/manifests: `/workspace/work/video-memory/final-verification/interpreter-checks/`. Exact commands/cwd/timestamps/exits are in commands.json; only one sampler harness and one package fixture ran. Sampler UTC03:29:58.995859→03:30:31.161503,32.166s; package03:30:31.162014→03:30:31.763111,0.601s,2026-11-25. Both exit0.

## Relevant source and interpreter custody

Source-before.json equals source-after.json: **nine** exact frozen inputs, copied byte-for-byte in source-snapshot/. These are measure-video-memory.ps1, its run.py/controlled.ps1/markers.ps1/revisions.ps1, build-release-x64.ps1, package-layout-probe.ps1 and both Package.appxmanifest files. They match the preceding preparation bindings. No source/test/assertion rewrite, manifest/version mutation or historical receipt replacement occurred.

PowerShell-before.json equals powershell-after.json: **732** complete current installation files under `/workspace/work/maintenance-foundation/package-layout/powershell/`; no file changes. The binary hash is `3f27efac38d345f9a39dbb68c7ca519bcab35884a4f12e36ce0e456f876d275a`; manifest hash is `47a72d2ef3b44514a360ce2ba59c6d649df3099a7744a79b21979d8e27baf6d5`. Actual `$PSVersionTable` metadata is7.7.0/Core/Unix/Debian13, retained in powershell-version-command.json, exit0. That metadata command is distinct from the two fixture invocations. PowerShell's bundled net11.0/runtime11.0.0 installation is separate from the existing product-probe SDK8.0.427/runtime/ref8.0.33.

Host-before.json equals host-after.json: current Linux/platform/Python3.12.14/binary paths and hashes plus limited relevant environment metadata. Current python executable resolves `/opt/codex/runtimes/codex-primary-runtime/dependencies/python/bin/python3.12`, SHA256 `fa67443527ed9647f760d807e2a38f26340757123e643c4639cf273ed15d5ea7`. Five current system libraries reported by `ldd pwsh` are hashed before/after with no changes; raw launch linkage is pwsh-ldd.txt. This covers current installed PowerShell files, launch dependencies and recorded host binaries/metadata, **not** a complete transitive OS/Python-library manifest or any prior Python/OS-byte identity. Old package/sampler receipts still lack historic PowerShell hashes; no retroactive equality is asserted. These new receipts close the current-run provenance concern at the explicitly documented granularity.

## Actual supported outcomes

All46 actual sampler command label/exit pairs match supported-checks.json: **six exit0,40 intended exit1**. Native-host/invalid-input rejections retain their expected nonzero exits and no-output/preservation assertions. Current results.json is byte-identical to the accepted corrected summary; real command records/logs remain new/current because paths and timestamps differ. All21 named controlled groups pass. Revision result has106 PASS/zero failures and the same16 ordered checkpoints; actual settled50/idle60/idle300 ranks1003/1021/1022. Marker instrumentation reports55 actual helper calls and52 accepted synthetic rows. Complete check/group/command records are retained; none are counted as aggregate stages or native samples.

Package log reports EXTRACTED2/BUILD_SCRIPT_PARSE_ONLY=YES, exact build SHA `479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10`, Read-AppxPackageLayout extent `512315f016cfdc5fc11a6d7e922b3d9892fcaccf90aaf6e0bac51ca0c82812f0` and Test-FullPackageLayout extent `05a2f7f00f6f7cb7fb8237565429cc539a3e19180c9b8a330fad617420040698`. Actual15 PASS lines and CASES15/PASSED15/FAILED0 agree. It retains Int64/65535 boundary,65536 rejection, compressed4MiB root, required-entry/manifest and physical-error cases. Zero-root acceptance remains the approved limited predicate, without executable validity claims. Fixture-owned child directories were cleaned by the unchanged probe; its private parent remains.

## Exact retained hashes

| Artifact relative to private scratch | SHA256 |
| --- | --- |
| source-before.json / source-after.json | 21ce4eb9d8424234ad7ad2f0e607385f8b4b52a953bbb9755c27d8d2ae8f50bf |
| powershell-before.json / powershell-after.json | 47a72d2ef3b44514a360ce2ba59c6d649df3099a7744a79b21979d8e27baf6d5 |
| host-before.json / host-after.json | de78247f170c3619458a595c672fd3f669ecd9c5b1d950f889c406a05db7d814 |
| commands.json | 98dee746765a713da0294d42c58552ebe2046d3989cfd1d240dc25c8bd51a42a |
| sampler/results.json | c5cb6161972021782255bb752f04aaf77eed6f74456907ca20a8fb8894f3d7c2 |
| sampler/commands.json | ca09ba458e9b389dc206b1ad4c552216b73067fdbd2b676c2e945efd15377603 |
| sampler/revisions/revision-results.json | 0edcf216812164ea8718e1ece524021379df2b0ec0bc19e3040a02901f45609d |
| sampler-console.log | 59482a1d1905ed8da6f94211941bb68385f5b920c44ba49bd360aa58c77af4cc |
| package-console.log | f37a816bb153b0c341a1ffe4630ddbb06edb9636097f20239b48d24ea1ad03e3 |
| powershell-version-command.json | 1a08a9974b48f89f4414463356dbb0cba4d8a7dd09a78d02573f9832e8f334a3 |
| validation.json | 2bb493b56ae93f552edf1b4a38f2a2c6d19b41ae710d36c1cfa8fcd655065123 |
| stability.json | 6f086cbadb29d38052591c524cfbd14c34327f1b96499e8475f2ccf2056677c9 |
| artifact-hashes.json | 1d1bda4745da541e3c64bf9191c069251f0635129e13c288ce59884c94b45a50 |

artifact-hashes.json binds every private raw fixture/report/source/provenance artifact present at closure. No runner14/full/owning suite, nativePS5.1 sample, real UWP50video experiment, source/index/ref/publication change or whole-tree stability assertion occurred. Current ZIP/model/Mark results establish no signed/installable package, Windows startup, RAM/private-commit/native/GPU trend or BR-21 measurement. All targeted receipt custody is **FROZEN/RELEASED**; await root's separate combined-gate followup.

# Complete aggregate and preservation evidence

Command:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run-all.sh --output /workspace/work/maintenance-foundation/runner-execution/full-final
```

Exit **0**; suite **PASS**, complete_required=true; **50/50 logical and 51/51 executable PASS**, historical baseline/current manifest both 50/51; all FAIL/MISSING/BLOCKED/TIMEOUT/ERROR/CANCELLED/NOT_SELECTED/PENDING counts are zero. Duration **174.255 seconds**. Manifest SHA-256 `3b240e48f78bd7020abcb038eaa37a005a198452c44828f3ae0e0fd80ea676e6`. The observed start HEAD is `0368569ea34c6b15491c256bdab106c25dfe7951`; dirty state is honestly recorded because the disjoint documentation owner and this execution receipt had pending edits. Final source HEAD `f1963e0ed59816188dac487ed30ff8ddddbc7e64` changes only one external control assertion, separately verified by 11/11 controls at that exact SHA. Every aggregate/selector/legacy/product input is byte-identical across these SHAs. Workflow commit `79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf` changes only the new workflow; no complete suite rerun is justified by metadata-only publication.

| Stage | Status | Exit | Seconds |
| --- | --- | --- | --- |
| batch:api02 | PASS | 0 | 11.44 |
| batch:auth1 | PASS | 0 | 3.781 |
| batch:auth2 | PASS | 0 | 3.743 |
| batch:auth3 | PASS | 0 | 4.264 |
| batch:auth4 | PASS | 0 | 3.632 |
| batch:c1 | PASS | 0 | 4.139 |
| batch:c2 | PASS | 0 | 3.878 |
| batch:c3 | PASS | 0 | 3.445 |
| batch:cap | PASS | 0 | 3.313 |
| batch:cardlayout | PASS | 0 | 3.188 |
| batch:cm1 | PASS | 0 | 3.38 |
| batch:comments | PASS | 0 | 3.718 |
| batch:d1 | PASS | 0 | 3.204 |
| batch:d2 | PASS | 0 | 3.695 |
| batch:dirpress | PASS | 0 | 3.197 |
| batch:firstrun | PASS | 0 | 3.212 |
| batch:focus | PASS | 0 | 3.38 |
| batch:g1 | PASS | 0 | 9.734 |
| batch:g2 | PASS | 0 | 3.523 |
| batch:g3 | PASS | 0 | 4.733 |
| batch:g4 | PASS | 0 | 4.25 |
| batch:g5 | PASS | 0 | 9.095 |
| batch:localdir | PASS | 0 | 3.41 |
| batch:ov | PASS | 0 | 3.228 |
| batch:playersess | PASS | 0 | 3.502 |
| batch:playertoolbar | PASS | 0 | 3.144 |
| batch:season1 | PASS | 0 | 3.574 |
| batch:settings1 | PASS | 0 | 3.389 |
| batch:smart1 | PASS | 0 | 3.526 |
| batch:stab | PASS | 0 | 0.251 |
| batch:toastalign | PASS | 0 | 3.128 |
| batch:ux0 | PASS | 0 | 3.113 |
| batch:ux1a | PASS | 0 | 3.114 |
| batch:ux1b | PASS | 0 | 3.229 |
| batch:ux1c | PASS | 0 | 3.444 |
| batch:ux1d | PASS | 0 | 3.363 |
| batch:ux2a | PASS | 0 | 3.554 |
| batch:ux2b | PASS | 0 | 3.334 |
| batch:ux3 | PASS | 0 | 3.414 |
| batch:ux4 | PASS | 0 | 3.409 |
| batch:ux5 | PASS | 0 | 3.133 |
| batch:uxfix | PASS | 0 | 3.596 |
| batch:webrefresh | PASS | 0 | 3.711 |
| LM01 | PASS | 0 | 0.69 |
| LM02 | PASS | 0 | 0.728 |
| LM03 | PASS | 0 | 0.759 |
| LM04 | PASS | 0 | 0.688 |
| LM05 | PASS | 0 | 0.752 |
| API01 | PASS | 0 | 1.006 |
| AUTH01B.account | PASS | 0 | 0.819 |
| AUTH01B.loginvm | PASS | 0 | 0.968 |

Comments remains exactly one stage/program, including `DIALOG_CHECKS=153 FAILURES=0` and `PASS 1070 assertions`. 22/22 released source/test inputs and the actual generated wrapper hash remain identical to the approved binding. Package-layout is separately NOT_CONFIGURED, reason `separately-owned, excluded`, and not invoked.

Toolchain: actual `/workspace/work/maintenance-foundation/quality/dotnet/dotnet --version` = `8.0.427`, runtime list includes `Microsoft.NETCore.App 8.0.33`; exact compiler/Newtonsoft/reference/runtime inputs validated. PATH and caches use the declared tools root. No install/roll-forward/source fetch/manifest mutation was used by the aggregate.

The earlier initial complete run also passed all 50/51 in 229.698 seconds; it is retained as `full-v1`, not substituted for the final runner run. Unchanged no-argument `bash plan/tests/run.sh` exited 0 (`default-v1.log`) and executed LM01–LM05/API01/Account/LoginVM in original order. Existing output includes LM05=22, API01=76, Account=70, LoginVM=39 actual checks. Scratch prefix failures and suffix absence are covered separately by the permanent external controls. All seven compiler command lists and both AUTH extraction blocks compare byte-for-byte with prechange scripts (`preservation.json`).

Final relevant source/test hash manifest: **869 existing files**, scratch `final-source-hashes.json`, SHA-256 `0e1142c5452d9c91e968507f94069aa045c312924b50b13209717e29632e5779`; every listed current input is rechecked unchanged. The manifest contains hashes, not copied source, and is retained outside uploaded diagnostics. Python compilation, Bash syntax, owned diff whitespace, YAML BaseLoader parse and combined CI shell-body syntax checks passed. Full report/log tree is below 60 MiB; every combined stage log is <=1 MiB. Contract tree is 1,771,375 bytes at the earlier exact-source control receipt; final control tree has the same bounded form. No bin/obj/source/cache/real account data is retained under report/upload roots.

Windows UWP/XAML compilation, MSIX inspection/install, real startup/playback/account/device/session validation remain **BLOCKED**. Offline success and hosted CI cannot satisfy those platform gates.

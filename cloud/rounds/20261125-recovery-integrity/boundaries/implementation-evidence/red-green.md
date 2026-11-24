# BND-B/C/D implementation evidence

Status: **TARGETED PASS, source publication pending**. Exact authorized scope: execution-dispatch.md and root-granted boundaries/commit-window.md. BND-A FavoriteDetailVM/c2, other executors, aggregate tooling, main documentation, protected files and UWP project remain excluded.

## Behavior and actual-source seams

- Four Int32 payload readers reuse the existing strict bounded integer reader and preserve exact Payload stages/defaults. Legacy feed/reposts range-check Int64 integers; typed page failure remains distinguishable from public ReadHasMore's false default. Bool/nonzero feed and integer-equals-one repost contracts remain distinct.
- Season normalizes away null sections and skips null episode/optional metadata dereferences. Comment skips null top/reply/subreply rows in mapping; DTO arrays, raw ReplyCount and source cursor remain unchanged. Non-null wrong DTO shapes still reject as Payload.
- Six video actions capture initiating owner/targets before login and reject retired owners before sending or publishing. Reference identity retires same-aid replacement too. Favorite supplied IDs are snapshotted, supplied avid remains the request target, and mismatched visible avid cannot publish. LoadFavorite independently owns its list publication. Public standalone AttentionUP keeps its signature and no-detail behavior through a private optional-owner overload.
- c1 now compiles the **full actual VideoDetailVM**, IModules/RelayCommand/ApiModel/ReturnModel/Common and its existing parser/model closure. Request delivery, login, logging and display-model properties are primitive stubs; no stub decides acceptance/publication/paging. All existing source-wiring checks remain. g4 executes the actual Home DynamicVM and retains its existing user-space source checks. season1 executes the actual SeasonDetailVM; comments retains its actual dialog/decoder/builder checks.
- c1's unique-marker extraction copies the complete actual HttpResults tail **byte-for-byte**, including the namespace closure and production salvage. Generated file is under ignored obj/probe-generated and provenance includes source/body SHA256. The obsolete build/**/*.cs compile path is explicitly removed. final-green contains an intentionally invalid `build/DetailModels.cs` sentinel: c1 passes despite that stale file; no extracted model source is compiled alongside the full VM.

## Exact reproduction and final outcomes

All snapshots are under `/workspace/work/recovery-integrity/boundary-execution/`. Product baseline is git077ef35ae194d3ae85aa0621daeac07dc29e29ab. final-red contains unchanged baseline product plus the **same final eight test/harness files** as final-green. final-green differs in precisely the seven owned product files; its scratch outputs are independent of shared gRPC builds. `run.py` freezes every non-generated src/plan/tests input into phase-hashes.json before running commands serially.

```bash
python3 /workspace/work/recovery-integrity/boundary-execution/run.py final-red c1 season1 comments g4
python3 /workspace/work/recovery-integrity/boundary-execution/run.py final-green c1 season1 comments g4 g3 g5
```

The driver directly executes `bash plan/tests/<batch>/run.sh` from that phase snapshot, with PATH/DOTNET_ROOT/UPSTREAM_UNIT_TOOLS pointing to `/workspace/work/maintenance-foundation/quality[/dotnet]`, NUGET_PACKAGES to its cache/nuget, and private DOTNET_CLI_HOME in this scratch root. Exact SDK8.0.427/runtime/ref8.0.33 is reused, without upgrading tools. No extra physical boundary stage/program or hidden second invocation was added.

| Existing owning batch | Unchanged-product final red | Fixed-product final green |
| --- | --- | --- |
| c1 | exit134; all186 original assertions pass, new97 pass/42 fail | exit0; original186 + new139 pass, zero failures |
| season1 | exit134; originals reach completion, new null section causes actual parser NullReferenceException | exit0,141 assertions |
| comments | exit134; originals/dialog153 pass, new null top reply causes actual mapper NullReferenceException | exit0,1089 assertions; dialog153/failures0 retained |
| g4 | exit134; originals reach completion, new beyond-Int64 has_more causes actual InvalidCastException | exit0,179 assertions |
| g3 shared | not a new red target | exit0,386 assertions plus existing29/105 rich-text assertions |
| g5 shared | not a new red target | exit0,335 assertions |

c1's42 genuine red outcomes comprise16 scalar exceptions, one optional-read sequence failure, twelve stale action publications (B and same-aid replacement), two stale favorite refresh initiations, six login-retirement requests, three changed login argument snapshots, stale list publication and mismatched-avid publication. Success **and rejection** are gated deterministically for all six actions. Async-void completion is tracked through SynchronizationContext OperationStarted/Completed; WaitAsync is a bounded deadlock deadline, with no sleep-based correctness assertion. Current-owner success/rejection/undo/count effects, login inputs, standalone attention and salvage controls pass. Parser-only silence checks run before all VM action effects.

Null-row acceptance covers all eight discovery cases, all-null pages, mixed valid-neighbor order/metadata, section-first duplicate winner and source classification, actual season VM publication, comment first/later-page pin/dedup behavior, raw count/cursor retention and malformed non-null rejection. Legacy tests exercise the real Home caller: failed append preserves rows and the last card-derived cursor, and records the ordinary MalformedResponse source outcome. Original reviewed discovery evidence remains independent proof of all57 failures/46 controls; final owning-stage reds above use the exact final fixture bytes.

## Corrected new fixture setup, retained attempts

Initial baseline/green attempts and their logs remain in scratch, never overwritten by final-red/final-green. Two **new** expectations were corrected to the reviewed existing contract, without changing product behavior or original assertions: SeasonDetailVM prefers a successful whole-detail response over ep/list metadata, so the new VM case now makes the optional whole-detail read unavailable and actually exercises the ep/list mapped fallback; Home DynamicVM's legacy cursor is the last card's desc.dynamic_id (`70001`), not response data.offset. The parser test separately preserves the exact large response offset string. Final-red reruns all corrected test bytes against unchanged product and remains genuinely red; those initial invalid expectations are not counted as defects.

Every original method body/assertion suffix in c1/g4/season1/comments Program.cs is unchanged after line-ending normalization compared with git077ef35. `assertion-preservation.json` records the four exact original suffix hashes. No original assertion was deleted, replaced or weakened.

## Input/log hashes

| Owned product path | final-red SHA256 | final-green SHA256 |
| --- | --- | --- |
| `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs` | `66de7eb0ef0d1ed2fd0e87dca4558f2784ac78853448d7430b2f454068efd50f` | `83d3927017ecb67becbebdad1a075199f4065f969849e3c47b19ee4ea3a3cd08` |
| `src/BiliLite.UWP/Parsing/Video/WebVideoStateParser.cs` | `469b46e1e03b5620947c7355808f4200229e8327134a14774ad1f233362cb6cc` | `83e9ea9ce86331fb1ed477ea2f6c9f3940cf59fdc6cd76f201402f5f23e7eed1` |
| `src/BiliLite.UWP/Parsing/Video/WebRelationParser.cs` | `fa7d4ff8b2a7b1d66ff14267c7c8cb51f0230a49f34edff0d62b5c17ad404a54` | `d5c37d01f7b0df48a69ac6f19546eae9c584593c4ee1b7d13d00e70335ec4cc7` |
| `src/BiliLite.UWP/Parsing/Dynamic/LegacyDynamicFeedParser.cs` | `ca41be3cfe13dfb1f4ef7423ae7de7b3359bfeefe9c23fb9f4481e94bd602e0b` | `d340999de9abb5bd44852492b0afec4495b3381386fef7edb5314b79da0a5705` |
| `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListParser.cs` | `239a221228518294df53318d0c6a20c4184eaf4c5f230d4d4e79017af15fce72` | `9c4f55fd851fd7000c624df21832328fe2237e431b1c658a6f3505fbdb71d8e9` |
| `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListMapper.cs` | `5ab4b114904d875b2a9115c6fb401fe12acccfbf842fc6004eb7c941c14e86ca` | `1d63a7d6c18dce72acfa300998560a6e1986c2033f9756c3a6cd6a57f70a620f` |
| `src/BiliLite.UWP/Parsing/Comment/WebCommentPageMapper.cs` | `78196b8402677eb0080616d6d4d3e8ec132fc39ab2ff67a85d01c2f2c6fcf714` | `fb770bf57fcb2c575494f3c4d91221dd98dfa27d7400ce01ff6614d93569c5c2` |

| Final test/harness path (identical in both phases) | SHA256 |
| --- | --- |
| `plan/tests/c1/C1Probe.csproj` | `c9e63beef78b5597cc183305c80ff9d9c26117192748a96361c43e1b316c67f5` |
| `plan/tests/c1/C1Stubs.cs` | `3c1c680ef8bb55a4540059397a9e93fe53c0638f04b42f7671a7499835f30399` |
| `plan/tests/c1/Program.cs` | `35feae7879caef8985ce044db1555a808ff1394cd96dc9e83fcc5a0a55f92480` |
| `plan/tests/c1/run.sh` | `e954176d89bb9dbd2fba0ada416b164bd88568ae8d9cc3c5eb2234c7d87fe5cf` |
| `plan/tests/c1/BoundaryChecks.cs` | `90c9fd8da8db110f11467e259164620d8257183f7d33aa0d8c494681e6eba8dd` |
| `plan/tests/g4/Program.cs` | `78fef3971086a29dd96ef6fee9c658e1d0e51a34d08c41acc5f6573bc094da05` |
| `plan/tests/season1/Program.cs` | `60dcb21036fccd435c603d3cb8338c7eeed5acf4998591ccc7571c0abcd4f772` |
| `plan/tests/comments/Program.cs` | `81267281ea538cda600858ec4c71a7da999639d0e705386a32d143cf0f4dbd38` |

| Scratch evidence file | SHA256 |
| --- | --- |
| `final-red-hashes.json` | `069ad6260358f6081b9518657a490acc5ffebf8f9d93d8397af094ae3c947d74` |
| `final-green-hashes.json` | `1397fb1d704c82171867fe94c7a08590773119f6b33b49fec251e63daf85c03d` |
| `assertion-preservation.json` | `08d7d9b05043c01e5698414ec46c0bb1438db41e8b9b9ba421db662bc86db9da` |
| `final-red-c1.log` | `8757d87fd42dbf9092382e72d97b3b6bfa1a2b3d68e3510cfa57f6437c3fc611` |
| `final-red-season1.log` | `80c018bfbde97937163c5910f216bae441f4eb9f765103406bab826d03b94dfa` |
| `final-red-comments.log` | `bba18dd352ad3f4aa4069b862b1eb93575012fa9d0a13cd990f3128dbe347624` |
| `final-red-g4.log` | `6eab49f9c4927c8e1a9f28934fdaedcbed5390bdff67af46a2726d22d000a30c` |
| `final-green-c1.log` | `df15ffe8bac937b82180caf7ceeece1a817ff92878c96db62d223bec875922e8` |
| `final-green-season1.log` | `df76cbb76d8b1ba82e6da5cc957b5df02718160664261c6f3f2631ffb7eec59c` |
| `final-green-comments.log` | `e4b856e45712a0dcb7f02a1a28882b2ef315323750dede385ec5a6b59f56da71` |
| `final-green-g4.log` | `d6259b3006965ba77bfcb3ce10ee798bc527588e3d8548b7fc6c7f0807bcdb6b` |
| `final-green-g3.log` | `ab7a9d6ddc1066b817ff570576881038a72334e49b1768f14a6e028a68249b8d` |
| `final-green-g5.log` | `bcc27d34f761194c7ca5e0ab1e24f3958e9f9fdd0dc2077a6cc1acb5d6772a77` |

Decoder extraction provenance:

```json
{
  "source": "src/BiliLite.UWP/Helpers/HttpHelper.cs",
  "source_sha256": "b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13",
  "body_sha256": "7757ba101e2e8ba3aaaa51ffa283456dae6f393226cd3baa2872dd58a1068917"
}
```

All test processes completed; all fake gates completed and no probe processes remain. No account/service request, device/native rendering, Windows build, XAML/BackgroundTransfer/session behavior or hosted CI PASS is inferred. Targeted results are partial, not the final current51 logical/52 executable common gate. Next native actions: UWP compile; switch video/playlist entries during each pending action/login; exercise valid/rejected actions and partial season/comment pages. Main-document reconciliation and final integrated gate remain assigned elsewhere.

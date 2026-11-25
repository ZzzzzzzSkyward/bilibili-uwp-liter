# AF2 evidence — refreshed created folders remain stale in binding

Status: **CONFIRMED bounded P1 / requirements ready**, 2026-11-25 UTC. Authorized only by [af2-discovery-dispatch.md](../af2-discovery-dispatch.md). Repository writes are this file and [af2-requirements.md](af2-requirements.md); scratch writes are exclusively /workspace/work/api-shape-comparison/contracts/af2/. Main product/tests/project/stubs/tools/docs/index/refs and all earlier requirements/proofs were not changed.

## Actual source/caller observations

| Path | Evidence |
| --- | --- |
| Modules/User/MyFollowVideoVM.cs:31–50 | The MyFavorite getter resets stack_will_overflow; setter assigns _myFavorite=value before testing _myFavorite!=value. After inited_favorite=true from first event, later changes emit no MyFavorite notification. Both flags are private and referenced only here; they do not own requests, parser or lifecycle state. |
| Same VM:67–117 | Complete LoadFavorite requests FavoriteAPI.MyFavorite, decodes actual HttpResults→ApiDataModel<JObject>, deserializes created folders, assigns MyFavorite, inserts default_folder.folder_detail at0, applies created has_more/Page, then independently assigns CollectFavorite. The latter setter already notifies. |
| Same VM:122–158,190–203 | LoadCreateList appends to the same MyFavorite collection via Add and uses existing numbered Page; Refresh's Loading guard then null resets and real LoadFavorite are separate existing command behavior. No paging modification is needed for notification repair. |
| Pages/User/FavoritePage.xaml:80,107 | Created-folder AdaptiveGridView.ItemsSource is x:Bind videoVM.MyFavorite, Mode=TwoWay, with drag/reorder bindings; visible refresh uses videoVM.RefreshCommand. |
| FavoritePage.xaml.cs:60–76,122–145 | Initial selected favorite tab awaits videoVM.LoadFavorite. Create/edit completion and delete completion invoke videoVM.Refresh. This is the same method bound by RefreshCommand. The dialogs themselves and live writes were not executed. |
| FavoritePage.xaml.cs:147–174 | Drag completion uses SortMyFavorite; later temporary binding-switch code follows return and is inactive. Existing TwoWay/reorder behavior remains; property identity repair requires no XAML edit. |
| Api/User/FavoriteAPI.cs:17–37; IModules.cs:31–35 | Actual signed-app-family folder space and created/list request builders remain; real inherited PropertyChanged synchronously notifies. No route/auth/shared-base change is justified. |

The fixture's space_infos/mediaListResponse/default_folder shape comes from the actual legacy caller. It is deterministic local caller-shape evidence, not a live Bilibili capture, supported-route certification or sibling migration requirement. Default-folder and source order are asserted separately from the missing replacement signal.

## Complete actual-source offline reproduction

Own af2/snapshot includes byte-copied full current MyFollowVideoVM, FavoriteAPI, IModules/RelayCommand, wire models and every c2-linked product/stub input. Complete HttpResults is extracted from current HttpHelper using the same existing fail-closed final-class boundary. The exact complete Utils.DeserializeJson<T> method is extracted from Utils.cs; only namespace/usings/partial-class wrappers adapt it to the portable harness. In own copied C2Stubs the sole helper wrapper change is static Utils→static partial Utils, so the byte-exact product helper can join it. No notification, parser, default-folder or paging algorithm is copied into a stub. C2's existing delivery/platform/signing primitives remain; the signing recorder demonstrates chosen request fields, not cryptographic/server acceptance.

The new AF2Probe observes the actual MyFavorite property event; its listener changes retained collection only on that event, attaches CollectionChanged and asserts retained folder IDs. A TaskCompletionSource boundary delays the real refresh response; actual Loading=false notification completes the async-void command with a5-second bound. Thus refresh clearing and accepted replacement are checked separately. A separate real LoadCreateList path proves same-collection append remains active.

Command: bash /workspace/work/api-shape-comparison/contracts/af2/run.sh

Result: **exit1;11 positive assertions PASS, exactly2 desired-safe assertions FAIL**. Initial event/default Insert correctly display default-initial/folder-initial. Actual RefreshCommand nulls the model and starts its second request, but listener does not clear. The complete real decoder/helper/VM then accepts default-updated/folder-updated while listener remains at signals=1/view=folder-initial. CollectFavorite independently emits its existing reset/replacement events and receives updated collected folders; HasMore/Loading and actual request route/identity fields pass. Append positive retains one bound collection and emits one add for the new string ID9007199254740993 at actual created/list Page2. The11 positives show a live notification/collection/delivery boundary, not a dummy no-op harness.

Private original-red.log preserves the entire result. The fixture runs actual Request delivery, full GetJson decoder and Task.Run JsonConvert helper; no model-only assertion or source substring check substitutes for the binding journey. No foreign code, Bilibili request or main-tree probe execution occurred. Existing SDK8.0.427/runtime8.0.33/Newtonsoft13.0.3 were reused read-only.

## Closing acceptance and custody

The AF2 requirement defines the accepted AF1-style identity setter/pure getter/null/echo table and a strictly named companion source/test surface. Preserve every original positive and the two desired-safe failures unchanged in the final fixture; extend meaningful property/command/failure/retry controls before running the same final fixture on frozen original and repaired complete sources. Preserve the CollectFavorite setter and every endpoint/parser/signature/action/accepted-value rule, with AS1–AS3/AF1 proofs untouched. The later independently evidenced amendment permits only current required response decoding before publication inside LoadFavorite/LoadCreateList; it supersedes the original property-only byte-preservation proposal for those two methods. Requirements do not claim repair-green, native XAML/dialog/reorder, server schema, account/write, cookie/session or liveU2 acceptance.

All AF2 artifacts/snapshot/fixture/results read custody is **RELEASED** for independent review after binding receipt completion. Original AF2 inputs and logs are frozen; all other owners' source/audit custody remains theirs. Root must wait for independent PASS and all affected reader releases before any exact sole-writer code grant.

## Frozen binding receipt

Private /workspace/work/api-shape-comparison/contracts/af2/receipt.json binds every compiled/runnable input, complete source snapshots, helper/decoder verification, exact sole stub declaration adaptation, SDK compiler/runtime and Newtonsoft DLL bytes. All source-manifest entries and current MyFollowVideoVM equality were rechecked after execution; wrapper bodies exactly equal the complete product method/class regions. Input snapshots and the result are now frozen.

| Binding | SHA-256 |
| --- | --- |
| Complete pre-AF2 MyFollowVideoVM | 98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe |
| AF2Probe.cs (13 assertions) | dbe4ccfc7069a3bb0e70aef8ce6f31380abffe814913549848793990bece5600 |
| Full receipt.json | e7408ffb075dbd04c493b3799d711d06962ba83ef702e2d52c337dbaaf8c8fed |
| original-red.log | 70e2f715173bb72ffb93480095833a81aa9b15d8899bf1930f37d120c4b5a7ad |

AF2 artifacts and all own private input/result read custody are **RELEASED** for independent requirements review. No repair-green or new live/native/session acceptance is claimed.

## Independent review correction and safe-response staging proof

The sole P1 failure-retention contradiction is addressed by [af2-amendment.md](af2-amendment.md) and the authoritative revised requirements phase/state table, pending same-reviewer PASS. Root explicitly selected current response validation before reference/default/row publication inside LoadFavorite/LoadCreateList, preserving exact decoder/helper/casts/accepted values/optional branches and existing successful event order. Direct load retains accepted M/C on any precommit response failure; Refresh retains its prior null reset; append retains rows/Page until list and flag validation succeed. This intentionally repairs late metadata partial publication without rollback, dedup or a general subscriber transaction.

Raw reviewer seven unsafe-behavior controls/log remain byte-identical discovery history in exclusive af2/revision-history/. Original13 binding assertions/logs/source/tool receipts remain unchanged. New complete-source af2/revision-safe/AF2SafeBoundaryProbe runs direct and actual settled Refresh across missing-default/invalid-flag/collected-decode errors and append/same-page retry: **exit1,17PASS/20 desired-safe FAIL across37 checks** on the frozen original VM. Exact paths/hashes, full-source/helper/decoder/tool graph and results are bound by private af2/revision-receipt.json and listed in af2-amendment.md. No repaired-green, live schema or native/session pass is claimed. All amended artifacts and own scratch read custody are released for same-reviewer re-review; no main code/test/project/stub/script/docs/Git edit occurred.

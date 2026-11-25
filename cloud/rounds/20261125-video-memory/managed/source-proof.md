# Source-slice proof binding and reproduction

This is an offline .NET source mechanism experiment. It has **no** decoded video frames, native XAML cache, actual watching duration, Windows process footprint, network or GPU evidence. The pending-close race closes before detail loading returns; it does not reproduce the user's already-watched 50-video journey. All product files are read-only.

## Reproduce from the current exact baseline

Scratch root: `/workspace/work/video-memory/managed/`. There is no NuGet restore or live request.

```bash
python3 /workspace/work/video-memory/managed/prepare-proof.py
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/maintenance-foundation/quality/dotnet/sdk/8.0.427/Roslyn/bincore/csc.dll @/workspace/work/video-memory/managed/compile.rsp
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/video-memory/managed/ManagedMemoryProof.dll
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/video-memory/managed/ManagedMemoryProof.dll --require-safe-season
```

Last two exits: **0** (30 descriptive assertions), **1** (desired closed-season safety assertion fails). Compiler uses `-optimize+ -nostdlib+ -nullable:disable`, references every .NET8.0.33 ref assembly through the response file, and runtimeconfig requests Microsoft.NETCore.App8.0.33. No repository test was changed or newly run; selected existing tests are a future execution gate. `compile-first.log` preserves one harness bring-up error for a missing stub `VideoDetailRelatesModel`; the stub was supplied without changing any extracted product body. Final compile has no errors and 5 harmless unused fixture variable/event warnings.

## Exact compiled product slices

Anchors must occur exactly once; extraction fails if they do not. Complete brace-delimited methods/classes are retained without product-text replacements. The sole explicit prefix comparison is marked below; generated helpers/stubs are outside these regions. Full source/extraction hashes and ranges are in scratch `extraction-manifest.json` (UTF-8 body text hashes; whole-source hashes retain original bytes/BOM). Current HEAD7356b94893377558191b0d70576a52bae704c1f9.

| Product source | Lines | Exact anchor | Extracted body SHA256 |
| --- | --- | --- | --- |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 119–126 | `private void SubscribeClosedPage()` | `db807273253f8c8a7f178512ceb50179404d3f810d80215c1eccdd9e59e7cb0a` |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 128–134 | `private void UnsubscribeClosedPage()` | `fb591c8f3ae595780be05101c22b205e27cee7b253060498c1047404e85241e9` |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 136–141 | `private void SubscribeShare()` | `077e519c351912007be80fb6f35e3f031d1f3ab4b5d172fc372070fa6a2a83d2` |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 143–146 | `private void ReleaseShare()` | `00edc11878b717cb2ba5146895590c0da1ee74a8264e690e2f6751c1e7f8ce87` |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 153–181 | `private void ClosePage()` | `e31d0e742fb065771ac9c81cfc4a30ea91f9ef62ce915d0241fb0d9be046ffdd` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 56–63 | `private void SubscribeClosedPage()` | `7371621e44e3e0deef10f48b8cb95c4b11905dfd0c77c6373791611b4b4922b1` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 65–71 | `private void UnsubscribeClosedPage()` | `5e4c52a1a599b9aadc86f4d4ac24c5b0008a80b6e5ccbab554e845e17b99ad7f` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 73–78 | `private void SubscribeShare()` | `077e519c351912007be80fb6f35e3f031d1f3ab4b5d172fc372070fa6a2a83d2` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 80–83 | `private void ReleaseShare()` | `00edc11878b717cb2ba5146895590c0da1ee74a8264e690e2f6751c1e7f8ce87` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 89–106 | `private void ClosePage()` | `81d2d26ff81c9795aef5e6edeb9ad6d62aa260c788c0a7c257eaeaad683d95ff` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 158–173 | `private async Task InitSeasonDetail()` | `635434bab6f02f2728a82f584ea9f84b838ec3286bd594a04fc649714b6c9325` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 175–205 | `private void InitializePlayInfo()` | `474cff18b342c4193e23840e8b735887180f2f83c5714a32609a6f4e0f516357` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 206–236 | `private void UpdatePlayInfoToEpisode(int index)` | `bbb76d5f587861ac6fb20d790e9cb56913b376bd4bfdea22551cd6fa5d182f72` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 237–267 | `private void UpdatePlayInfoToPreview(int index)` | `0676af27083b95a2aee99a40e0c1e3d6d10b61d99ab4b213e4c54408cffe5fb5` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | 277–291 | `public void ChangeTitle(string title)` | `9a7f32933102f6940b2d630e7e7dcad1e35e285a3bf28137450e11ebab9cb717` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | 1066–1076 | `public void InitializePlayInfo(List<PlayInfo> playInfos, int index, PlayTargetStart start)` | `d1669e2ad33ad853ef1d00f9003f5833124af726980645626170a97dc771502c` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | 3920–3959 | `public async void Dispose()` | `cab0d6b1d78144db8092c481d649ec0441058e46349421d838681a2272178edd` |
| `src/BiliLite.UWP/MainPage.xaml.cs` | 191–202 | `private void ClosePage(TabViewItem tabItem)` | `594292442864c695191320a5c0b3066f441de843d8f5b45f5f705ad751c6ec7a` |
| `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs` | 151–221 | `public async Task LoadVideoDetail(string id,bool isbvid=false)` | `c980d29fbf7b35e33599e7b45ef16c5fc1581708bc058bcde9c5f2d8ec0b6b91` |
| `src/BiliLite.UWP/Controls/MyFrame.cs` | 6–14 | `public class MyFrame:Frame` | `e5fa6949d3e8ed75fb2ab9cd6bf123bfca9dbb0596907f4b34bd41e0576e531f` |
| `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` | 326–445 | `private async Task<bool> InitializeVideo(string id, PlayTargetStart start)` **PREFIX ONLY** | `db1239a4f63f5fc7f4fe126d6869c83ec4ccaed264a93da0eec150befc1700ec` |
| `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs` | 71–78 | `private void SubscribeClosedPage()` | `2a41fe3a022f35ed43619eeabac51ef24612f3b112710ac38495a26a6afc91c2` |
| `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs` | 80–86 | `private void UnsubscribeClosedPage()` | `b9f87205aade4379ba9320ed7687440819b2c96c13603606cd98f3f8266e8088` |
| `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs` | 88–96 | `private void LocalPlayerPage_ClosedPage(object sender, EventArgs e)` | `7f56081324d716fa82cc56bea5d89b9bc6ad59d01bed67a23344b15ba9965446` |
| `src/BiliLite.UWP/NoTabMainPage.xaml.cs` | 189–289 | `public class NewInstanceFrame : Grid` | `f44348cc6aa2b1fb538582544a0918e9edf18438186a2cecf14cd65ddf7fe32b` |

For Video `InitializeVideo`, only the source prefix through the immediate `_isClosed / _id / transition` post-await guard is compiled. Its accepted path ends with a fixture `return true` plus finally clearing a flag; the UI/playback suffix is absent. This comparison proves rejection before that suffix, not complete video-page runtime behavior. The season page methods, season ChangeTitle and PlayerControl Initialize/Dispose methods are complete extracted bodies.

## Doubles and root controls

- Frame/Page/TabView/Grid are simple managed ownership containers. The Frame double stores/restores page objects for Back and clears forward entries; it has no UWP cache eviction, Loaded/Unloaded, COM peer or dispatcher behavior. NewInstanceFrame's **product** Navigate/GoBack/ContainsPageType methods run atop that double; assertions concern frame topology only.
- DataTransferManager is one intentionally static view publisher with the real subscribe/release method slices. Omitted close notifications leave this event as the sole intended external page root. The shell also stays statically rooted for all tests.
- SeasonVM is a deliberately held TaskCompletionSource and minimal Detail/Episodes publication double. The page mechanism executable keeps this minimal SeasonVM double. The separate `real-season-vm` executable below directly compiles the full real SeasonDetailVM and parse closure, so VM publication is independently established. Full extracted VideoDetailVM.LoadVideoDetail runs with API/parser/mapper/auxiliary-step doubles; no parser completeness or real timeout conclusion follows.
- FakePlayer compiles the **complete actual** InitializePlayInfo and Dispose bodies. Its DisplayRequest and media/helper/timer/resource objects are harmless managed doubles. Dispose clears dispRequest exactly as product source does; the late season Initialize dereferences it exactly as product source does. The source fields/events are present, but native disposal, history networking, timers and playback are not measured. The active delayed season initialization succeeds once, proving the setup is capable of accepted ownership.
- Video close's transition/queue/sequence/UI helpers are no-ops in this executable, except `_isClosed` and share/close subscriptions which are actual source. Their product cleanup methods were separately inspected; no queue cleanup claim is derived from the no-op stubs.
- Creators, close/drain helpers and reachability checks are non-inlined. Strong page locals exist only inside methods that return before collection. Pending tasks/gates are intentionally rooted controls; all are cleared after deterministic completion before collection. Only WeakReference objects remain in the counting frame. Test-only GC never appears in product. On completion the harness observes source side effects before dropping locals, then performs reachability checks outside that observation method.

## Whole-source hashes for compiled regions

- `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs`: `a057e1a6bde0b29f62809e51bf7f691c9ed28a56865cd391b7982a091ef118e3`
- `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs`: `b44706b9156259cc57acda3611023b4a9e0ed96f9345b3576ddaac3148804490`
- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`: `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`
- `src/BiliLite.UWP/MainPage.xaml.cs`: `7abe52a5547d2c6764c88a81da70648b72bafa152d09d9d9109eb339a039858c`
- `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs`: `83d3927017ecb67becbebdad1a075199f4065f969849e3c47b19ee4ea3a3cd08`
- `src/BiliLite.UWP/Controls/MyFrame.cs`: `38b392714f064a67886be401d6ec3dbc6fc38d891ec36ed4c215ed30a9554914`
- `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs`: `3786a92bde1909cf753162231db16a58618c41fe6eba9711c50e1a6b1853ea97`
- `src/BiliLite.UWP/NoTabMainPage.xaml.cs`: `cda4b43db019f323687ea68f00bbef01538ee4ed7371daea2843b340f5a94577`

## Fixture and result hashes

- `prepare-proof.py`: `b50e83c90b7e20c5ae87315faa0d4e1b151fdd44079abdd7ce752f858bc5fd23`
- `ProductSlices.cs`: `e1ae5bbe28494d8d380d07dcff7589b1a8b060658a66573f7d0189f70e055ac4`
- `Program.cs`: `bf4415a1c50457307785cdb95bdc2b7754872731f574888b90cd115fc76861fd`
- `compile.rsp`: `b2556df4b896556969960cc128555ec0621e39ea59dff11d577ac62d84edb79c`
- `ManagedMemoryProof.runtimeconfig.json`: `79f19d137bb946d3dbd60fa65151cecd95529a106390b93423a159a06e096be3`
- `ManagedMemoryProof.dll`: `abe19995010135cc45ae0286d3bf6c040f3a220c0e2a4315d055b7bb4cf2a2aa`
- `compile.log`: `e1bb0b68861d6a8f18f13a5b7bd1690308b03b58eb7e7854cf9c7c89f16d1ff1`
- `proof.log`: `410221e317090121e89288fa873dfb05d29ffe15455f12d5bcf5bb23284c8ed9`
- `red.log`: `90d6450ad74ed97a6b6fd3d612fc121eb44029b7eec4aa8fc1bb81ff901a208d`
- `run-status.json`: `88fa6ef404b77579bf305f0aea437b30957e8aafe01e59a7033a8b0f4657f869`
- `extraction-manifest.json`: `8726626c25e1a8d4b342bd3bd3956939f67e637ceb6640d9a464646fc33b8af9`
- `environment.log`: `f293a4429d8b5bfd219f0dd08b948af7e3d02bc8aa8e85355455f138f2596ffe`

## Full real Season VM confirmation

Scratch `/workspace/work/video-memory/managed/real-season-vm/` directly compiles current product source paths listed in `source-hashes.json` (no extraction): ApiParseResult, ApiEnvelope, WebSeasonEpListParser/Mapper, WebSeasonEpListDto, ReturnModel, IModules, RelayCommand and SeasonDetailVM. Its SeasonStubs.cs is the existing season1 transport/services stubs with an AsyncFixture hook; its Program supplies deterministic metadata/episode response gates and one valid episode body. No product file or repository test is copied back or edited. The gate continuations run synchronously on controlled release, avoiding concurrent mutation of the stub's request list. Normal real parsing/mapping runs.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/maintenance-foundation/quality/dotnet/sdk/8.0.427/Roslyn/bincore/csc.dll @/workspace/work/video-memory/managed/real-season-vm/compile.rsp
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/video-memory/managed/real-season-vm/RealSeasonVMProof.dll
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/video-memory/managed/real-season-vm/RealSeasonVMProof.dll --require-safe-vm
```

Compile0, descriptive run0 (5 assertions), safe-VM red run1. An active real VM publishes one episode. Fifty VMs closed via the product page's Loaded/Loading/Detail assignments republish50/50 after a held metadata response; fifty held at episode responses also republish50/50. Each scenario makes100 endpoint calls. A reflection call invokes proposed InvalidateLoad if present; it is absent in this baseline. The same fixture can exercise that API when it is implemented; it must not be used to claim baseline cancellation exists. This executable deliberately holds VMs in a list for observation; its counts are publication counts, not a memory/root test. Only its transport/Windows app services are doubled, as in existing season1. Cached Newtonsoft.Json13.0.3 netstandard2.0 is referenced/copied, matching the declared test package version; its initial SDK-Newtonsoft compile was superseded before final evidence. Two unused RelayCommand events are the final compile's only warnings.

- `real-season-vm/Program.cs`: `57ef778573d43d6599c8a16d0e046833993a4a86910c8907c21b2730c1a2c5b6`
- `real-season-vm/SeasonStubs.cs`: `61f1ad2ddec110e3c5e532bf91c7cc3295e64381ee1ceac620354e6f9fb975ea`
- `real-season-vm/compile.rsp`: `c708dc3d8184030c9adb9c735849992910cf89501fd9d07b27a6f434b3edf536`
- `real-season-vm/RealSeasonVMProof.runtimeconfig.json`: `79f19d137bb946d3dbd60fa65151cecd95529a106390b93423a159a06e096be3`
- `real-season-vm/RealSeasonVMProof.dll`: `1816c326017ec368f3bcc0566e83efcf78ed2b7795d32cebb64d717ee3c0fb22`
- `real-season-vm/Newtonsoft.Json.dll`: `8c1dd5c184b4e2e7ead06971ff3ebcb46783be972292d1deb1061744369b4d80`
- `real-season-vm/compile.log`: `a59035c9c03a14e800fd13c8748daa5237f6544e035a3f5bfda1ddb05184752e`
- `real-season-vm/proof.log`: `5f199d89c43b25e79843512057b16494c568526ce33288bac47b9745693166ea`
- `real-season-vm/red.log`: `ca25b07cdb2ed097db0f48c74fbf323821db35c2e4391136a797a5c697467f0b`
- `real-season-vm/run-status.json`: `09ae29486f1300bf3d5e3ab3b46c883ce18abda58692615c02d41fa683f595e1`
- `real-season-vm/source-hashes.json`: `4756ebfa4993faf76d9bd5be55379f005d90ebd3860ed42604709bc66a3749f7`

Full real-VM source bindings:

- `src/BiliLite.UWP/Parsing/ApiParseResult.cs`: `f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab`
- `src/BiliLite.UWP/Parsing/ApiEnvelope.cs`: `050317cddde277c46c6169f3878cb6c7accef3cec0dbbf018006cbc411948421`
- `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListParser.cs`: `9c4f55fd851fd7000c624df21832328fe2237e431b1c658a6f3505fbdb71d8e9`
- `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListMapper.cs`: `1d63a7d6c18dce72acfa300998560a6e1986c2033f9756c3a6cd6a57f70a620f`
- `src/BiliLite.UWP/Models/Api/Season/Web/WebSeasonEpListDto.cs`: `8c932e0ec3ea36339ca45985e19156d1831f583219adfd51a417f2e9febab165`
- `src/BiliLite.UWP/Models/ReturnModel.cs`: `0c5192f37d4447954937a8b226b5667dba77ed09f3d5a345b226e1e6c8994a1c`
- `src/BiliLite.UWP/Modules/IModules.cs`: `cef5334b540d7e74513d59b513e3d7671fce98e9be62b7d50bad471df14ed3c6`
- `src/BiliLite.UWP/Modules/RelayCommand.cs`: `2941ad18ba1a0aed08d0803cb6ddead223d20e750d4541f4284ecb828a1c74e8`
- `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`: `a9b84b5512e04f2081d34b9f9d5642a9f5aee902f01b476881996fc9c3c1a07b`

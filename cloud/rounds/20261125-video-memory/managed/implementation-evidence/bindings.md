# Frozen season implementation bindings

All SHA256 values bind exact bytes. Raw logs, generated C#, original snapshots and JSON manifests stay under `/workspace/work/video-memory/season-fix/`. The same seven final fixture inputs were used for original red and repaired green. Only the source-root argument changed.

## Full directly compiled real VM/parser closure and extracted source inputs

| Path | Original7356 | Repaired |
| --- | --- | --- |
| `src/BiliLite.UWP/Parsing/ApiParseResult.cs` | `f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab` | `f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab` |
| `src/BiliLite.UWP/Parsing/ApiEnvelope.cs` | `050317cddde277c46c6169f3878cb6c7accef3cec0dbbf018006cbc411948421` | `050317cddde277c46c6169f3878cb6c7accef3cec0dbbf018006cbc411948421` |
| `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListParser.cs` | `9c4f55fd851fd7000c624df21832328fe2237e431b1c658a6f3505fbdb71d8e9` | `9c4f55fd851fd7000c624df21832328fe2237e431b1c658a6f3505fbdb71d8e9` |
| `src/BiliLite.UWP/Parsing/Season/WebSeasonEpListMapper.cs` | `1d63a7d6c18dce72acfa300998560a6e1986c2033f9756c3a6cd6a57f70a620f` | `1d63a7d6c18dce72acfa300998560a6e1986c2033f9756c3a6cd6a57f70a620f` |
| `src/BiliLite.UWP/Models/Api/Season/Web/WebSeasonEpListDto.cs` | `8c932e0ec3ea36339ca45985e19156d1831f583219adfd51a417f2e9febab165` | `8c932e0ec3ea36339ca45985e19156d1831f583219adfd51a417f2e9febab165` |
| `src/BiliLite.UWP/Models/ReturnModel.cs` | `0c5192f37d4447954937a8b226b5667dba77ed09f3d5a345b226e1e6c8994a1c` | `0c5192f37d4447954937a8b226b5667dba77ed09f3d5a345b226e1e6c8994a1c` |
| `src/BiliLite.UWP/Modules/IModules.cs` | `cef5334b540d7e74513d59b513e3d7671fce98e9be62b7d50bad471df14ed3c6` | `cef5334b540d7e74513d59b513e3d7671fce98e9be62b7d50bad471df14ed3c6` |
| `src/BiliLite.UWP/Modules/RelayCommand.cs` | `2941ad18ba1a0aed08d0803cb6ddead223d20e750d4541f4284ecb828a1c74e8` | `2941ad18ba1a0aed08d0803cb6ddead223d20e750d4541f4284ecb828a1c74e8` |
| `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs` | `a9b84b5512e04f2081d34b9f9d5642a9f5aee902f01b476881996fc9c3c1a07b` | `615a4c04bb0f4077ff63976e993f013a455b6fcf55a77fc798cef0a9d5acc596` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | `b44706b9156259cc57acda3611023b4a9e0ed96f9345b3576ddaac3148804490` | `c91ceea2e58aa8318311841eaf22ef473711710c6222289baa6bed9b79bf4f5d` |
| `src/BiliLite.UWP/MainPage.xaml.cs` | `7abe52a5547d2c6764c88a81da70648b72bafa152d09d9d9109eb339a039858c` | `7abe52a5547d2c6764c88a81da70648b72bafa152d09d9d9109eb339a039858c` |
| `src/BiliLite.UWP/Controls/MyFrame.cs` | `38b392714f064a67886be401d6ec3dbc6fc38d891ec36ed4c215ed30a9554914` | `38b392714f064a67886be401d6ec3dbc6fc38d891ec36ed4c215ed30a9554914` |

## Same final fixture inputs

| File | SHA256 |
| --- | --- |
| `plan/tests/season1/Program.cs` | `ef5f606b8041dfc3c04ca27e0945abf5c86ecc9adc98308b57f87ef654a23766` |
| `plan/tests/season1/Season1Probe.csproj` | `5cd19422582bbe811c9cd3d265862a4f8b96c0fa005f810c8663b45d97c4bcb5` |
| `plan/tests/season1/Season1Stubs.cs` | `e82aba6606d5760608d4a54f37c0368867ab71ba310e46d0251f301335a16c9a` |
| `plan/tests/season1/run.sh` | `9fd4aceee8d478aeb734c00871e7d83d0470ef15335523e51644f967a54abd8b` |
| `plan/tests/season1/extract-lifetime.py` | `9945c8583bbb1dfdc8d5eead112a6be2b0920fb4f3939251274982b5d2d0a204` |
| `plan/tests/season1/SeasonLifetimeStubs.cs` | `54c9def4488b4a5801b961b999c7ce55e2a85461c838fe9c349b08b35e373354` |
| `plan/tests/season1/SeasonLifetimeProbe.cs` | `45e9fe8550fe53ea0d09b1de3c0dd0151442dca626e9618fac43ae52cc759f08` |

## Complete method/field extraction

No product method text is replaced or truncated. The generated class identifier and surrounding types/wrappers are test scaffolding, outside extracted bodies. The whole actual VM is directly compiled. Every anchor must occur once; the extractor scans brace depth while excluding strings/comments. Missing and duplicate-anchor controls both exit1 before creating output.

| Anchor | Original lines / body SHA256 | Fixed lines / body SHA256 |
| --- | --- | --- |
| `complete page fields before constructor` | 27–35 / `c4febe77337501dbbacbdb1c2d532993fde6cdf4a1021c9ed8ef27879b12228a` | 27–37 / `dcc05e4ac035fe8f281a91597748bea287f3c960c6b00992245138e9450c10c7` |
| `private void SeasonDetailPage_Loaded(object sender, RoutedEventArgs e)` | 51–54 / `8ea49703dc369d4d6f1754387a27258ce4407036ab0f7cc6271d5e1ff46d31ce` | 53–56 / `8ea49703dc369d4d6f1754387a27258ce4407036ab0f7cc6271d5e1ff46d31ce` |
| `private void SubscribeClosedPage()` | 56–63 / `7371621e44e3e0deef10f48b8cb95c4b11905dfd0c77c6373791611b4b4922b1` | 58–66 / `424dfd4e3b7741d45a0ed2f205a516b5f60d3968ad91b09de094bef80a816513` |
| `private void UnsubscribeClosedPage()` | 65–71 / `5e4c52a1a599b9aadc86f4d4ac24c5b0008a80b6e5ccbab554e845e17b99ad7f` | 68–74 / `5e4c52a1a599b9aadc86f4d4ac24c5b0008a80b6e5ccbab554e845e17b99ad7f` |
| `private void SubscribeShare()` | 73–78 / `077e519c351912007be80fb6f35e3f031d1f3ab4b5d172fc372070fa6a2a83d2` | 76–82 / `bad1e323312cb4e634db4076032147f2c24957f5cf38ff60ecd20caa1a3a2111` |
| `private void ReleaseShare()` | 80–83 / `00edc11878b717cb2ba5146895590c0da1ee74a8264e690e2f6751c1e7f8ce87` | 84–87 / `00edc11878b717cb2ba5146895590c0da1ee74a8264e690e2f6751c1e7f8ce87` |
| `private void SeasonDetailPage_ClosedPage(object sender, EventArgs e)` | 85–88 / `884601436e8d3aa7dae8073e2f2d488ef3cd85e4b3e19d001182189725d75b58` | 89–92 / `884601436e8d3aa7dae8073e2f2d488ef3cd85e4b3e19d001182189725d75b58` |
| `private void ClosePage()` | 89–106 / `81d2d26ff81c9795aef5e6edeb9ad6d62aa260c788c0a7c257eaeaad683d95ff` | 93–114 / `7c0dc356e725eeff48d4af6b1fb49c7ae1ac3f9f9fe291ec75a68f2e03ed988f` |
| `protected async override void OnNavigatedTo(NavigationEventArgs e)` | 114–150 / `6dcf95cac486e75e49d61ed791cc1e1e49a56d28811e91bee97a60c3a210a1f9` | 122–165 / `6f9690d74ba2f89199ee07c17b171859d5d224f4e2faaedcbad5a3dc09d2fb49` |
| `protected override void OnNavigatedFrom(NavigationEventArgs e)` | 151–156 / `263184e028bef5583bf92b5e8352024314aa37f3491c23e06074f78b1e0ae0cf` | 167–172 / `263184e028bef5583bf92b5e8352024314aa37f3491c23e06074f78b1e0ae0cf` |
| `protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)` | 268–276 / `b5eb5b1bf04f8d3ef5a6ab6e9203196116f8c89567beeec0b27b912b1509f5cc` | 291–299 / `eb15a1ccb1c16cc80adc368a9827fa9456032beb4998795be7e6a632e26a5b5d` |
| `private async Task InitSeasonDetail()` | 158–173 / `635434bab6f02f2728a82f584ea9f84b838ec3286bd594a04fc649714b6c9325` | 174–196 / `3c66a6eb2c2a03d170483e3dce0cfe6955e77d2c851581f07a25fbacc0fe96dd` |
| `private void InitializePlayInfo()` | 175–205 / `474cff18b342c4193e23840e8b735887180f2f83c5714a32609a6f4e0f516357` | 198–228 / `474cff18b342c4193e23840e8b735887180f2f83c5714a32609a6f4e0f516357` |
| `private void UpdatePlayInfoToEpisode(int index)` | 206–236 / `bbb76d5f587861ac6fb20d790e9cb56913b376bd4bfdea22551cd6fa5d182f72` | 229–259 / `bbb76d5f587861ac6fb20d790e9cb56913b376bd4bfdea22551cd6fa5d182f72` |
| `private void UpdatePlayInfoToPreview(int index)` | 237–267 / `0676af27083b95a2aee99a40e0c1e3d6d10b61d99ab4b213e4c54408cffe5fb5` | 260–290 / `0676af27083b95a2aee99a40e0c1e3d6d10b61d99ab4b213e4c54408cffe5fb5` |
| `public void ChangeTitle(string title)` | 277–291 / `9a7f32933102f6940b2d630e7e7dcad1e35e285a3bf28137450e11ebab9cb717` | 300–314 / `9a7f32933102f6940b2d630e7e7dcad1e35e285a3bf28137450e11ebab9cb717` |
| `private async void btnRefresh_Click(object sender, RoutedEventArgs e)` | 762–766 / `23f27e841cac86eb0335bc2119c839bbbfa20cf9675f86ef79d49e74a33447bb` | 785–789 / `23f27e841cac86eb0335bc2119c839bbbfa20cf9675f86ef79d49e74a33447bb` |
| `private void ClosePage(TabViewItem tabItem)` | 191–202 / `594292442864c695191320a5c0b3066f441de843d8f5b45f5f705ad751c6ec7a` | 191–202 / `594292442864c695191320a5c0b3066f441de843d8f5b45f5f705ad751c6ec7a` |
| `public class MyFrame:Frame` | 6–14 / `e5fa6949d3e8ed75fb2ab9cd6bf123bfca9dbb0596907f4b34bd41e0576e531f` | 6–14 / `e5fa6949d3e8ed75fb2ab9cd6bf123bfca9dbb0596907f4b34bd41e0576e531f` |

## External receipts

| Scratch file | SHA256 |
| --- | --- |
| `bindings.json` | `36616a1c40c2bbc436e83734993ef39b2321092ff8baf97f10bb0a1d1bc15baa` |
| `original-SeasonLifetimeSlices.g.manifest.json` | `e50f6ba4379ba5447335fc0dee5f7402ba528f51aa7e8d6dfc597de031e4d536` |
| `fixed-SeasonLifetimeSlices.g.manifest.json` | `0174a4cff1cbb8ffce7ea225cadc0e667f86c522e62b1f9667f6c8ec36b67703` |
| `original-SeasonLifetimeSlices.g.cs` | `ea61e4bf907aa293964c03be51476842de8a77e95b13714904937350e34147b8` |
| `fixed-SeasonLifetimeSlices.g.cs` | `6a710998d42bf3a9f8210360d46ec6e9fcb239e38d4de89a542ba6c3e64849c7` |
| `original-red.log` | `2684d4fce5d77df143db16b7dbbe89078696b7fd7b193d1d4ea66405642fc0df` |
| `fixed-green.log` | `42f33db65b4c3d25d8473ea0f9c7602fbd3502f3fdcae02e8707c5224412d4b0` |
| `red-status.json` | `8ef199e2ba32edcf45464649ce4dae0f2106f394db6f819c55faa6df8200c959` |
| `green-status.json` | `5bc232b82de377a5655a39a2af9d7ae0fc868de5b9df957e18905cb914606c88` |
| `selected.log` | `d4a202eb705ce378cf7b767aaab9984e6a3beb396e162467f11d444e8f238e77` |
| `selected-status.json` | `5bc232b82de377a5655a39a2af9d7ae0fc868de5b9df957e18905cb914606c88` |
| `selected-summary.json` | `47c8f30fe1e89a6c944b94b9650aa2129ced86b4baec48e4ec91fb04d69c410e` |
| `selected-report/results.json` | `100b3e3086a94b74b384d89e2bc2bc54769ab510d54f84d0987bc2d4e8b4a9e0` |
| `extractor-controls.log` | `16a3094665e450289b51ce911d09fc93412df94a01c190be8dc1abe5a8d0d5d0` |
| `check-extractor.py` | `5109a29e3499fc1c3ae3b259791643920c8d441fec27b55d5b2fff306fab085a` |
| `environment.json` | `d60fe1271a5ebb1cff240d54a05e6b2c92ba6a9e0e1f523ea276c2eb5463a1f2` |
| `fixed-binary/Season1Probe.dll` | `a72ba46a0ef09e134ff308aa60a634a5c448102ea988f6386536540ca7aa28c5` |
| `fixed-binary/Newtonsoft.Json.dll` | `22c649f75fce5be7c7ccda8880473b634ef69ecf33f5d1ab8ad892caf47d5a07` |
| `fixed-binary/Season1Probe.runtimeconfig.json` | `088bcc1a867d80d3923a812855f7ae9297fc8ac3d844cf9a83510bd8d1352af8` |
| `finalize-evidence.py` | `71aee44ab3018ecbad6a0f8c2199ca7a0a27a29d0cf673dd6e5c2743de09df4f` |

PlayerControl is not compiled by the lifetime fixture. Its original source was captured before edits, remains unchanged at this release, and has SHA256 `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`. The fixture uses the explicitly labeled PlayerBoundary spy, counting attempted calls even after its modeled Dispose; no private product flag is reset. The selected original playersess source-wiring probe independently reads the frozen product file.

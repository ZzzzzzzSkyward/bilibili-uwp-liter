# Actual-source extraction provenance

Source state: product files from `10dd84857c748f6638e187d891d6a6649f887bb6` (U1 `5eec54e`, PB1 `edbd873`); uncommitted harness-only QF1 inputs captured in scratch manifest. All ranges below use one-based source line numbers and SHA-256 over UTF-8 text with LF. They are evidence, not committed production clones.

`run-auth01b.sh` retains exact Account method boundaries from its original extraction; adds the actual static readonly mirror initializer with the real SettingHelper delegates, and extracts actual ICookieStore interface from CookieService. The complete helper block includes provenance/expiry/refresh owner snapshot, ApplyCookieLogin, mirror, restore, supporting cookie declarations and keys. Whole RefreshCredentialPolicy/WebRefreshTransaction/CookieAppKeyBridge/AccessKeyRenewal/UserModels/CookieOrigins/PasswordLoginSession/IsolatedPasswordLoginTransport files are compiled, with primitive settings/HTTP/cookie substitutes. No credential decision is recreated.

LoginVM extraction retains every original method range listed below. The four original test-only signature transformations are preserved: public async void DoPasswordLogin/CompletePasswordVerificationCaptcha/SendPasswordVerificationSMS and private async void CompletePasswordLoginCheck become async Task; bodies are unchanged. Wrapper fields/events/property notifications/timers are external UI dependencies; Account stub now accepts and records AppkeyIdentity. Existing lifetime/cancel/retry tests remain unchanged. CS4014 warnings at calls originally made to async-void methods are reported, not suppressed.

playersess runner uses bounded awk extraction of the four verbatim methods plus all five actual field declarations, verifies counts 4/5, then compiles them in a partial harness class. Only primitive PlayInfo/player/settings/report substitutes and public test entry points are provided. The constructor explicitly marks a ready played item; default history serialization/pending/task initializers are the real source declarations. Report A is synchronously started and blocked before B/C calls; those calls occur sequentially, then A is released and all waiters awaited. No thread races or timing sleeps select the tail. The original SDK and --player selection remain; PLAYER_HISTORY_SOURCE permits isolated scratch negative-control input. Generated C# remains a trapped temp file supplied via HistoryCheckpointSource MSBuild property; direct csproj invocation needs that generated input, so use the owning Bash runner. No Python dependency is added to the committed runner.

| Source | Extracted range | Lines | SHA-256 |
| --- | --- | --- | --- |
| `src/BiliLite.UWP/Modules/User/Account.cs` | `actual static readonly mirror initializer` | 22–23 | `05900d58e93ff9816be25c1aa0c53229ede15b338aeea5d629a3bdf58a942bf8` |
| `src/BiliLite.UWP/Modules/User/Account.cs` | `CommitCookieLogin extraction through before FinishCookieLoginAsync` | 445–451 | `79ca469ac12678455e17b95b6e9ea62555d22aa3c138bc527763ea6ae8c61d11` |
| `src/BiliLite.UWP/Modules/User/Account.cs` | `complete snapshot/apply/restore/support block` | 468–669 | `fde9f927a8ffda235e73c0f6780205758ef857922bf2dfc8484051eb38c046c1` |
| `src/BiliLite.UWP/Services/CookieService.cs` | `actual ICookieStore declaration` | 18–22 | `fb32818238f7bd46f69c4369fbe4497774a2c6dcc8cc2ef8c0e99b3da2692ac5` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public bool PasswordVerificationPending` | 63–91 | `fde79979293b28ed705d92ce4057d4da8ccd84fceeee4f569f800c633170b9ff` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private void MessageCenter_LogoutedEvent(` | 51–54 | `4ccb7a18abefdd287ceabe1e511ff3b38c438f379ec608334b3a47cd312cf557` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public void ChangeLoginType(` | 131–171 | `a38d6bf44eec5b73b00c61828d3c3d827732f9428f7a0e796965cde08c8b82ef` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public void DoLogin()` | 172–190 | `6ab76c752cafc8ec8611e8bd9360317b9372ad2d982318629f62da2b103e78c2` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public void CancelPasswordVerification()` | 191–220 | `90238530c13591ed1fc26693cf9caeb5c04574ded22eb2564e24fcf39bc32189` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public async void DoPasswordLogin(` | 526–586 | `efcbeff38c1dfe360025c7fa8edd97946f850d090d21918302fe3fcc851eb59d` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private async Task<LoginCallbackModel> HandleLoginResult(` | 679–759 | `2b3e92111b2979b8b735b6d818ee9c95da6f30b992d4585f9a7edcf7c7dc098b` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private void HandleResult(` | 760–809 | `572510aa3287c60df118bdb6c93beed8cf7fd2ca75e5c233d79693620696f45c` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private async Task<GeetestRequestModel> StartGeetestCaptcha()` | 810–826 | `6d4c68b52f8c4c6e85fb3357c30fbdd0b2b69cfa734bac733752f1cb33ff089b` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public async void CompletePasswordVerificationCaptcha(` | 827–876 | `cc1436d661f781c74eb55e64f2083a4e0e813a5fbe0315e47cee56be09fb2299` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `public async void SendPasswordVerificationSMS()` | 877–901 | `109f91894c767de7394ce0e1056ff4c3f1b1ae212eac4a7785ff34c7b5089976` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private async Task SendPasswordVerificationSMSCore(` | 902–920 | `ca686f9de2a761e280dc392d6ef013e49ef907e0e3cc4391e57ac4252360ce7d` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private async void CompletePasswordLoginCheck()` | 921–974 | `512ec6c9fbb261437f8c009cad3a67393c4b80ce32908a82f586eb2da530fa3b` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private bool IsCurrentPasswordOperation(` | 975–977 | `53e786b7b25619490cf0bdeea1b66389c3413be897408a5517c52e41e679b0d5` |
| `src/BiliLite.UWP/Modules/User/LoginVM.cs` | `private static string GetQueryParameter(` | 978–992 | `23b81c8f4b28fa77c67506fa83fe1a5353e7b0aa3cd78be089ceff6e5c223c7f` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | `private static string HistoryKey(PlayInfo item)` | 3435–3438 | `3b35a07b5bf260cc03c88fe9100cbf445d5c16621662b7676a4f0c23c74a69cf` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | `private async Task CheckpointHistoryAsync(bool report, bool reportWhilePaused = false)` | 3446–3480 | `4ba5217e3a83bee5d6747157eacaf9026ee8174331eb0c3ff62ac0b8cb358f29` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | `private async Task DrainHistoryReportsAsync()` | 3486–3515 | `6a68c9de05fb672c9452c65676d29f1db807ff31a5a9a909937cd8fbb54aaee1` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | `private async Task FlushPlaybackAsync()` | 3528–3531 | `6df0fb8222f958177aa12ef4b74631e1195ea82b8a65808903318f1d7658fce2` |
| `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` | `five actual history field declarations` | 2108–2115 | `6a536babb2b01cda0c25925224372754c0a0c0ab7c88e8e84cef21257aec3190` |

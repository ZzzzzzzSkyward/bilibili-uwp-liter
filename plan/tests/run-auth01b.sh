#!/usr/bin/env bash
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sdk="$tools/dotnet/sdk/8.0.427"
refs="$tools/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
scratch="$(mktemp -d /tmp/auth01b.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
account_source="$scratch/AccountCookieCommit.cs"
{
  cat <<'CS'
using System;
using System.Collections.Generic;
using System.Linq;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Services;
using Windows.Web.Http.Filters;
namespace BiliLite.Modules { public sealed class Account {
CS
  awk '
    /public bool CommitCookieLogin\(/ { phase = 1 }
    phase == 1 && /public async Task FinishCookieLoginAsync\(/ { phase = 0 }
    phase == 1 { print }
    /private static CookieLoginSnapshot CaptureCookieLoginSnapshot\(/ { phase = 2 }
    phase == 2 && /public async Task<MyProfileModel> GetProfile\(/ { exit }
    phase == 2 { print }
  ' "$repo/src/BiliLite.UWP/Modules/User/Account.cs"
  echo '}}'
} > "$account_source"
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
args+=("-r:$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll")
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/AUTH01B.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Services/PasswordLoginSession.cs" \
  "$repo/src/BiliLite.UWP/Services/IsolatedPasswordLoginTransport.cs" \
  "$account_source" \
  "$(dirname -- "${BASH_SOURCE[0]}")/AUTH01BStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/AUTH01B.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/AUTH01B.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
export DOTNET_ROOT="$tools/dotnet" DOTNET_CLI_HOME="$tools/cache/dotnet-cli" NUGET_PACKAGES="$tools/cache/nuget" XDG_CACHE_HOME="$tools/cache/xdg" XDG_DATA_HOME="$tools/cache/xdg-data"
"$DOTNET_ROOT/dotnet" "$scratch/AUTH01B.dll"

login_vm_source="$scratch/LoginVMAuthProbe.cs"
extract_method() {
  awk -v start="$1" -v stop="$2" '
    index($0, start) { active = 1 }
    active && index($0, stop) { exit }
    active { print }
  ' "$repo/src/BiliLite.UWP/Modules/User/LoginVM.cs"
}
extract_tail_method() {
  awk -v start="$1" '
    index($0, start) { active = 1 }
    active && /^    }$/ { exit }
    active { print }
  ' "$repo/src/BiliLite.UWP/Modules/User/LoginVM.cs"
}
{
  cat <<'CS'
using System;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules;
using BiliLite.Services;
namespace BiliLite.Modules.User
{
public sealed class LoginVM
{
    public Account account = new Account();
    public Api.AccountApi accountApi = new Api.AccountApi();
    private readonly PasswordLoginSessionCoordinator passwordSessionCoordinator = new PasswordLoginSessionCoordinator();
    private long passwordSessionOperation;
    private string passwordVerificationTmpToken;
    private string passwordVerificationRequestId;
    private string passwordVerificationCaptchaKey;
    private string passwordVerificationChallenge;
    private string passwordVerificationGt;
    private string passwordVerificationValidate;
    private string passwordVerificationSeccode;
    private string passwordVerificationRecaptchaToken;
    private bool passwordLoginDialogClosed;
    private bool passwordVerificationPending;
    private bool passwordVerificationCaptchaActive;
    private string pwdSessionId = "";
    private string userName;
    private string password;
    private string code;
    private string hideTel;
    private bool enableSendSMS = true;
    private bool primaryButtonEnable = true;
    public Timer smsTimer = new Timer();
    public Timer qrTimer;
    public event EventHandler<Uri> OpenWebView;
    public event EventHandler<bool> SetWebViewVisibility;
    public event EventHandler CloseDialog;
    public event Action<bool> PrimaryButtonChanged;
    public string UserName { get => userName; set => userName = value; }
    public string Password { get => password; set => password = value; }
    public string Code { get => code; set => code = value; }
    public string HideTel { get => hideTel; set => hideTel = value; }
    public bool EnableSendSMS { get => enableSendSMS; set => enableSendSMS = value; }
    public int SMSCountDown { get; set; } = 60;
    public bool PrimaryButtonEnable { get => primaryButtonEnable; set { primaryButtonEnable = value; PrimaryButtonChanged?.Invoke(value); } }
    public int loginType;
    public int LoginType { get => loginType; set => loginType = value; }
    public string PasswordVerificationCaptchaKeyForProbe => passwordVerificationCaptchaKey;
    public bool Manual { get; set; }
    public string Title { get; set; }
    public void DoPropertyChanged(string propertyName) { }
    private void GetQRAuthInfo() { }
    private void DoSMSLogin() { }
    public LoginVM() { MessageCenter.LogoutedEvent += MessageCenter_LogoutedEvent; }
    public Task InvokeCompletePasswordLoginCheckAsync() => CompletePasswordLoginCheck();
CS
  extract_method 'public bool PasswordVerificationPending' 'public int loginType'
  extract_method 'private void MessageCenter_LogoutedEvent(' 'public event EventHandler<Uri> OpenWebView'
  extract_method 'public void ChangeLoginType(' 'public void DoLogin()'
  extract_method 'public void DoLogin()' 'public void CancelPasswordVerification()'
  extract_method 'public void CancelPasswordVerification()' 'public async void ValidateLogin('
  extract_method 'public async void DoPasswordLogin(' '#endregion'
  extract_method 'private async Task<LoginCallbackModel> HandleLoginResult(' 'private void HandleResult('
  extract_method 'private void HandleResult(' 'private async Task<GeetestRequestModel> StartGeetestCaptcha()'
  extract_method 'private async Task<GeetestRequestModel> StartGeetestCaptcha()' 'public async void CompletePasswordVerificationCaptcha('
  extract_method 'public async void CompletePasswordVerificationCaptcha(' 'public async void SendPasswordVerificationSMS()'
  extract_method 'public async void SendPasswordVerificationSMS()' 'private async Task SendPasswordVerificationSMSCore('
  extract_method 'private async Task SendPasswordVerificationSMSCore(' 'private async void CompletePasswordLoginCheck()'
  extract_method 'private async void CompletePasswordLoginCheck()' 'private bool IsCurrentPasswordOperation('
  extract_method 'private bool IsCurrentPasswordOperation(' 'private static string GetQueryParameter('
  extract_tail_method 'private static string GetQueryParameter('
  cat <<'CS'
}
}
CS
} > "$login_vm_source"
# Test-only instrumentation makes the extracted async-void UI handlers awaitable. Method bodies
# remain verbatim production source, so each gate can be released and drained deterministically.
sed -i \
  -e 's/public async void DoPasswordLogin(/public async Task DoPasswordLogin(/' \
  -e 's/public async void CompletePasswordVerificationCaptcha(/public async Task CompletePasswordVerificationCaptcha(/' \
  -e 's/public async void SendPasswordVerificationSMS(/public async Task SendPasswordVerificationSMS(/' \
  -e 's/private async void CompletePasswordLoginCheck(/private async Task CompletePasswordLoginCheck(/' \
  "$login_vm_source"
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/AUTH01BLoginVM.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Services/PasswordLoginSession.cs" \
  "$repo/src/BiliLite.UWP/Services/IsolatedPasswordLoginTransport.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/AUTH01BStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/AUTH01BLoginVMStubs.cs" \
  "$login_vm_source" \
  "$(dirname -- "${BASH_SOURCE[0]}")/AUTH01BLoginVM.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/AUTH01BLoginVM.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
"$DOTNET_ROOT/dotnet" "$scratch/AUTH01BLoginVM.dll"

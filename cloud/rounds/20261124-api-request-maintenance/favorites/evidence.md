# U9 evidence — request-boundary requirements

Date: 2026-11-24 UTC. Source HEAD: `41dc562fba6e45896b0c4ba604f9ec38f4661e73`.
Evidence class: source-confirmed and reproduced offline. No product/repository-test/build/main-doc/history edits and no network request or folder mutation.

## Source observations

| Location | Observation |
| --- | --- |
| `Api/User/FavoriteAPI.cs:116–125,135–144` | Both add/edit put `privacy=` directly after common parameters, then sign the completed body. |
| `Api/ApiHelper.cs:172–180` | Common parameters end in timestamp digits; access key is conditional on existing login state. |
| `Api/ApiHelper.cs:78–93` | Signer splits raw form on ampersands, sorts and hashes with the chosen key; it cannot restore a missing delimiter. |
| `Dialogs/CreateFavFolderDialog.xaml.cs:29`, `EditFavFolderDialog.xaml.cs:34` | Callers pass raw textbox values and public checkbox state. XAML labels it 公开收藏夹. |
| `plan/tests/c2/C2Probe.csproj:19–31`, `C2Stubs.cs:6–36`, `Program.cs:543–574` | Existing batch owns favorite-resource parsing and compatible request stubs; no source link to FavoriteAPI or ApiHelper stub yet. |

Paths beginning Api/Dialogs are under `src/BiliLite.UWP/`. The preceding verification baseline records unchanged c2 PASS 162, but repository-wide baseline FAILED and Windows gates unavailable. That result is independent of this scratch experiment.

## Actual-source reproducer

Scratch project: `/workspace/work/maintenance-foundation/favorites/request-boundary/FavoriteBoundaryProbe.csproj`.
It directly links the complete original `src/BiliLite.UWP/Api/User/FavoriteAPI.cs`. `ExtractedApiHelper.cs` contains exact method text extracted from current ApiHelper by the signatures `GetSign(string url, ApiKeyInfo apiKeyInfo, string par = "&sign=")` and `MustParameter(ApiKeyInfo apikey, bool needAccesskey = false)`, through each balanced closing brace. No product expression or helper method body is reimplemented.

The remaining scratch seam provides only ApiModel/enum/key types, dummy key/access-key values, fake login state, deterministic timestamp 1700000000, helper constants, and BCL MD5 in place of the Windows crypto primitive. A net8 build can compile all FavoriteAPI methods without UWP. Hashing/sign verification is an offline control, not certification of platform cryptography or transport. The test parses form fields with multiplicity and decodes values once.

Inputs: both operations × both `isOpen` values × logged-in/out; raw title `收藏 + &= %25 👋`, intro with newline/plus/ampersand/equal/question mark, edit ID `9007199254740993123`; all credentials dummy. No `Request()` call or HttpClient is present.

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/favorites/dotnet-cli
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$DOTNET_ROOT/dotnet" run --project /workspace/work/maintenance-foundation/favorites/request-boundary/FavoriteBoundaryProbe.csproj -c Debug
```

**Actual dotnet exit: 1. CONTRACT_VIOLATIONS=16.** Timestamp is contaminated and privacy absent in each of 8 cases. Curated observations:

| Operation | Logged in | isOpen | Decoded ts | privacy | Text/ID/credential/sign controls |
| --- | --- | --- | --- | --- | --- |
| create | False | False | `1700000000privacy=1` | absent | preserved |
| edit | False | False | `1700000000privacy=1` | absent | preserved |
| create | False | True | `1700000000privacy=0` | absent | preserved |
| edit | False | True | `1700000000privacy=0` | absent | preserved |
| create | True | False | `1700000000privacy=1` | absent | preserved |
| edit | True | False | `1700000000privacy=1` | absent | preserved |
| create | True | True | `1700000000privacy=0` | absent | preserved |
| edit | True | True | `1700000000privacy=0` | absent | preserved |

“Preserved” means raw title/intro round-trip, optional dummy access-key policy is unchanged, edit ID survives (create has none), and the actual extracted signer produces the appended signature for the final malformed unsigned body. Signing an invalid form still produces a signature; a signature-shaped suffix is therefore insufficient acceptance. The permanent c2 extension must assert the exact signing input/key and independently valid fields.

Raw log remains in scratch `result.txt`; project, generated helper, stubs and program are alongside it. No raw logs are committed. Requirements prescribe permanent owning-batch red before source change and green after; these requirements-stage results do not claim the product has been fixed.

## Source hashes and preservation

SHA-256:

```text
ff18ff97e9779be120f0eb14310f6dbf931b100bb01d039d8a42ffc16349c4f5  /workspace/bilibili-uwp-liter/src/BiliLite.UWP/Api/User/FavoriteAPI.cs
dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9  /workspace/bilibili-uwp-liter/src/BiliLite.UWP/Api/ApiHelper.cs
674733f4c8254a70bf7df9276c71a308fb6aa08d02b5ef2b7fba424ff1a391d9  /workspace/work/maintenance-foundation/favorites/request-boundary/ExtractedApiHelper.cs
a8d07c186efe0062164ec640027a51c7158d30c410c16aa5193e7aa22036f977  /workspace/work/maintenance-foundation/favorites/request-boundary/Program.cs
910c6f6d4bf828e488ee4343b0ca384b233534c1feaee4a85d9f5964dad59895  /workspace/work/maintenance-foundation/favorites/request-boundary/Stubs.cs
```

`git status --short -- src plan/tests docs plan/PLAN.md plan/VALIDATION.md` returned no paths after the experiment (exit 0). Shared cloud artifacts are concurrently owned and intentionally not represented as a globally clean worktree. Requirements write scope is only this stream's Markdown files.

# API migration progress

## Scope and source of truth

The target is `/workspace/biliuwp-lite-fix`, starting from clean baseline `ea47513` (`Document Luna subagent reasoning preference`). Read `AGENTS.md`; it requires high reasoning effort for any Luna subagent.

The sole behavioral reference is the requested shallow clone at `/workspace/biliuwp-lite`, URL `https://github.com/ywmoyue/biliuwp-lite`, commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b` on `dev`. API internet requests and external documentation were out of scope and were not used. Network was used for the shallow clone only. The environment reported unrestricted HTTP through the inherited proxy and VPN disabled; proxy and CA settings were left unchanged.

The task covers read/data retrieval APIs, with emphasis on video playback, feeds, followed authors and series, comments, and related caller integration. Publishing, posting, uploads, and action or mutation APIs are excluded. The implementation follows the sibling source where mapped while retaining target-specific callers and compatibility paths when needed. This migration does not claim to port every read API present in the reference repository.

The target is a C# UWP solution. Its legacy wrappers live in `src/BiliLite.UWP/Api/`; the reference moved most wrappers to `src/BiliLite.UWP/Models/Requests/Api/` and shared request infrastructure to `Services/` and `Models/Requests/`. Target callers remain primarily in `Modules/`, `Controls/`, and `Pages/`.

## Completed implementation

| Commit | Work |
|---|---|
| `a977277cfccf77f26fbf63978ce498d334420fd6` | Recorded setup, pinned reference, and inventoried the target retrieval API surface. |
| `dff9b93f9e926e13ae44ba2bf298e80f0cd1c47e` | Migrated playback URL, player info, danmaku segment and video detail read wrappers. |
| `cd83cd4152d54ae60a7d2f736d9e9e809f55d57f` | Migrated cursor-based comment and reply reads and emote panel headers/cookie behavior. |
| `dcd064463fba43638386b0203b9c13317f2f7c80` | Made WBI signing asynchronous and full-query, added Aurora headers and extra-cookie forwarding. |
| `c23d86724f9fa10e7e56880efbbd92d6183b804f` | Migrated recommendation feed, dynamic reads and followed-series/relation reads. |
| `882f8df1f9b26d22c935b63eef540fd4fb6c2fe9` | Migrated search, ranking, region, seasonal, home discovery and live discovery reads. |
| `fa6fb54cf5ffd6cead5c446de391b571b560b107` | Migrated account/profile, library and live-room reads and their callers. |
| `4e0783689542e625b0eb51f6219a49c998229ca1` | Integrated web video detail, related-video, tags and user-state reads; updated player metadata and interaction IDs. |
| `5578ca00abc25576e157b702965138c8c363faeb` | Migrated gRPC playback and danmaku protocol/parsing reads. |

### Shared request behavior

`ApiHelper.GetWbiSign(string)` now returns `Task<string>`, awaits available WBI keys, and returns the complete signed query. Existing callers were migrated to await and assign the returned string. `ApiModel.ExtraCookies` is forwarded through the target request extension and merged into outgoing web cookies, with extra values taking precedence. `GetAuroraHeaders()` supplies the two reference Aurora headers. The target's configured app keys remain in use; the target has no equivalent of the reference account-selected `BaseApi.AppKey`, so no new key-selection framework was added.

### Migrated retrieval areas

- **Video and playback:** regular and season playback parameters, player information, interaction edge IDs, segment danmaku retrieval, web video detail and related-video reads, tags, and login-only uploader/like/coin/favorite state reads. Current gRPC playback and TV danmaku response protocols/parsers were updated from the sibling sources.
- **Feeds and subscriptions:** recommendation feed, dynamic feed/history/detail/repost reads, source response handling in dynamic callers, followed bangumi/cinema, and relation lookup.
- **Comments and emotes:** WBI-signed cursor comment reads with legacy fallback, reply reads, and Aurora/cookie configuration for user emotes.
- **Discovery and catalog:** search and suggestions, ranking, regions, season detail/episode lists/index, anime/cinema/popular/live-home content, live areas/recommendations, and live-center read APIs. Relevant async callers were updated.
- **Account, user and library:** WBI-backed user info/video/following reads, profile and collection reads, watch-later read behavior, and live-room metadata/rank/lottery info retrieval with BUVID3 cookie support. Existing account read endpoints that already match the reference were left as they were.

## Preserved and excluded behavior

Target-only compatibility paths remain where the reference does not supply a direct replacement, including the legacy live-play URL fallback and older season detail entry points. The target's detail/proxy fallback remains available if web detail retrieval does not provide usable data. Existing matching behavior was recorded rather than rewritten where appropriate: account profile/history routes, mention suggestions, watch-later listing, user space/stat/follower/favorite-list reads, and favorite-season retrieval.

Action and mutation methods were left outside the migration, including likes, coins, follows/unfollows, favorite edits and transfers, watch-later changes, comment submission/deletion/likes, dynamic publishing/reposting/deletion/likes, live gifts/chat/rewards/lottery entry, review actions, and history deletion. The target's `FindMoreEntrance` and older season review action methods remain target-only. Some reference-only retrieval methods outside the selected migration surface remain unported; see the per-worker notes for exact ownership and choices.

## Formatting and verification

No `dotnet`, `csharpier`, or `fantomas` executable was available, and the target has no `.editorconfig` or `global.json`. No formatter was installed. If one becomes available, use a formatting-only command scoped to changed C# files, such as `dotnet format whitespace BiliLite.sln --no-restore --include <changed .cs paths>`.

Only formatting checks were run: workers report `git diff --check` passed for their owned changes, and the central progress note passed its trailing-whitespace check. No tests, builds, package restore, protocol generation, runtime checks, API requests, or correctness validation were run.

## Worker notes and resumption

- [Playback, video detail, gRPC protocols and parsers](work/api-migration/playback.md)
- [Comments and emotes](work/api-migration/comments.md)
- [Feed and followed-author reads](work/api-migration/feed_follow.md)
- [Search and catalog discovery](work/api-migration/catalog.md)
- [Account, library and live-room reads](work/api-migration/account_reads.md)

All implementation batches above are committed. To resume this work, start from the current target HEAD, use only the pinned sibling checkout as API behavior evidence, and preserve the mutation exclusions and format-only verification limit. The worker notes identify the exact source symbols and any reference additions intentionally left outside scope.

# Feed and subscribed-author API migration

## Ground truth

- Source repository: `/workspace/biliuwp-lite`
- Source revision: `ca2caddc597b3faf05b4dac6dcb226aa0694365b`
- Scope: recommendation feed, followed-author dynamic reads, subscribed anime/cinema reads, and relation lookup.

## Changes made

- Recommendation feed now uses the source `/x/v2/feed/index` endpoint and Aurora headers.
- Dynamic API reads now match source endpoints and parameters for new feed, history, detail, repost, and space history. The source's `SpaceHistoryV2`, `Article`, and `ArticleV2` reads are exposed as methods too.
- Dynamic callers now consume the legacy response `data.cards` shape and use the existing legacy dynamic display converter.
- Followed bangumi reads now send source Aurora headers; added source `GetAttention(mid)` relation lookup for the playback detail integration.
- No POST, publishing, follow/unfollow action, or upload methods were changed.

## Files changed

- `src/BiliLite.UWP/Api/Home/RecommendAPI.cs`
- `src/BiliLite.UWP/Api/User/DynamicAPI.cs`
- `src/BiliLite.UWP/Api/User/FollowAPI.cs`
- `src/BiliLite.UWP/Modules/Home/DynamicVM.cs`
- `src/BiliLite.UWP/Modules/User/DynamicVM.cs`
- `work/api-migration/feed_follow.md`

## Progress and verification

- Read `/workspace/biliuwp-lite-fix/AGENTS.md`.
- Compared target behavior with source revision `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.
- `git diff --check` on the owned API and caller files: passed.
- No tests, builds, restore, API requests, or external sources used.
- Ready for parent review and a serialized commit slot; no files staged or committed yet.

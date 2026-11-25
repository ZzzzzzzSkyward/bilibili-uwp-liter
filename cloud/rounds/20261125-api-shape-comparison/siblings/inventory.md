# Immutable sibling source inventory — initial release

Status: **RELEASED FOR READ CUSTODY**, 2026-11-25 UTC. Owner: `api_siblings_requirements` (`gpt-6.1-sol` / `xhigh`). The three cache worktrees below are detached at the listed full commits and were clean at release. They will not be fetched, checked out, edited, or repinned while readers use them. Later evidence documents may grow; these pins do not move. Main repository Git index/refs and existing memory-round edits are untouched.

## Released pins

All candidates were inspected through public GitHub repository/commit metadata and ordinary source-only HTTPS clones. All report `archived=false`, `disabled=false`. Baseline date is 2026-11-25; preferred substantive-change window begins 2026-08-27. Clones use `--depth 100 --single-branch` and disabled hooks/LFS smudging; no foreign script, build, test, install, dependency or hook was executed.

| ID | Independent source / client family | Default branch and immutable pin | Pin committer date (UTC) | License at pin | Local read-only custody path |
| --- | --- | --- | --- | --- | --- |
| S1 | [bggRGjQaUbCoE/PiliPlus](https://github.com/bggRGjQaUbCoE/PiliPlus), Flutter working client, primarily web REST plus app/gRPC | `main`; `217fb54f51f6764176319bbdedab2a7522d75401` | 2026-11-14 08:04:43 | `LICENSE`: GNU GPL v3; GitHub SPDX `GPL-3.0` | `/workspace/work/api-shape-comparison/siblings/PiliPlus-217fb54f/` |
| S2 | [10miaomiao/bilimiao2](https://github.com/10miaomiao/bilimiao2), independently implemented Kotlin/Compose working client, web/app REST and gRPC | `master`; `d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b` | 2026-11-20 16:10:43 | `LICENSE`: GNU GPL v3; GitHub SPDX `GPL-3.0` | `/workspace/work/api-shape-comparison/siblings/bilimiao2-d60a9477/` |
| S3 | [nooblong/bilibili-api](https://github.com/nooblong/bilibili-api), API-focused Python implementation; maintained fork of archived `Nemo2011/bilibili-api` | `main`; `4789508a93f3a39b7d3c896273309db892a9cc8b` | 2026-09-28 19:50:14 | `LICENSE`: GNU GPL v3; GitHub SPDX `GPL-3.0` | `/workspace/work/api-shape-comparison/siblings/bilibili-api-4789508a/` |

Independence means three implementation lineages, not three mirrors of one codebase. S1's broader PiliPala ancestry does not make S2 or S3 the same implementation. S3 counts once with its Nemo/MoyuScript ancestry; no other Nemo fork is counted. Our old ywmoyue comparison does not count as new independent corroboration.

## Actual maintenance evidence

| Source | Substantive observed change within preferred window | Inspection and strength |
| --- | --- | --- |
| S1 | [`c741279080fa90571d59ff9558640beb88861492`](https://github.com/bggRGjQaUbCoE/PiliPlus/commit/c741279080fa90571d59ff9558640beb88861492), 2026-10-31 08:00:42 UTC, `add page order support for sub detail`; request/decoder/controller/view changes in `lib/http/fav.dart`, favorite/subscription DTOs and consumers. More recent API-path commits include `6a1090102723cd4469d847b01506223aee62d15d` (2026-11-12, fans medal management) and `f67a902916038cd863ab40cecf76c767427d562e` (2026-11-07, offline subtitle/viewpoint). | `git show --stat` plus actual favorite request/decoder diff inspected, not release/bot/README-only. |
| S2 | [`8ea47ff800d7c5927a180f129e236f43737dde8f`](https://github.com/10miaomiao/bilimiao2/commit/8ea47ff800d7c5927a180f129e236f43737dde8f), 2026-09-09 15:37:46 UTC, separates live bilibili gRPC implementation into KMP module and adapts `BiliGRPCHttp`/`PlayListStore`; current client also has substantive player implementation commit `019b67034aaecb186df31fcc32ecbc2c06f16634` (2026-11-20, keyboard controls). | Actual changed-file list inspected. September item is API implementation maintenance/refactor, **not proof the REST endpoints were recently server-validated**. |
| S3 | [`425f5200f5fa17a316b93cdf21643513d9a3075f`](https://github.com/nooblong/bilibili-api/commit/425f5200f5fa17a316b93cdf21643513d9a3075f), 2026-08-28 06:26:29 UTC, guards downloaded video/audio stream comparison against missing quality; merged 2026-09-28 at pin. | Actual four added guards in `bilibili_api/video.py` inspected. Meets 90-day preference narrowly (89 days). This is **narrow maintenance**, not a claim that the whole inherited API catalog is refreshed. Old inherited endpoints require corroboration. |

## Entry paths for contract readers

| Source | Requests / transport | Decoders / callers |
| --- | --- | --- |
| S1 | `lib/http/api.dart`, `constants.dart`, `init.dart`; `fav.dart`, `search.dart`, `reply.dart`, `dynamics.dart`, `video.dart`, `pgc.dart` | `lib/models_new/fav/`, `lib/models_new/sub/`, `lib/models_new/` search/reply/dynamic DTOs; `lib/pages/fav_detail/controller.dart`, `subscription_detail/controller.dart` and feature controllers. |
| S2 | `bilimiao-comm/src/commonMain/kotlin/com/a10miaomiao/bilimiao/comm/apis/{UserApi,SearchApi,CommentApi,VideoAPI,BangumiAPI,PlayerAPI}.kt`; sibling `network/` transport; `bilimiao-grpc/` read-only schema/implementation | `bilimiao-comm/.../entity/`; `bilimiao-compose/` view models and stores. Use `rg --files` from cache root; do not execute foreign code. |
| S3 | `bilibili_api/{favorite_list,search,comment,dynamic,video}.py`; `bilibili_api/data/api/*.json`; `bilibili_api/utils/network.py` | Same module consumers and `docs/`; inherited declaration is weaker than current independently corroborated implementation use. |

## Rejected / not selected candidates

- `PiliPlusTeam/PiliPlus`, `Richasy/BilibiliKernel`, `bilibili-api-dev/bilibili-api`: direct public repository metadata lookup returned **404**, not guessed substitutions.
- `Vespa314/bilibili-api`: nonarchived but `pushed_at=2022-06-19`; description explicitly says no longer maintained. Rejected as active.
- `Richasy/Bili.Copilot`: nonarchived but `pushed_at=2025-11-26`; outside activity window by roughly a year. Not selected as active primary.
- `ILoveScratch2/bilibili-api-collect-new`: nonarchived; latest pin candidate `bee4521ead2aa3699d0c77455616101a241c123f` (2026-01-30 README), latest observed substantive schema commit `cfc5fddcc8a94b74d91970bb5b4eaeb349addc47` (2026-01-22 AI audio fields). Too old for active primary.
- `BACNext/BACNext`: nonarchived MIT reference; recent collection entries are automated publication commits, latest `d04f2d5bcf9eabd366a1d46c156b44beb92cdb27` (2026-08-17), outside preference and no inspected substantive underlying source change. Not counted.
- `ywmoyue/biliuwp-lite`: nonarchived, default `dev`, current `1dbe49d6aac82d84ab3915c148b195a3f2fb6e7b` (2026-11-24). Active UI/player changes observed, but same historical client lineage as this project; not counted as independent corroboration or used to reopen the frozen 2026-11-09 route-count comparison.
- Discovery dispatch already records archived popular predecessors (`SocialSisterYi/bilibili-API-collect`, `Nemo2011/bilibili-api`, Richasy UWP predecessors, `xiaoyaocz/biliuwp`). That root search is screening input, not newly inspected evidence here.

## Evidence limitations and reuse

Pins establish observed source usage only. No signed-in Bilibili request, write request, live-session/protocol migration, public service response validation or native client execution is claimed. GPL code is read as evidence; derive minimal original changes/fixtures and cite provenance, do not transplant implementation. Account P4/P5 hold, credential/session invariants and gated live U2 remain unchanged.

Full request-to-decoder-to-caller observations will be written to `siblings/evidence.md` and bounded requirements to `siblings/requirements.md`. Contract expert may start reading these three frozen caches immediately.

## Final custody receipt

Discovery completed on 2026-11-25. `siblings/evidence.md` and `siblings/requirements.md` are complete and ready for independent review. Final per-cache `git rev-parse HEAD` still exactly matches the three pins above; `git symbolic-ref -q HEAD` exits 1 (detached); `git status --porcelain` exits 0 with empty output. Source files/ref targets have not changed since initial read release. A compact metadata observation file remains private at `/workspace/work/api-shape-comparison/siblings/metadata-observed.json`. The three assigned Markdown artifacts passed a trailing-whitespace check. No product/test/build/main-document changes, main Git index/ref mutation, foreign execution, live-service acceptance claim or publication was performed by this owner. Read custody is fully released; future source updates require a new cache path and inventory release.

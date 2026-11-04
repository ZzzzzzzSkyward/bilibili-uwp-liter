# Review origin/dev 2fdcb8a58f6b35274346019e343f1a7ebb012a70

Reviewed all new source changes since `4db61b859b9f72373e32e3d455380ad1c36314e1`, with Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed.

## Reloading multiple evicted pages reverses their order

Location: `src/BiliLite.UWP/Services/DynamicFeedWindow.cs:214` and `:226-227`; prepend at `src/BiliLite.UWP/Modules/User/DynamicVM.cs:1596-1600`.

Trigger: scroll through 12 pages so the ten-page window retains pages 3–12 and the checkpoint list holds pages 1 and 2. Click “重新加载” twice. Both checkpoint methods select index 0, so the first request restores page 1 and the second restores page 2. Each response is inserted at the start of both `Items` and the window's page list. The visible order becomes `[page 2, page 1, page 3, …]`, rather than the original feed order; even the first reload jumps across the still-missing page 2. Users scrolling back through retained history see cards in the wrong sequence.

Suggested fix: peek and consume the most recently evicted checkpoint (the page immediately preceding the current window), so prepending restores page 2 and then page 1. Alternatively, place restored pages and their items according to page order instead of always prepending. Keep peek/take consistent and retain the checkpoint on request failure.

BR-21: source now drops evicted card models from the feed collection and caps repost expansion at two card layers, addressing two retention paths. This does not establish the cause of the reported session-wide memory growth or resolve the player/native-memory question; that remains outside what this static review can determine.

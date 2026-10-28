The plan is to port notifications, gRPC, API organization, authentication improvements, and danmaku improvements, while expanding your existing local-media player into a structured media-library system. SponsorBlock is excluded entirely.

The key architectural decision is to keep the existing player as the playback engine and introduce a local media manifest that describes a video and its associated audio, subtitles, and danmaku files. This avoids hard-coding assumptions about a folder's layout into the player.

## Implementation phases

Phase 1 — Establish the integration boundary

Foundation · Before feature ports

- Preserve the existing `PlayerControl`, `LocalPlayerPage`, and local playback path.
- Introduce a local-media domain model independent of UI controls.
- Record the target's current API, auth, danmaku, and gRPC dependencies before changing them.
- Add compatibility adapters so existing downloaded videos continue to play.

Phase 2 — Local-media schema and loading

First user-facing deliverable

- Define a versioned manifest and folder convention.
- Discover media assets, validate paths, and associate alternate tracks.
- Support both manifest-driven folders and a single-file playback fallback.
- Wire the manifest into the current player, including subtitle and danmaku selection.

Phase 3 — API and authentication

Low-level service changes

- Separate API endpoint classes, cookie handling, and WBI signing.
- Port missing account-verification and cookie-refresh endpoints.
- Keep existing public method signatures until callers can migrate safely.

Phase 4 — gRPC and danmaku

- Add protocol definitions and gRPC methods incrementally.
- Add the dedicated danmaku API and selected rendering controls.
- Preserve the existing `NSDanmaku` backend and add other renderers only if needed.

Phase 5 — Notifications and hardening

- Integrate the player toast service first.
- Add live-tile/background-task support only if wanted.
- Run regression tests across authentication, streaming, downloads, danmaku, and local playback.

## Local-media manifest v1

Your baseline already has the necessary starting point: `LocalPlayerPage`, `PlayInfo`, `PlayerControl`, `BiliDashPlayUrlInfo`, and `BiliDashItem.file`. The target's per-item `LocalPlayInfo` currently stores a single playback info object, a subtitle dictionary, one danmaku path, and a quality value. The source's model adds separate video/audio track collections and current selections.

Use that idea, but introduce a stable manifest schema of your own rather than copying the source's internal types directly.

### 1. Supported folder layout

Use `bili-media.json` as the manifest filename. All paths inside it are relative to the folder containing that manifest.

My Series/

`bili-media.json` — collection manifest

`poster.jpg` — optional cover

`Episodes/01/`

`video-1080p.mkv`, `video-720p.mp4`

`audio/ja.flac`, `audio/en.m4a`

`subtitles/en.srt`, `subtitles/zh-Hans.json`

`danmaku/bilibili.xml`, `danmaku/translated.xml`

`Episodes/02/` — same structure for the next episode

The manifest can describe a movie or an entire series. Each item is independently playable, and its video, audio, subtitle, and danmaku tracks can reference files anywhere beneath the collection root.

### 2. Canonical manifest example

This is the proposed v1 data contract. It supports alternate video files, external audio tracks, multiple subtitle tracks, and multiple danmaku tracks.

```

{
  "schemaVersion": 1,
  "id": "sample-series",
  "title": "Sample Series",
  "poster": "poster.jpg",
  "items": [
    {
      "id": "ep01",
      "title": "Episode 1",
      "videoTracks": [
        {
          "id": "1080p",
          "path": "Episodes/01/video-1080p.mkv",
          "label": "1080p",
          "quality": "1080p",
          "default": true
        },
        {
          "id": "720p",
          "path": "Episodes/01/video-720p.mp4",
          "label": "720p",
          "quality": "720p",
          "default": false
        }
      ],
      "audioTracks": [
        {
          "id": "ja",
          "path": "Episodes/01/audio/ja.flac",
          "label": "Japanese",
          "language": "ja-JP",
          "default": true
        },
        {
          "id": "en",
          "path": "Episodes/01/audio/en.m4a",
          "label": "English",
          "language": "en",
          "default": false
        }
      ],
      "subtitleTracks": [
        {
          "id": "en",
          "path": "Episodes/01/subtitles/en.srt",
          "label": "English",
          "language": "en",
          "format": "srt",
          "default": true
        },
        {
          "id": "zh",
          "path": "Episodes/01/subtitles/zh-Hans.json",
          "label": "Simplified Chinese",
          "language": "zh-Hans",
          "format": "bilibili-json",
          "default": false
        }
      ],
      "danmakuTracks": [
        {
          "id": "bili",
          "path": "Episodes/01/danmaku/bilibili.xml",
          "label": "Bilibili",
          "language": "zh-CN",
          "format": "bilibili-xml",
          "enabledByDefault": true
        },
        {
          "id": "translation",
          "path": "Episodes/01/danmaku/translated.xml",
          "label": "Translation",
          "language": "en",
          "format": "bilibili-xml",
          "enabledByDefault": false
        }
      ]
    }
  ]
}

```

The example is illustrative; the loader must validate that every referenced file actually exists.

### 3. Schema rules

| Field              | Contract                                                                                                     |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| `schemaVersion`    | Required integer; v1 is `1`.                                                                                 |
| `id`               | Stable, non-empty collection or item identifier.                                                             |
| `items`            | One or more playable items.                                                                                  |
| `videoTracks`      | Required, at least one video file. Each entry is an alternative video track, such as a different resolution. |
| `audioTracks`      | Optional external audio files. When empty, use the video's embedded audio where supported.                   |
| `subtitleTracks`   | Optional independent subtitle files. At most one is selected by default.                                     |
| `danmakuTracks`    | Optional independent danmaku files. Multiple tracks can be enabled and merged.                               |
| `path`             | Relative path, using `/` separators; absolute paths and parent-directory traversal are forbidden.            |
| `language`         | Optional BCP 47 language tag, such as `en`, `ja-JP`, or `zh-Hans`.                                           |
| `default`          | Selects the initial video, audio, or subtitle track. At most one default per respective track category.      |
| `enabledByDefault` | Controls initial danmaku activation; multiple tracks may be enabled.                                         |

Keep the v1 manifest deliberately small. Add sync offsets, chapter markers, richer metadata, and embedded-stream selection in a later schema version once the playback implementation supports them.

### 4. File-format contract

The manifest format and the decoder's capabilities are separate concerns. A file can pass manifest validation but still fail to decode.

| Media type     | Initial formats                                                    |
| -------------- | ------------------------------------------------------------------ |
| Video          | MP4, MKV, MOV, WebM, AVI, subject to the installed playback engine |
| External audio | AAC, M4A, MP3, FLAC, WAV, OGG, subject to decoder support          |
| Subtitles      | Bilibili JSON, SRT, VTT                                            |
| Danmaku        | Bilibili XML                                                       |

Your existing player already parses Bilibili subtitle JSON and Bilibili danmaku XML. Add SRT/VTT subtitle adapters; do not claim support for those formats until their parsing and rendering tests pass. ASS/SSA subtitles and additional danmaku formats can be added through future parser adapters.

## Local-media implementation tasks

These are the concrete code changes I recommend making in the target repository.

LM-01 — Add the manifest models and loader

New model and service classes

Create:

- `Models/LocalMedia/LocalMediaManifest.cs`
- `Models/LocalMedia/LocalMediaItem.cs`
- `Models/LocalMedia/LocalMediaTrack.cs`
- `Services/LocalMedia/LocalMediaManifestLoader.cs`
- `Services/LocalMedia/LocalMediaValidator.cs`

Use the existing Newtonsoft.Json dependency. Validate schema version, required fields, duplicate track IDs, default selections, supported formats, and path containment.

Acceptance: a valid manifest loads into typed models; an invalid manifest produces a useful error without crashing the app.

LM-02 — Add folder-based import

UI and Windows storage permissions

Add a folder-picker flow and a `LocalMediaLibraryService`. Open `bili-media.json` from the selected folder and resolve every asset relative to that root.

Use `StorageApplicationPermissions.FutureAccessList` to retain access to selected folders across app sessions. Do not require broad filesystem access just to support imported collections.

Preserve the existing single-file picker as a fallback that creates a one-item local session without a manifest.

Acceptance: users can open a collection, navigate its episodes, and reopen it after restarting the app.

LM-03 — Adapt manifest items to the existing player

Compatibility adapter

Add `Services/LocalMedia/LocalMediaPlaybackAdapter.cs`.

Translate each manifest item into the existing `LocalPlayerPage` playlist and `PlayInfo` model. Extend the per-item local playback model with typed track metadata while retaining its existing `Info`, `Subtitles`, `DanmakuPath`, and `Quality` properties for legacy downloads.

Reuse `BiliDashPlayUrlInfo.Video`, `BiliDashPlayUrlInfo.Audio`, and `BiliDashItem.file` to represent local video and audio files. Do not duplicate the player engine.

Acceptance: legacy downloaded videos and single-file playback work unchanged.

LM-04 — Add video and audio track selection

Add local track selectors to the existing player UI, adapting the source's separate `VideoTrackInfos`, `AudioTrackInfos`, and current-track selection concept.

- Select alternate video files without leaving the current item.
- Select external audio files independently of video.
- Preserve playback position during track changes.
- Use the FFmpeg-backed playback path for external video/audio combinations where needed; keep the native path as an option for compatible single-file playback.
- Report unsupported codecs and failed track switches cleanly.

Acceptance: switch between two video tracks and two audio tracks without losing the current playback position.

LM-05 — Add subtitle and danmaku track loading

Create:

- `Services/LocalMedia/SubtitleTrackParser.cs`
- `Services/LocalMedia/DanmakuTrackLoader.cs`

Convert SRT/VTT cues into the cue model used by the existing subtitle UI and timer. Reuse the current Bilibili JSON subtitle parser.

For danmaku, parse each enabled XML file through the existing parser, merge the results by timestamp, and feed the combined list into the existing `NSDanmaku` renderer. Add a UI to enable or disable individual danmaku tracks.

Acceptance: subtitle selection works across supported formats, and multiple danmaku tracks can be enabled without duplicating the entire playback pipeline.

## Port plan for the other features

### API organization and authentication

API-01 — Introduce a compatibility layer.

Source reference: [`Models/Requests/Api`](https://github.com/ywmoyue/biliuwp-lite/tree/dev/src/BiliLite.UWP/Models/Requests/Api?utm_source=chatgpt.com).

- Introduce a shared `BaseApi` and migrate endpoint classes incrementally into `Models/Requests/Api/`.
- Extract cookie management into `CookieService`.
- Extract WBI key retrieval, caching, and signing into `WbiKeyService`.
- Keep the existing static `ApiHelper` methods as compatibility wrappers while callers are migrated.
- Port the source's dedicated `DanmakuApi.Like` endpoint after the base API and cookie service are available.

Acceptance: existing API callers still work, and API endpoint code no longer needs to manage cookie refresh and signing independently.

AUTH-01 — Port missing authentication endpoints.

Compare the source [`AccountApi.cs`](https://github.com/ywmoyue/biliuwp-lite/blob/dev/src/BiliLite.UWP/Models/Requests/Api/AccountApi.cs?utm_source=chatgpt.com) against the [target ](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/master/src/BiliLite.UWP/Api/AccountApi.cs?utm_source=chatgpt.com)[`AccountApi.cs`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/master/src/BiliLite.UWP/Api/AccountApi.cs?utm_source=chatgpt.com).

Port these in order:

1. Password-login secondary verification: masked phone lookup, verification SMS, confirmation code submission, and cookie exchange.
2. Cookie-refresh CSRF and confirmation endpoints.
3. Missing history/WBI and QR-login variants where required.

Keep the target's `GetKey2023()`, `Login2023()`, and existing login UI. Integrate new endpoints into the existing login flow rather than replacing it wholesale.

Acceptance: independently test password, SMS, QR login, secondary verification, session restoration, cookie refresh, and logout. Verify that failed verification cannot accidentally replace a valid session.

### gRPC

Source reference: [`BiliLite.gRPC`](https://github.com/ywmoyue/biliuwp-lite/tree/dev/src/BiliLite.gRPC?utm_source=chatgpt.com).

GRPC-01 — Port the transport abstractions.

- Adapt the source's request-header factory and Android/iOS header models to the target's existing gRPC request class.
- Keep the existing nine protobuf definitions; do not copy all 128 source definitions as a single change.
- Add generated protobuf classes and corresponding service methods by feature.

GRPC-02 — Add service methods incrementally.

Start with the source's `GrpcService` methods for space archive search and dynamic feeds/details. Add other methods only when the UI or a requested feature needs them.

Acceptance: test successful responses, invalid or empty replies, timeouts, network errors, and authenticated requests. Confirm that existing playback and API requests are unaffected.

### Danmaku

Source references: [`DanmakuApi.cs`](https://github.com/ywmoyue/biliuwp-lite/blob/dev/src/BiliLite.UWP/Models/Requests/Api/DanmakuApi.cs?utm_source=chatgpt.com), [`IDanmakuController.cs`](https://github.com/ywmoyue/biliuwp-lite/blob/dev/src/BiliLite.UWP/Services/Interfaces/IDanmakuController.cs?utm_source=chatgpt.com).

DM-01 — Port the useful capabilities, not the whole renderer stack.

- Add the dedicated danmaku like/unlike API.
- Improve the current danmaku list and settings UI.
- If multiple rendering engines are required, introduce `IDanmakuController` and wrap the existing `NSDanmaku` implementation first.
- Add the alternative FrostMaster renderer only as a separate, optional change, with dependency and performance testing.

Acceptance: existing danmaku rendering, filtering, sending, importing, and settings still work. Test the new API independently of renderer selection.

### Notifications

Source references: [`PlayerToastService.cs`](https://github.com/ywmoyue/biliuwp-lite/blob/dev/src/BiliLite.UWP/Services/PlayerToastService.cs?utm_source=chatgpt.com) and [`LiveTileService.cs`](https://github.com/ywmoyue/biliuwp-lite/blob/dev/src/BiliLite.UWP/Services/Notification/LiveTileService.cs?utm_source=chatgpt.com).

NOTIFY-01 — Port player overlay toasts first.

- Add and register `PlayerToastService`.
- Integrate it with the existing player controls for volume, brightness, progress and temporary messages.
- Correct the source's empty `ShowToast()` implementation if that method is required; do not port it expecting working behavior.

NOTIFY-02 — Optional live tiles.

If live tiles are a product requirement, port the background task, register it in the packaging manifest, and fix the async task lifecycle so tile updates finish before the background task completes its deferral. Keep this separate from the in-player toast work.

Acceptance: overlay notifications appear and disappear correctly; background tile updates complete reliably and do not prevent the application from launching.
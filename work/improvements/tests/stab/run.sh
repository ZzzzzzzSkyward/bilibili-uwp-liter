#!/bin/sh
# Stability report item 3: when the player closes, the separate audio element must be
# detached from its own MediaPlayer. The routine used to detach mediaPlayerVideo twice,
# leaving mediaPlayerAudio bound to a player that the same method then disposes.
set -eu
repo="$(cd -- "$(dirname -- "$0")/../../../.." && pwd)"
player="$repo/src/BiliLite.UWP/Controls/Player.xaml.cs"

fail() {
	printf 'FAIL %s\n' "$1" >&2
	exit 1
}

[ -f "$player" ] || fail "source file missing: src/BiliLite.UWP/Controls/Player.xaml.cs"

# Body of ClosePlay() only: from the signature to its 8-space-indented closing brace.
body="$(awk '
	{ sub(/\r$/, "") }
	/public void ClosePlay\(\)/ { inside = 1; next }
	inside && /^        \}$/ { exit }
	inside { print }
' "$player")"

[ -n "$body" ] || fail "could not locate the ClosePlay() body in Player.xaml.cs"

audio_guard="$(printf '%s\n' "$body" | grep -n 'if (mediaPlayerAudio\.MediaPlayer != null)' | cut -d: -f1 || true)"
[ -n "$audio_guard" ] || fail "ClosePlay() no longer guards the audio element: if (mediaPlayerAudio.MediaPlayer != null)"

audio_detach="$(printf '%s\n' "$body" | grep -n 'mediaPlayerAudio\.SetMediaPlayer(null);' | cut -d: -f1 || true)"

video_detach="$(printf '%s\n' "$body" | grep -n 'mediaPlayerVideo\.SetMediaPlayer(null);' | cut -d: -f1 || true)"
[ -n "$video_detach" ] || fail "ClosePlay() no longer detaches the video element: mediaPlayerVideo.SetMediaPlayer(null)"

# 1. The audio branch detaches the audio element, exactly once, inside that branch.
[ "$(printf '%s\n' "$audio_detach" | grep -c . || true)" -eq 1 ] \
	|| fail "ClosePlay() must call mediaPlayerAudio.SetMediaPlayer(null) exactly once (found $(printf '%s\n' "$audio_detach" | grep -c . || true))"
[ "$audio_detach" -gt "$audio_guard" ] \
	|| fail "mediaPlayerAudio.SetMediaPlayer(null) is not inside the audio branch (line $audio_detach vs guard $audio_guard)"
[ "$audio_detach" -le "$((audio_guard + 3))" ] \
	|| fail "mediaPlayerAudio.SetMediaPlayer(null) is not the body of the audio branch (line $audio_detach, guard $audio_guard)"

# 2. No second video detach: every mediaPlayerVideo.SetMediaPlayer(null) precedes the audio guard.
for line in $video_detach; do
	[ "$line" -lt "$audio_guard" ] \
		|| fail "a second mediaPlayerVideo.SetMediaPlayer(null) remains in the audio branch (line $line, audio guard $audio_guard)"
done

# 3. The video detach before the audio branch is still present.
first_video="$(printf '%s\n' "$video_detach" | sort -n | sed -n 1p)"
[ -n "$first_video" ] && [ "$first_video" -lt "$audio_guard" ] \
	|| fail "the video detach is no longer present before the audio branch (video lines: $(printf '%s' "$video_detach" | tr '\n' ' '), audio guard $audio_guard)"

printf 'PASS 3 assertions\n'

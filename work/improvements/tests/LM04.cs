using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiliLite.Controls;
using BiliLite.Models.LocalMedia;
using BiliLite.Modules.Player.Playurl;
using BiliLite.Services.LocalMedia;
using Windows.Storage;

internal static class LM04
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static LocalMediaPlaybackTrack MakeTrack(string id, string label, string path)
    {
        return new LocalMediaPlaybackTrack
        {
            Track = new LocalMediaTrack { Id = id, Label = label, Path = path },
            File = new StorageFile { Path = path, Name = id + ".media" }
        };
    }

    private static LocalPlayInfo MakeInfo(LocalMediaPlaybackTrack video, LocalMediaPlaybackTrack nextVideo,
        LocalMediaPlaybackTrack audio, LocalMediaPlaybackTrack nextAudio)
    {
        var info = new LocalPlayInfo
        {
            Info = new BiliPlayUrlInfo { QualityName = video.Track.Label, DashInfo = new BiliDashPlayUrlInfo() },
            VideoTracks = new List<LocalMediaPlaybackTrack> { video, nextVideo },
            AudioTracks = new List<LocalMediaPlaybackTrack> { audio, nextAudio }
        };
        LocalMediaTrackSelection.SelectVideo(info, video);
        LocalMediaTrackSelection.SelectAudio(info, audio);
        return info;
    }

    public static async Task Main()
    {
        var video1 = MakeTrack("v1", "1080p", "video/1080.mkv");
        var video2 = MakeTrack("v2", "720p", "video/720.mp4");
        var audio1 = MakeTrack("a1", "Japanese", "audio/ja.flac");
        var audio2 = MakeTrack("a2", "English", "audio/en.m4a");

        var info = MakeInfo(video1, video2, audio1, audio2);
        var position = -1d;
        var autoplay = true;
        var opens = 0;
        var pausedAtZero = await LocalMediaTrackSwitchCoordinator.SwitchAsync(info, false, video2,
            (savedPosition, wasPlaying) => { position = savedPosition; autoplay = wasPlaying; }, 0, false,
            () =>
            {
                opens++;
                Check(position == 0 && !autoplay, "paused switch opener must receive exact zero position and paused state");
                return Task.FromResult(new LocalMediaTrackOpenResult { Success = true });
            });
        Check(pausedAtZero.Success && opens == 1, "paused video switch should open once");
        Check(ReferenceEquals(info.SelectedVideoTrack, video2) && ReferenceEquals(info.Info.DashInfo.Video.file, video2.File), "video switch must update the selected video source");
        Check(ReferenceEquals(info.SelectedAudioTrack, audio1) && ReferenceEquals(info.Info.DashInfo.Audio.file, audio1.File), "video switch must leave audio selection unchanged");

        info = MakeInfo(video1, video2, audio1, audio2);
        position = -1;
        autoplay = false;
        opens = 0;
        var playingAudio = await LocalMediaTrackSwitchCoordinator.SwitchAsync(info, true, audio2,
            (savedPosition, wasPlaying) => { position = savedPosition; autoplay = wasPlaying; }, 23.75, true,
            () =>
            {
                opens++;
                Check(position == 23.75 && autoplay, "playing audio switch opener must receive exact position and playing state");
                return Task.FromResult(new LocalMediaTrackOpenResult { Success = true });
            });
        Check(playingAudio.Success && opens == 1, "playing audio switch should open once");
        Check(ReferenceEquals(info.SelectedAudioTrack, audio2) && ReferenceEquals(info.Info.DashInfo.Audio.file, audio2.File), "audio switch must update the selected audio source");
        Check(ReferenceEquals(info.SelectedVideoTrack, video1) && ReferenceEquals(info.Info.DashInfo.Video.file, video1.File), "audio switch must leave video selection unchanged");

        info = MakeInfo(video1, video2, audio1, audio2);
        var originalVideoFile = info.Info.DashInfo.Video.file;
        var originalAudioFile = info.Info.DashInfo.Audio.file;
        position = -1;
        autoplay = true;
        opens = 0;
        var failedThenRestored = await LocalMediaTrackSwitchCoordinator.SwitchAsync(info, true, null,
            (savedPosition, wasPlaying) => { position = savedPosition; autoplay = wasPlaying; }, 0, false,
            () =>
            {
                opens++;
                Check(position == 0 && !autoplay, "failed and rollback opens must both retain zero position and paused state");
                return Task.FromResult(opens == 1
                    ? new LocalMediaTrackOpenResult { Success = false, Message = "unsupported codec", Detail = "engine detail" }
                    : new LocalMediaTrackOpenResult { Success = true });
            });
        Check(!failedThenRestored.Success && opens == 2 && failedThenRestored.OpenResult.Message == "unsupported codec", "immediate failure should reopen the prior source exactly once");
        Check(ReferenceEquals(info.SelectedVideoTrack, video1) && ReferenceEquals(info.Info.DashInfo.Video.file, originalVideoFile), "failed audio switch must roll back video model and file handle");
        Check(ReferenceEquals(info.SelectedAudioTrack, audio1) && ReferenceEquals(info.Info.DashInfo.Audio.file, originalAudioFile), "failed audio switch must roll back audio model and file handle");

        var audioOptions = LocalMediaTrackSelection.AudioOptions(info);
        Check(audioOptions.Count == 3 && audioOptions[0].IsEmbeddedAudio && audioOptions[0].Label == "视频内置音频", "audio selector must expose embedded audio with external tracks");
        Console.WriteLine("LM-04 actual-source switch coordinator probes passed.");
    }
}

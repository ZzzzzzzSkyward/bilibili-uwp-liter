using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Controls;

namespace BiliLite.Services.LocalMedia
{
    public sealed class LocalMediaTrackSelectionOption
    {
        public string Label { get; set; }
        public LocalMediaPlaybackTrack Track { get; set; }
        public bool IsEmbeddedAudio { get; set; }
    }

    public sealed class LocalMediaTrackOpenResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
    }

    public sealed class LocalMediaTrackSwitchOutcome
    {
        public bool Changed { get; set; }
        public bool Success { get; set; }
        public LocalMediaTrackOpenResult OpenResult { get; set; }
        public Exception OpenException { get; set; }
        public LocalMediaTrackOpenResult RestoreResult { get; set; }
        public Exception RestoreException { get; set; }
    }

    public static class LocalMediaTrackSwitchCoordinator
    {
        public static async Task<LocalMediaTrackSwitchOutcome> SwitchAsync(
            LocalPlayInfo info,
            bool isAudio,
            LocalMediaPlaybackTrack requestedTrack,
            Action<double, bool> restorePlaybackState,
            double position,
            bool wasPlaying,
            Func<Task<LocalMediaTrackOpenResult>> open)
        {
            var previousVideo = LocalMediaTrackSelection.GetVideo(info);
            var previousAudio = LocalMediaTrackSelection.GetAudio(info);
            if (isAudio ? ReferenceEquals(previousAudio, requestedTrack) : ReferenceEquals(previousVideo, requestedTrack))
                return new LocalMediaTrackSwitchOutcome { Success = true };

            if (isAudio)
                LocalMediaTrackSelection.SelectAudio(info, requestedTrack);
            else
                LocalMediaTrackSelection.SelectVideo(info, requestedTrack);

            restorePlaybackState(position, wasPlaying);
            var outcome = new LocalMediaTrackSwitchOutcome { Changed = true };
            try
            {
                outcome.OpenResult = await open();
                if (outcome.OpenResult?.Success == true)
                {
                    outcome.Success = true;
                    return outcome;
                }
            }
            catch (Exception exception)
            {
                outcome.OpenException = exception;
            }

            LocalMediaTrackSelection.Restore(info, previousVideo, previousAudio);
            restorePlaybackState(position, wasPlaying);
            try
            {
                outcome.RestoreResult = await open();
            }
            catch (Exception exception)
            {
                outcome.RestoreException = exception;
            }
            return outcome;
        }
    }

    public static class LocalMediaTrackSelection
    {
        public static IList<LocalMediaTrackSelectionOption> VideoOptions(LocalPlayInfo info)
        {
            return (info?.VideoTracks ?? new List<LocalMediaPlaybackTrack>())
                .Select(track => new LocalMediaTrackSelectionOption
                {
                    Label = TrackLabel(track),
                    Track = track
                }).ToList();
        }

        public static IList<LocalMediaTrackSelectionOption> AudioOptions(LocalPlayInfo info)
        {
            var options = new List<LocalMediaTrackSelectionOption>
            {
                new LocalMediaTrackSelectionOption { Label = "视频内置音频", IsEmbeddedAudio = true }
            };
            options.AddRange((info?.AudioTracks ?? new List<LocalMediaPlaybackTrack>()).Select(track =>
                new LocalMediaTrackSelectionOption { Label = TrackLabel(track), Track = track }));
            return options;
        }

        public static LocalMediaPlaybackTrack GetVideo(LocalPlayInfo info) => info?.SelectedVideoTrack;
        public static LocalMediaPlaybackTrack GetAudio(LocalPlayInfo info) => info?.SelectedAudioTrack;

        public static void SelectVideo(LocalPlayInfo info, LocalMediaPlaybackTrack track)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (track == null || !info.VideoTracks.Contains(track)) throw new ArgumentException("The video track does not belong to this item.", nameof(track));
            info.SelectedVideoTrack = track;
            info.Info.DashInfo.Video = ToDashItem(track, true);
            info.Info.QualityName = TrackLabel(track);
            info.Quality = info.Info.QualityName;
        }

        public static void SelectAudio(LocalPlayInfo info, LocalMediaPlaybackTrack track)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (track != null && (info.AudioTracks == null || !info.AudioTracks.Contains(track)))
                throw new ArgumentException("The audio track does not belong to this item.", nameof(track));
            info.SelectedAudioTrack = track;
            info.Info.DashInfo.Audio = track == null ? null : ToDashItem(track, false);
        }

        public static void Restore(LocalPlayInfo info, LocalMediaPlaybackTrack video, LocalMediaPlaybackTrack audio)
        {
            SelectVideo(info, video);
            SelectAudio(info, audio);
        }

        public static string TrackLabel(LocalMediaPlaybackTrack track)
        {
            return track?.Track?.Label ?? track?.Track?.Quality ?? track?.Track?.Language ?? track?.Track?.Id ?? track?.File?.Name ?? "Track";
        }

        private static BiliLite.Modules.Player.Playurl.BiliDashItem ToDashItem(LocalMediaPlaybackTrack track, bool isVideo)
        {
            return new BiliLite.Modules.Player.Playurl.BiliDashItem
            {
                IsVideo = isVideo,
                Url = track.File.Path,
                file = track.File
            };
        }
    }
}

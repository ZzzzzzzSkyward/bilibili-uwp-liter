using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Models.LocalMedia;
using NSDanmaku.Model;
using Windows.Storage;

namespace BiliLite.Services.LocalMedia
{
    /// <summary>Loads enabled XML tracks through the existing Bilibili XML parser.</summary>
    public static class DanmakuTrackLoader
    {
        public static async Task<List<DanmakuModel>> LoadAsync(IEnumerable<LocalMediaPlaybackTrack> enabledTracks, Func<StorageFile, Task<List<DanmakuModel>>> parseFile)
        {
            if (parseFile == null) throw new ArgumentNullException(nameof(parseFile));
            var merged = new List<DanmakuModel>();
            foreach (var track in enabledTracks ?? Enumerable.Empty<LocalMediaPlaybackTrack>())
            {
                if (track?.File == null) throw new LocalMediaTrackLoadException("file-resolution", track?.Track?.Id ?? "unknown", new InvalidOperationException("A selected danmaku track has no resolved file."));
                try
                {
                    var comments = await parseFile(track.File);
                    if (comments != null) merged.AddRange(comments);
                }
                catch (Exception exception)
                {
                    throw new LocalMediaTrackLoadException("danmaku-parse", track.Track.Id, exception);
                }
            }
            return merged.OrderBy(comment => comment.time_s).ToList();
        }
    }
}

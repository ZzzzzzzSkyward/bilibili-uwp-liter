using System;
using System.Collections.Generic;
using System.Globalization;

namespace BiliLite.Models.Common
{
    /// <summary>Which slice of the loaded danmaku pool the list shows.</summary>
    public enum DanmakuListMode
    {
        /// <summary>Comments within ten seconds of the playhead.</summary>
        Recent,

        /// <summary>Every comment currently loaded in the pool.</summary>
        All,
    }

    /// <summary>
    /// One loaded comment as the list shows it: the server ids the like API needs, the text and
    /// the time. Deliberately free of renderer and WinRT types so the list rules stay testable.
    /// </summary>
    public sealed class DanmakuListRow
    {
        /// <summary>Server side danmaku id, empty for comments that only exist locally.</summary>
        public string RowId { get; set; }

        /// <summary>Author hash, as the pool already carries it.</summary>
        public string SendId { get; set; }

        public string Text { get; set; }

        /// <summary>Seconds from the start of the video.</summary>
        public double Time { get; set; }

        /// <summary>True when this row may send a like to the server right now.</summary>
        public bool CanLike { get; set; }

        /// <summary>True when this row was liked from the list while the current item was open.</summary>
        public bool LikedInSession { get; set; }

        public bool HasServerId => !string.IsNullOrEmpty(RowId);

        public bool ShowLike => CanLike && !LikedInSession;

        public bool ShowCancelLike => CanLike && LikedInSession;

        public string TimeText
        {
            get
            {
                var total = Time > 0 ? (int)Math.Floor(Time) : 0;
                var hours = total / 3600;
                var minutes = (total % 3600) / 60;
                var seconds = total % 60;
                return hours > 0
                    ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", hours, minutes, seconds)
                    : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    /// <summary>
    /// Builds the danmaku list from the comments the player already has: no download, no second
    /// renderer, only a window over the loaded pool plus the per-row action rules.
    /// </summary>
    public static class DanmakuListSnapshot
    {
        /// <summary>A comment counts as recent inside this many seconds either side of the playhead.</summary>
        public const int RecentWindowSeconds = 10;

        public static bool IsNearPlayhead(double time, double position)
        {
            return time > position - RecentWindowSeconds && time < position + RecentWindowSeconds;
        }

        /// <summary>
        /// Returns the rows for the mode, oldest first. Like is offered only when the playback is
        /// online and the comment carries a server id, so locally inserted comments stay copy-only.
        /// </summary>
        public static List<DanmakuListRow> Build(IEnumerable<DanmakuListRow> loadedRows, double position,
            DanmakuListMode mode, bool onlinePlayback)
        {
            var rows = new List<DanmakuListRow>();
            if (loadedRows == null) return rows;
            foreach (var row in loadedRows)
            {
                if (row == null) continue;
                if (mode == DanmakuListMode.Recent && !IsNearPlayhead(row.Time, position)) continue;
                row.CanLike = onlinePlayback && row.HasServerId;
                if (!row.CanLike) row.LikedInSession = false;
                rows.Add(row);
            }
            rows.Sort(CompareByTime);
            return rows;
        }

        /// <summary>Marks the rows liked from this list for the current item, dropping stale marks.</summary>
        public static void ApplySessionLikes(IEnumerable<DanmakuListRow> rows, ICollection<string> likedRowIds)
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row == null) continue;
                row.LikedInSession = row.CanLike && likedRowIds != null && likedRowIds.Contains(row.RowId);
            }
        }

        private static int CompareByTime(DanmakuListRow left, DanmakuListRow right)
        {
            var byTime = left.Time.CompareTo(right.Time);
            if (byTime != 0) return byTime;
            return string.CompareOrdinal(left.Text ?? string.Empty, right.Text ?? string.Empty);
        }
    }
}

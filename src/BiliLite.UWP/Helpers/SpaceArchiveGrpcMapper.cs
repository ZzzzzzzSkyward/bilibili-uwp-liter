using System;
using System.Collections.Generic;
using Bilibili.App.Interface.V1;
using BiliLite.Modules.User.UserDetail;

namespace BiliLite.Helpers
{
    /// <summary>
    /// Adapts the space archive-search results returned by the sibling gRPC API to
    /// the target app's existing submission item model.
    /// </summary>
    public static class SpaceArchiveGrpcMapper
    {
        public static List<SubmitVideoItemModel> MapArchiveItems(IEnumerable<Arc> archives)
        {
            var result = new List<SubmitVideoItemModel>();
            if (archives == null)
            {
                return result;
            }

            foreach (var item in archives)
            {
                var archive = item?.Archive;
                if (archive == null)
                {
                    continue;
                }

                result.Add(new SubmitVideoItemModel()
                {
                    aid = archive.Aid.ToString(),
                    title = StripKeywordHighlight(archive.Title),
                    pic = archive.Pic ?? "",
                    description = archive.Desc ?? "",
                    author = archive.Author?.Name ?? "",
                    length = FormatDuration(archive.Duration),
                    created = archive.Ctime,
                    play = (archive.Stat?.View ?? 0).ToString(),
                    comment = ToInt(archive.Stat?.Reply ?? 0),
                    video_review = ToInt(archive.Stat?.Danmaku ?? 0),
                });
            }

            return result;
        }

        private static string StripKeywordHighlight(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return "";
            }

            return title.Replace("<em class=\"keyword\">", "").Replace("</em>", "");
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var duration = TimeSpan.FromSeconds(seconds);
            return seconds >= 3600
                ? duration.ToString(@"hh\:mm\:ss")
                : duration.ToString(@"mm\:ss");
        }

        private static int ToInt(long value)
        {
            return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
        }
    }
}

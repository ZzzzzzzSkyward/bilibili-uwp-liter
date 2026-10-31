using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 视频详情里的合集（ugc_season）
    /// </summary>
    public class VideoUgcSeason
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public string Cover { get; set; }

        public string Intro { get; set; }

        public List<VideoUgcSeasonSection> Sections { get; set; }

        [JsonProperty("stat")]
        public VideoUgcSeasonStat Stat { get; set; }
    }

    public class VideoUgcSeasonStat
    {
        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        public long View { get; set; }

        public long Danmaku { get; set; }

        public long Reply { get; set; }

        [JsonProperty("fav")]
        public long Favorite { get; set; }

        public long Coin { get; set; }

        public long Share { get; set; }

        public long Like { get; set; }

        public long Mtime { get; set; }
    }

    public class VideoUgcSeasonSection
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public int Type { get; set; }

        public List<VideoUgcSeasonSectionEpisode> Episodes { get; set; }
    }

    public class VideoUgcSeasonSectionEpisode
    {
        private string m_cover;
        private VideoAuthor m_author;

        public long Id { get; set; }

        [JsonProperty("section_id")]
        public long SectionId { get; set; }

        public string Aid { get; set; }

        public string Bvid { get; set; }

        public string Cid { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// 封面，合集分集不带 cover 时回退到 arc.pic
        /// </summary>
        public string Cover
        {
            get { return m_cover ?? Arc?.Pic; }
            set { m_cover = value; }
        }

        /// <summary>
        /// UP主，合集分集不带 author 时回退到 arc.author
        /// </summary>
        public VideoAuthor Author
        {
            get { return m_author ?? Arc?.Author; }
            set { m_author = value; }
        }

        public VideoUgcSeasonSectionEpisodeArc Arc { get; set; }

        public VideoUgcSeasonSectionEpisodePage Page { get; set; }
    }

    /// <summary>
    /// 合集分集简化的视频信息，只保留封面和UP主
    /// </summary>
    public class VideoUgcSeasonSectionEpisodeArc
    {
        public string Pic { get; set; }

        public VideoAuthor Author { get; set; }
    }

    public class VideoUgcSeasonSectionEpisodePage
    {
        public long Duration { get; set; }
    }

    public class VideoAuthor
    {
        public long Mid { get; set; }

        public string Name { get; set; }

        public string Face { get; set; }
    }

    /// <summary>
    /// 合集的播放顺序:详情里的 Sections 就是分区的顺序,分区里 Episodes 就是分集顺序。
    /// 纯函数,便于在 UWP 之外验证连播顺序。
    /// </summary>
    public static class VideoUgcSeasonOrder
    {
        /// <summary>
        /// 按分区、分集顺序展开的视频 aid;没有 aid 的分集不参与连播
        /// </summary>
        public static List<string> AidOrder(VideoUgcSeason season)
        {
            var aids = new List<string>();
            if (season?.Sections == null) return aids;

            foreach (var section in season.Sections)
            {
                if (section?.Episodes == null) continue;
                foreach (var episode in section.Episodes)
                {
                    if (episode == null || string.IsNullOrEmpty(episode.Aid)) continue;
                    aids.Add(episode.Aid);
                }
            }
            return aids;
        }

        /// <summary>
        /// 当前视频在合集里的下一个 aid;已是最后一个或不在合集里时返回 null
        /// </summary>
        public static string NextAid(VideoUgcSeason season, string currentAid)
        {
            if (string.IsNullOrEmpty(currentAid)) return null;

            var aids = AidOrder(season);
            var index = aids.IndexOf(currentAid);
            if (index < 0 || index == aids.Count - 1) return null;
            return aids[index + 1];
        }
    }
}

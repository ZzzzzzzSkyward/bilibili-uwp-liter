using System;
using System.Collections.Generic;
using System.Globalization;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 目录里的一段:当前视频的分P、详情里的合集、打开时的播放列表
    /// </summary>
    public enum PlayDirectorySectionKind
    {
        Parts = 0,
        UgcSeason = 1,
        Playlist = 2,
    }

    /// <summary>
    /// 目录的一行。身份是 Key(分P用 cid,合集/播放列表用 aid),Ordinal 只是展示位置;
    /// 标题重复、翻页追加、封面裁剪都不会改变 Key。
    /// </summary>
    public sealed class PlayDirectoryRow
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
        /// <summary>
        /// 次要信息:合集的分区名、UP主
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// 位置文字,例如 合集 7/24、第7项;当前行会再带上分P位置
        /// </summary>
        public string PositionText { get; set; }
        public string Duration { get; set; }
        /// <summary>
        /// 1 起的序号,只是展示用
        /// </summary>
        public int Ordinal { get; set; }
        public bool IsCurrent { get; set; }
        /// <summary>
        /// 当前行的文字标记("正在播放"),不是当前行时为空;界面直接按这个字符串显隐
        /// </summary>
        public string CurrentBadge { get; set; }
    }

    public sealed class PlayDirectorySection
    {
        public PlayDirectorySectionKind Kind { get; set; }
        public string Title { get; set; }
        public IList<PlayDirectoryRow> Rows { get; set; }
    }

    /// <summary>
    /// 一份完整的目录。Caption 是播放器"目录"按钮上的短标签,CountText 是页头的数量说明,
    /// CurrentPositionText 是"现在播到哪"的一句话;三者都跟着活动来源。
    /// </summary>
    public sealed class PlayDirectoryModel
    {
        public string SourceTitle { get; set; }
        public string Caption { get; set; }
        public string CountText { get; set; }
        /// <summary>
        /// 活动分段里当前行的 Key,定位正在播放时用它
        /// </summary>
        public string CurrentKey { get; set; }
        public string CurrentPositionText { get; set; }
        /// <summary>
        /// 当前分P的 P2/3;只有一个分P时为空,免得页头一直写 P1/1
        /// </summary>
        public string CurrentPartText { get; set; }
        /// <summary>
        /// 页头那行"连播来源：… · 接下来：…"
        /// </summary>
        public string HeaderText { get; set; }
        public PlayDirectorySectionKind ActiveKind { get; set; }
        public IList<PlayDirectorySection> Sections { get; set; }

        /// <summary>
        /// 一段都没有时不弹空抽屉
        /// </summary>
        public bool HasSections
        {
            get { return Sections != null && Sections.Count > 0; }
        }
    }

    /// <summary>
    /// 分P的最小输入:只有目录需要的字段,构建器不碰播放器的 PlayInfo
    /// </summary>
    public sealed class PlayDirectoryPart
    {
        public string Cid { get; set; }
        public string Title { get; set; }
        /// <summary>
        /// 秒
        /// </summary>
        public long DurationSeconds { get; set; }
    }

    /// <summary>
    /// 目录的组装。纯函数:只读传进来的顺序、合集和队列,不取页、不改播放、不决定下一步播什么。
    /// </summary>
    public static class PlayDirectoryBuilder
    {
        public const string PartsSectionTitle = "当前视频分P";
        public const string CollectionSectionTitle = "合集";
        public const string PlaylistSectionTitle = "播放列表";
        public const string NowPlayingBadge = "正在播放";

        /// <summary>
        /// 当前视频的分P。只有一个分P的普通视频没有这一段;互动视频除外。
        /// </summary>
        public static PlayDirectoryModel FromParts(IList<PlayDirectoryPart> parts, int currentIndex, string sourceTitle, bool interactionVideo)
        {
            var count = parts == null ? 0 : parts.Count;
            var index = count == 0 ? -1 : Math.Min(Math.Max(0, currentIndex), count - 1);

            var rows = new List<PlayDirectoryRow>();
            var currentKey = "";
            for (var i = 0; i < count; i++)
            {
                var part = parts[i];
                var key = PartKey(part, i);
                if (i == index) currentKey = key;
                rows.Add(new PlayDirectoryRow
                {
                    Key = key,
                    Title = part == null ? "" : part.Title,
                    Duration = FormatDuration(part == null ? 0 : part.DurationSeconds),
                    Ordinal = i + 1,
                    IsCurrent = i == index,
                    CurrentBadge = i == index ? NowPlayingBadge : "",
                });
            }

            var partText = PartText(index, count);
            var model = new PlayDirectoryModel
            {
                SourceTitle = sourceTitle ?? "",
                Caption = Caption(PlayDirectorySectionKind.Parts, index + 1, count),
                CountText = "共" + count.ToString(CultureInfo.InvariantCulture) + "P",
                CurrentKey = currentKey,
                CurrentPositionText = partText,
                CurrentPartText = count > 1 ? partText : "",
                ActiveKind = PlayDirectorySectionKind.Parts,
                Sections = new List<PlayDirectorySection>(),
            };

            if (count > 1 || interactionVideo)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.Parts,
                    Title = PartsSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 详情里的合集。行顺序和连播顺序一样(分区、分集),身份是 aid。
        /// 合集分集只带时长,没有别的视频的分P数,所以位置文字只写视频位置。
        /// </summary>
        public static PlayDirectoryModel FromUgcSeason(VideoUgcSeason season, string currentAid)
        {
            var episodes = new List<EpisodeRow>();
            if (season != null && season.Sections != null)
            {
                foreach (var section in season.Sections)
                {
                    if (section == null || section.Episodes == null) continue;
                    foreach (var episode in section.Episodes)
                    {
                        if (episode == null || string.IsNullOrEmpty(episode.Aid)) continue;
                        episodes.Add(new EpisodeRow()
                        {
                            Aid = episode.Aid,
                            SectionTitle = section.Title,
                            Title = episode.Title,
                            Author = episode.Author == null ? null : episode.Author.Name,
                            Cover = episode.Cover,
                            Duration = episode.Page == null ? 0 : episode.Page.Duration,
                        });
                    }
                }
            }

            var total = episodes.Count;
            var rows = new List<PlayDirectoryRow>();
            var currentIndex = -1;
            for (var i = 0; i < total; i++)
            {
                var episode = episodes[i];
                var isCurrent = currentIndex < 0 && !string.IsNullOrEmpty(currentAid)
                    && string.Equals(episode.Aid, currentAid, StringComparison.Ordinal);
                if (isCurrent) currentIndex = i;
                rows.Add(new PlayDirectoryRow()
                {
                    Key = episode.Aid,
                    Title = episode.Title,
                    Cover = episode.Cover,
                    Subtitle = Join(episode.SectionTitle, episode.Author),
                    PositionText = PositionOf(CollectionSectionTitle, i + 1, total),
                    Duration = FormatDuration(episode.Duration),
                    Ordinal = i + 1,
                    IsCurrent = isCurrent,
                    CurrentBadge = isCurrent ? NowPlayingBadge : "",
                });
            }

            var model = new PlayDirectoryModel()
            {
                SourceTitle = season == null ? "" : season.Title,
                Caption = Caption(PlayDirectorySectionKind.UgcSeason, currentIndex + 1, total),
                CountText = "共" + total.ToString(CultureInfo.InvariantCulture) + "个视频",
                CurrentKey = currentIndex < 0 ? "" : rows[currentIndex].Key,
                CurrentPositionText = currentIndex < 0 ? "" : PositionOf(CollectionSectionTitle, currentIndex + 1, total),
                CurrentPartText = "",
                ActiveKind = PlayDirectorySectionKind.UgcSeason,
                Sections = new List<PlayDirectorySection>(),
            };

            if (total > 0)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.UgcSeason,
                    Title = CollectionSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 打开视频时带的队列。带了翻页状态的是远程 medialist:数量按已加载和已知总数写,
        /// 不把已加载数冒充总数;没有翻页状态的固定队列直接写可播放总量。
        /// </summary>
        public static PlayDirectoryModel FromPlaylist(VideoPlaylist playlist, MediaListPagingState paging)
        {
            var items = playlist == null ? null : playlist.Playlist;
            var total = items == null ? 0 : items.Count;
            var index = total == 0 ? -1 : Math.Min(Math.Max(0, playlist.Index), total - 1);

            var rows = new List<PlayDirectoryRow>();
            for (var i = 0; i < total; i++)
            {
                var item = items[i];
                rows.Add(new PlayDirectoryRow()
                {
                    Key = item == null ? "" : item.ID,
                    Title = item == null ? "" : item.Title,
                    Cover = item == null ? "" : item.Cover,
                    Subtitle = item == null ? "" : item.Author,
                    PositionText = "",
                    Duration = item == null ? "" : item.Duration,
                    Ordinal = i + 1,
                    IsCurrent = i == index,
                    CurrentBadge = i == index ? NowPlayingBadge : "",
                });
            }

            var ordinal = index + 1;
            var title = paging != null && !string.IsNullOrEmpty(paging.Title)
                ? paging.Title
                : (playlist != null && !string.IsNullOrEmpty(playlist.Title) ? playlist.Title : MediaListPagingState.DefaultTitle);

            var model = new PlayDirectoryModel()
            {
                SourceTitle = title,
                Caption = Caption(PlayDirectorySectionKind.Playlist, ordinal, total),
                CountText = paging != null
                    ? CountText(ordinal, paging.LoadedCount, paging.HasKnownTotal, paging.TotalCount, paging.HasMore)
                    : FixedCountText(ordinal, total),
                CurrentKey = index < 0 ? "" : rows[index].Key,
                CurrentPositionText = ordinal > 0 ? OrdinalText(ordinal) : "",
                CurrentPartText = "",
                ActiveKind = PlayDirectorySectionKind.Playlist,
                Sections = new List<PlayDirectorySection>(),
            };

            if (total > 0)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.Playlist,
                    Title = PlaylistSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 把三段拼成一份目录:分段顺序固定为分P、合集、播放列表,只收有内容的分段。
        /// 活动分段优先跟随连播来源,没有外层来源时落在分P上。
        /// </summary>
        public static PlayDirectoryModel Combine(PlayDirectoryModel parts, PlayDirectoryModel collection,
            PlayDirectoryModel playlist, WatchSequenceSourceKind activeSource, string nextTargetText)
        {
            var sections = new List<PlayDirectorySection>();
            AppendSections(sections, parts);
            AppendSections(sections, collection);
            AppendSections(sections, playlist);

            var activeKind = ResolveActiveKind(sections, activeSource);
            var activeSection = SectionOf(sections, activeKind);
            var model = ModelOf(activeKind, parts, collection, playlist);
            var partsText = parts == null ? "" : parts.CurrentPartText;

            var positionText = model == null ? "" : model.CurrentPositionText;
            if (activeKind != PlayDirectorySectionKind.Parts && !string.IsNullOrEmpty(partsText))
            {
                positionText = Join(positionText, partsText);
                // 活动分段的当前行也写出分P位置,例如 合集 7/24 · P2/3
                if (activeSection != null && activeSection.Rows != null)
                {
                    foreach (var row in activeSection.Rows)
                    {
                        if (row.IsCurrent) row.PositionText = Join(row.PositionText, partsText);
                    }
                }
            }

            var sourceLabel = SourceLabelOf(activeKind);
            var sourceTitle = model == null ? "" : model.SourceTitle;
            var header = "连播来源：" + sourceLabel;
            if (activeKind != PlayDirectorySectionKind.Parts && !string.IsNullOrEmpty(sourceTitle))
            {
                header = header + "《" + sourceTitle + "》";
            }
            if (!string.IsNullOrEmpty(nextTargetText))
            {
                header = header + " · 接下来：" + nextTargetText;
            }

            return new PlayDirectoryModel()
            {
                SourceTitle = sourceTitle,
                Caption = model == null ? "" : model.Caption,
                CountText = model == null ? "" : model.CountText,
                CurrentKey = model == null ? "" : model.CurrentKey,
                CurrentPositionText = positionText,
                CurrentPartText = partsText,
                HeaderText = header,
                ActiveKind = activeKind,
                Sections = sections,
            };
        }

        /// <summary>
        /// 播放器按钮上的短标签:合集 · 7/24、播放列表 · 第7项、分P · 2/3。
        /// 当前位置不在这一段里(没有当前行)时只写总量。
        /// </summary>
        public static string Caption(PlayDirectorySectionKind kind, int ordinal, int count)
        {
            if (count < 0) count = 0;
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason:
                    return ordinal > 0 && ordinal <= count
                        ? CollectionSectionTitle + " · " + CountPosition(ordinal, count)
                        : CollectionSectionTitle + " · " + count.ToString(CultureInfo.InvariantCulture) + "项";
                case PlayDirectorySectionKind.Playlist:
                    return ordinal > 0
                        ? PlaylistSectionTitle + " · " + OrdinalText(ordinal)
                        : PlaylistSectionTitle;
                default:
                    return count > 0
                        ? "分P · " + CountPosition(Math.Max(1, ordinal), count)
                        : "分P";
            }
        }

        /// <summary>
        /// 远程队列的数量说明:第7项 · 已加载20项 · 共120项 / 总数未知,
        /// 只有翻页真的到头(hasMore 为 false)才写已全部加载。
        /// </summary>
        public static string CountText(int ordinal, int loadedCount, bool hasKnownTotal, int totalCount, bool hasMore)
        {
            if (loadedCount < 0) loadedCount = 0;
            var text = ordinal > 0 ? OrdinalText(ordinal) : "";
            text = Join(text, "已加载" + loadedCount.ToString(CultureInfo.InvariantCulture) + "项");
            text = Join(text, hasKnownTotal
                ? "共" + Math.Max(0, totalCount).ToString(CultureInfo.InvariantCulture) + "项"
                : "总数未知");
            if (!hasMore) text = Join(text, "已全部加载");
            return text;
        }

        /// <summary>
        /// 固定内存队列没有翻页:写可播放总量
        /// </summary>
        public static string FixedCountText(int ordinal, int count)
        {
            if (count < 0) count = 0;
            var total = "共" + count.ToString(CultureInfo.InvariantCulture) + "个可播放视频";
            return ordinal > 0 ? Join(OrdinalText(ordinal), total) : total;
        }

        /// <summary>
        /// 秒数写成 mm:ss,超过一小时写成 hh:mm:ss
        /// </summary>
        public static string FormatDuration(long seconds)
        {
            if (seconds < 0) seconds = 0;
            var duration = TimeSpan.FromSeconds(seconds);
            return seconds >= 3600 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");
        }

        private static string PartKey(PlayDirectoryPart part, int index)
        {
            // 分P的身份是 cid;详情没给 cid 时退到位置,至少不会和别的行撞成同一个 Key
            if (part != null && !string.IsNullOrEmpty(part.Cid)) return part.Cid;
            return "part-" + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string PartText(int index, int count)
        {
            if (count <= 0 || index < 0) return "";
            return "P" + (index + 1).ToString(CultureInfo.InvariantCulture)
                + "/" + count.ToString(CultureInfo.InvariantCulture);
        }

        private static string CountPosition(int ordinal, int count)
        {
            return ordinal.ToString(CultureInfo.InvariantCulture) + "/" + count.ToString(CultureInfo.InvariantCulture);
        }

        private static string PositionOf(string label, int ordinal, int count)
        {
            if (count <= 0) return label;
            return label + " " + CountPosition(ordinal, count);
        }

        private static string OrdinalText(int ordinal)
        {
            return "第" + ordinal.ToString(CultureInfo.InvariantCulture) + "项";
        }

        private static string SourceLabelOf(PlayDirectorySectionKind kind)
        {
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason: return CollectionSectionTitle;
                case PlayDirectorySectionKind.Playlist: return PlaylistSectionTitle;
                default: return "本视频分P";
            }
        }

        private static void AppendSections(List<PlayDirectorySection> target, PlayDirectoryModel model)
        {
            if (model == null || model.Sections == null) return;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Rows == null || section.Rows.Count == 0) continue;
                target.Add(section);
            }
        }

        private static PlayDirectorySectionKind ResolveActiveKind(IList<PlayDirectorySection> sections, WatchSequenceSourceKind activeSource)
        {
            if (activeSource == WatchSequenceSourceKind.UgcSeason && SectionOf(sections, PlayDirectorySectionKind.UgcSeason) != null)
            {
                return PlayDirectorySectionKind.UgcSeason;
            }
            if (activeSource == WatchSequenceSourceKind.Playlist && SectionOf(sections, PlayDirectorySectionKind.Playlist) != null)
            {
                return PlayDirectorySectionKind.Playlist;
            }
            // 活动来源不在目录里时,先开还有什么外层列表;都没有外层列表才落在分P上
            if (SectionOf(sections, PlayDirectorySectionKind.UgcSeason) != null) return PlayDirectorySectionKind.UgcSeason;
            if (SectionOf(sections, PlayDirectorySectionKind.Playlist) != null) return PlayDirectorySectionKind.Playlist;
            return PlayDirectorySectionKind.Parts;
        }

        private static PlayDirectorySection SectionOf(IList<PlayDirectorySection> sections, PlayDirectorySectionKind kind)
        {
            if (sections == null) return null;
            foreach (var section in sections)
            {
                if (section != null && section.Kind == kind) return section;
            }
            return null;
        }

        private static PlayDirectoryModel ModelOf(PlayDirectorySectionKind kind, PlayDirectoryModel parts,
            PlayDirectoryModel collection, PlayDirectoryModel playlist)
        {
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason: return collection ?? parts ?? playlist;
                case PlayDirectorySectionKind.Playlist: return playlist ?? parts ?? collection;
                default: return parts ?? collection ?? playlist;
            }
        }

        private static string Join(string left, string right)
        {
            if (string.IsNullOrEmpty(left)) return right ?? "";
            if (string.IsNullOrEmpty(right)) return left;
            return left + " · " + right;
        }

        private sealed class EpisodeRow
        {
            public string Aid;
            public string SectionTitle;
            public string Title;
            public string Author;
            public string Cover;
            public long Duration;
        }
    }
}

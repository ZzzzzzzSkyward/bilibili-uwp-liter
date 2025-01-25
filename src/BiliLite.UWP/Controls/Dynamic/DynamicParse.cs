using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using BiliLite.Helpers;
using Newtonsoft.Json;
using BiliLite.Models.Dynamic;
using System.Xml;
using BiliLite.Modules;

namespace BiliLite.Controls.Dynamic
{
    public enum DynamicDisplayType
    {
        /// <summary>
        /// 转发
        /// </summary>
        Repost,
        /// <summary>
        /// 文本
        /// </summary>
        Text,
        /// <summary>
        /// 图片
        /// </summary>
        Photo,
        /// <summary>
        /// 视频
        /// </summary>
        Video,
        /// <summary>
        /// 短视频
        /// </summary>
        ShortVideo,
        /// <summary>
        /// 番剧/影视
        /// </summary>
        Season,
        /// <summary>
        /// 音乐
        /// </summary>
        Music,
        /// <summary>
        /// 网页、活动
        /// </summary>
        Web,
        /// <summary>
        /// 文章
        /// </summary>
        Article,
        /// <summary>
        /// 直播
        /// </summary>
        Live,
        /// <summary>
        /// 分享直播
        /// </summary>
        LiveShare,
        /// <summary>
        /// 付费课程
        /// </summary>
        Cheese,
        /// <summary>
        /// 播放列表(公开的收藏夹)
        /// </summary>
        MediaList,
        /// <summary>
        /// 缺失的，动态可能被删除
        /// </summary>
        Miss,
        /// <summary>
        /// 直播预约
        /// </summary>
        Reserve,
        /// <summary>
        /// 其他
        /// </summary>
        Other
    }
    public static class DynamicParse
    {
        //纯文字
        //视频
        //音乐
        //网页
        //专题
        //投票
        //活动
        //图片
        //直播 4308
        public static DynamicDisplayType ParseType(int type)
        {
            switch (type)
            {
                case 1:
                    return DynamicDisplayType.Repost;
                case 2:
                    return DynamicDisplayType.Photo;
                case 4:
                    return DynamicDisplayType.Text;
                case 8:
                    return DynamicDisplayType.Video;
                case 16:
                    return DynamicDisplayType.ShortVideo;
                case 64:
                    return DynamicDisplayType.Article;
                case 256:
                    return DynamicDisplayType.Music;
                case 1024:
                    return DynamicDisplayType.Miss;
                case 512:
                case 4097:
                case 4098:
                case 4099:
                case 4100:
                case 4101:
                    return DynamicDisplayType.Season;
                case 2048:
                case 2049:
                    return DynamicDisplayType.Web;
                case 4308:
                    return DynamicDisplayType.Live;
                case 4200:
                    return DynamicDisplayType.LiveShare;
                case 4300:
                case 4310:
                    return DynamicDisplayType.MediaList;
                case 4303:
                case 4302:
                    return DynamicDisplayType.Cheese;
                default:
                    return DynamicDisplayType.Other;
            }
        }
        //20240527
        public static DynamicDisplayType ParseType2024(string type)
        {
            switch (type)
            {
                case "DYNAMIC_TYPE_WORD":
                    return DynamicDisplayType.Text;
                case "DYNAMIC_TYPE_FORWARD":
                    return DynamicDisplayType.Repost;
                case "DYNAMIC_TYPE_DRAW":
                    return DynamicDisplayType.Photo;
                case "DYNAMIC_TYPE_AV":
                    return DynamicDisplayType.Video;
                case "DYNAMIC_TYPE_ARTICLE":
                    return DynamicDisplayType.Article;
                case "DYNAMIC_TYPE_MUSIC":
                    return DynamicDisplayType.Music;
                case "DYNAMIC_TYPE_NONE":
                    return DynamicDisplayType.Miss;
                case "DYNAMIC_TYPE_PGC_UNION":
                    return DynamicDisplayType.Season;
                case "DYNAMIC_TYPE_UGC_SEASON":
                    return DynamicDisplayType.MediaList;
                case "DYNAMIC_TYPE_LIVE_RCMD":
                    return DynamicDisplayType.Live;
                case "DYNAMIC_TYPE_LIVE":
                    return DynamicDisplayType.LiveShare;
                default:
                    return DynamicDisplayType.Other;
            }
        }
        public static string GetDynamicSubType(string subtype)
        {
            switch (subtype)
            {
                case "ADDITIONAL_TYPE_NONE":
                    return "none";
                case "ADDITIONAL_TYPE_PGC":
                    return "pgc";
                case "ADDITIONAL_TYPE_GOODS"://商品信息
                    return "goods";
                case "ADDITIONAL_TYPE_VOTE":// 投票  716365292050055176
                    return "vote";
                case "ADDITIONAL_TYPE_COMMON":// 一般类型 游戏716357878942793745
                    return "common";
                case "ADDITIONAL_TYPE_MATCH":
                    return "match";
                case "ADDITIONAL_TYPE_UP_RCMD":
                    return "recommend";
                case "ADDITIONAL_TYPE_UGC":// 视频跳转 716489253410832401
                    return "ugc";
                case "ADDITIONAL_TYPE_RESERVE":
                    return "reserve";
                default:
                    return subtype;
            }
        }
        public static RichTextBlock Render(string t)
        {
            try
            {
                var xaml = string.Format(@"<RichTextBlock HorizontalAlignment=""Stretch"" TextWrapping=""Wrap""  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:mc = ""http://schemas.openxmlformats.org/markup-compatibility/2006"" LineHeight=""20"">{0}</RichTextBlock>", t);
                return (RichTextBlock)XamlReader.Load(xaml);
            }
            catch
            {
                Utils.AddALog("无法解析动态 "+t);
            }
            return null;
        }
        public static DynamicItemDisplayOneRowInfo ParseOneRowInfo2024(DynamicItemDisplayModel data, Modules.DynamicCardModel2024 item)
        {
            var info = new DynamicItemDisplayOneRowInfo()
            {
                Cover = item.pic,
                CoverText = item.durationtext,
                Subtitle = item.desc,
                ID = item.aid,
                Desc = item.desc,
                Title = item.title,
            };
            data.Content = Render(item.richtext);
            switch (data.Type)
            {
                case DynamicDisplayType.Text:
                    {
                        if (GetDynamicSubType(item.modules.module_dynamic.additional?.type ?? "") == "reserve")
                        {
                            data.Type = DynamicDisplayType.Reserve;
                            info = new DynamicItemDisplayOneRowInfo()
                            {
                                Title = item.title,
                                Desc = item.modules.module_dynamic.additional.reserve.desc1?.text ?? "" + " " +
                                item.modules.module_dynamic.additional.reserve.desc2?.text ?? "",
                                Subtitle = item.desc,
                                CoverText = item.modules.module_dynamic.additional.reserve.button?.text,
                                Cover = item.modules.module_dynamic.additional.reserve.button?.icon_url,
                                Url = item.modules.module_dynamic.additional.reserve.jump_url ?? item.jumpurl ?? "",
                            };
                            data.Content = Render(Parser.ParseText(info.Subtitle));
                        }
                    }
                    break;
                case DynamicDisplayType.Video:
                    {
                        var duration = item.durationtext ?? "";
                        var coverText = duration;
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic,
                            CoverText = coverText,
                            Subtitle = "播放:" + item.viewtext + " 弹幕:" + item.danmakutext,
                            Tag = "视频",
                            ID = item.aid,
                            Desc = item.desc,
                            Title = item.title,
                        };
                        info.Url = "http://b23.tv/av" + info.ID;
                    }
                    break;
                case DynamicDisplayType.Season:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic,
                            CoverText = "",
                            Subtitle = "播放:" + item.viewtext + " 弹幕:" + item.danmakutext,
                            ID = item.aid,
                            Title = item.title,
                            CoverWidth = 160,
                            AID = item.aid,
                        };
                        if (string.IsNullOrEmpty(info.Title))
                        {
                            info.Title = item.title;
                        }
                        info.Url = item.jumpurl;
                    }
                    break;
                case DynamicDisplayType.Music:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic,
                            CoverParameter = "200w",
                            Subtitle = "播放:" + item.viewtext + " 评论:" + item.comment.ToCountString(),
                            ID = item.aid,
                            Title = item.title,
                            CoverWidth = 80,
                            Tag = "音频",

                        };
                        info.Url = "http://b23.tv/au" + info.ID;
                    }
                    break;
                case DynamicDisplayType.Web:
                    {
                        var cover = item.pic?.ToString() ?? "";
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = cover == "" ? "" : cover + "@200w.jpg",
                            Subtitle = item.desc,
                            ID = item.aid,//fixme
                            Title = item.title,
                            CoverWidth = 80,
                        };
                        info.Url = info.ID.ToString();
                    }
                    break;
                case DynamicDisplayType.Article:
                    {
                        var cover = item.pic ?? "";
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = cover + "@412w_232h_1c.jpg",
                            //CoverText = obj["words"].ToCountString()+"字",
                            Subtitle = "浏览:" + item.viewtext + " 点赞:" + item.like.ToCountString() + " ID:" + item.aid,
                            ID = item.aid,
                            Title = item.title,
                            Desc = item.desc,
                            Tag = "专栏",

                        };
                        info.Url = "https://www.bilibili.com/read/cv" + info.ID;
                    }
                    break;
                case DynamicDisplayType.Live:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic,
                            CoverText = "",
                            Subtitle = "【直播有待修复】parent_area_name" + " · 人气:" + item.viewtext,
                            Tag = "直播",
                            ID = item.aid,
                            Title = item.title,
                        };
                        info.Url = "https://b23.tv/live" + info.ID;
                    }
                    break;
                case DynamicDisplayType.LiveShare:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic,
                            //fixme
                            CoverText = item.modules.module_dynamic.major.live?.live_state == 0 ? "直播已结束" : "",
                            Subtitle = "【直播有待修复】area_v2_name",
                            Tag = "直播",
                            ID = item.aid,
                            Title = item.title,
                        };
                        info.Url = "https://b23.tv/live" + info.ID;
                    }
                    break;
                case DynamicDisplayType.MediaList:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = item.pic ?? "" + "@412w_232h_1c.jpg",
                            Subtitle = "播放:" + item.viewtext + " 点赞:" + item.like,
                            Tag = "合集",
                            ID = item.aid,
                            Title = item.title,
                        };
                        info.Url = item.jumpurl;
                    }
                    break;
                case DynamicDisplayType.Photo:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Desc = item.desc,
                        };
                    }
                    break;
                default:
                    break;
            }
            if (data.Type == DynamicDisplayType.Photo)
            {
                var imgs = new List<string> { };
                var objs = new List<DyanmicItemDisplayImageInfo>() { };
                var imgsrc = item.modules?.module_dynamic?.major?.draw?.items;
                int i = 0;
                if (imgsrc != null)
                    foreach (var img in imgsrc)
                    {
                        if (img.src != null)
                        {
                            imgs.Add(img.src);
                            objs.Add(new DyanmicItemDisplayImageInfo()
                            {
                                ImageUrl = img.src,
                                Height = img.height,
                                Width = img.width,
                                Index = i,
                                //偷懒方法，点击图片时可以获取全部图片信息，好孩子不要学
                                AllImages = imgs,
                            });
                        }
                        i++;
                    }
                data.ImagesInfo = objs;
            }

            if (data.Type == DynamicDisplayType.Repost)
            {
                info = new DynamicItemDisplayOneRowInfo()
                {
                    Cover = item.pic,
                    CoverText = "转发",
                    Subtitle = item.desc,
                    Tag = "转发tag",
                    ID = item.orig.aid,
                    Desc = item.orig.desc,
                    Title = item.orig.title,
                };
                info.Url = data.OriginInfo?[0]?.OneRowInfo?.Url ?? item.jumpurl ?? "";
            }
            return info;
        }

        public static DynamicItemDisplayOneRowInfo ParseOneRowInfo(DynamicDisplayType type, JObject obj)
        {
            DynamicItemDisplayOneRowInfo info = null;
            switch (type)
            {
                case DynamicDisplayType.Video:
                    {
                        var duration = TimeSpan.FromSeconds(obj["duration"].ToInt32());
                        var coverText = duration.ToString(@"mm\:ss");
                        if (duration.TotalHours >= 1)
                        {
                            coverText = duration.ToString(@"hh\:mm\:ss");
                        }
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["pic"].ToString() + "@412w_232h_1c.jpg",
                            CoverText = coverText,
                            Subtitle = "播放:" + obj["stat"]["view"].ToCountString() + " 弹幕:" + obj["stat"]["danmaku"].ToCountString(),
                            Tag = "视频",
                            ID = obj["aid"].ToString(),
                            Desc = obj["desc"].ToString(),
                            Title = obj["title"].ToString(),

                        };
                        info.Url = "http://b23.tv/av" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.Season:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["cover"].ToString() + "@200w.jpg",
                            CoverText = "",
                            Subtitle = "播放:" + obj["play_count"].ToCountString() + " 弹幕:" + obj["bullet_count"].ToCountString(),
                            ID = obj["apiSeasonInfo"]["season_id"].ToString(),
                            Title = obj["new_desc"].ToString(),
                            CoverWidth = 160,
                            AID = obj["aid"].ToString(),
                        };
                        if (string.IsNullOrEmpty(info.Title))
                        {
                            info.Title = obj["apiSeasonInfo"]["title"].ToString();
                        }
                        info.Url = "http://b23.tv/ss" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.Music:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["cover"].ToString() + "@200w.jpg",
                            CoverParameter = "200w",
                            Subtitle = "播放:" + obj["playCnt"].ToCountString() + " 评论:" + obj["replyCnt"].ToCountString(),
                            ID = obj["id"].ToString(),
                            Title = obj["title"].ToString(),
                            CoverWidth = 80,
                            Tag = "音频",

                        };
                        info.Url = "http://b23.tv/au" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.Web:
                    {
                        var cover = obj["sketch"]["cover_url"]?.ToString() ?? "";
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = cover == "" ? "" : cover + "@200w.jpg",
                            Subtitle = obj["sketch"]["desc_text"]?.ToString() ?? "",
                            ID = obj["sketch"]["target_url"]?.ToString() ?? "",
                            Title = obj["sketch"]["title"]?.ToString() ?? "",
                            CoverWidth = 80,
                        };
                        info.Url = info.ID.ToString();
                    }
                    return info;
                case DynamicDisplayType.Article:
                    {
                        var cover = obj["origin_image_urls"]?[0]?.ToString() ?? "";
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = cover + "@412w_232h_1c.jpg",
                            //CoverText = obj["words"].ToCountString()+"字",
                            Subtitle = "浏览:" + obj["stats"]["view"].ToCountString() + " 点赞:" + obj["stats"]["like"].ToCountString(),
                            ID = obj["id"].ToString(),
                            Title = obj["title"].ToString(),
                            Desc = obj["summary"].ToString(),
                            Tag = "专栏",

                        };
                        info.Url = "https://www.bilibili.com/read/cv" + info.ID.ToString();
                    }
                    return info;
                case DynamicDisplayType.Live:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["live_play_info"]["cover"].ToString() + "@412w_232h_1c.jpg",
                            CoverText = "",
                            Subtitle = obj["live_play_info"]["parent_area_name"].ToString() + " · 人气:" + obj["live_play_info"]["online"].ToCountString(),
                            Tag = "直播",
                            ID = obj["live_play_info"]["room_id"].ToString(),
                            Title = obj["live_play_info"]["title"].ToString(),
                        };
                        info.Url = "https://b23.tv/live" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.LiveShare:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["cover"].ToString() + "@412w_232h_1c.jpg",
                            CoverText = obj["live_status"].ToInt32() == 0 ? "直播已结束" : "",
                            Subtitle = obj["area_v2_name"].ToString(),
                            Tag = "直播",
                            ID = obj["roomid"].ToString(),
                            Title = obj["title"].ToString(),
                        };
                        info.Url = "https://b23.tv/live" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.MediaList:
                    {
                        //TODO 合集这部分需要重写
                        //https://t.bilibili.com/625835271145782341
                        if (obj["videos"].ToInt32() == 1)
                        {
                            return ParseOneRowInfo(DynamicDisplayType.Video, obj);
                        }
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["cover"].ToString() + "@412w_232h_1c.jpg",
                            Subtitle = obj["media_count"].ToString() + "个内容",
                            Tag = "收藏夹",
                            ID = obj["id"].ToString(),
                            Title = obj["title"].ToString(),
                        };
                        info.Url = "https://www.bilibili.com/medialist/detail/ml" + info.ID;
                    }
                    return info;
                case DynamicDisplayType.Cheese:
                    {
                        info = new DynamicItemDisplayOneRowInfo()
                        {
                            Cover = obj["cover"].ToString() + "@412w_232h_1c.jpg",
                            Subtitle = obj["subtitle"].ToString(),
                            Tag = "付费课程",
                            ID = obj["id"].ToString(),
                            Title = obj["title"].ToString(),
                        };
                        info.Url = obj["url"].ToString();
                    }
                    return info;
                default:
                    return info;
            }
        }

        public static DyanmicItemDisplayShortVideoInfo ParseShortVideoInfo(JObject obj)
        {
            try
            {
                return new DyanmicItemDisplayShortVideoInfo()
                {
                    Height = obj["item"]["height"].ToInt32(),
                    Width = obj["item"]["width"].ToInt32(),
                    UploadTime = obj["item"]["upload_time"].ToString(),
                    VideoPlayurl = obj["item"]["video_playurl"].ToString(),
                };
            }
            catch (Exception)
            {

                return null;
            }

        }
        /**
         * Command
         * UserCommand=>打开用户页面
         * LotteryCommand=>打开抽奖页面
         * LaunchUrlCommand=>打开网页
         * TagCommand=>打开话题
         **/


        /// <summary>
        /// 评论文本转为RichText
        /// </summary>
        /// <param name="id">动态id</param>
        /// <param name="txt"></param>
        /// <param name="emote"></param>
        /// <param name="extend_json"></param>
        /// <returns></returns>
        public static RichTextBlock StringToRichText(string id, string txt, List<DynamicCardDisplayEmojiInfoItemModel> emote, JObject extend_json)
        {
            if (string.IsNullOrEmpty(txt)) return new RichTextBlock();
            string input = txt;
            try
            {
                //处理特殊字符

                input = input.Replace("&", "&amp;");
                input = input.Replace("<", "&lt;");
                input = input.Replace(">", "&gt;");

                //处理换行
                input = input.Replace("\r\n", "<LineBreak/>");
                input = input.Replace("\n", "<LineBreak/>");
                //处理@
                input = HandleAtAndVote(input, txt, extend_json);
                //处理网页🔗
                input = HandleUrl(input);

                //处理表情
                input = HandleEmoji(input, emote);
                //处理话题
                input = HandleTag(input);


                //互动抽奖🎁
                input = HandleLottery(input, id, extend_json);
                input = HandleVideoID(input);
                input = input.Replace("^x$%^", "@");
                //生成xaml
                var xaml = string.Format(@"<RichTextBlock HorizontalAlignment=""Stretch"" TextWrapping=""Wrap""  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:mc = ""http://schemas.openxmlformats.org/markup-compatibility/2006"" LineHeight=""20"">
                                          <Paragraph>{0}</Paragraph>
                ", input);
                //处理直播
                var more = HandleCard(input, extend_json["直播"] as JArray);
                xaml += more;
                xaml += "</RichTextBlock>";
                var p = (RichTextBlock)XamlReader.Load(xaml);
                return p;

            }
            catch (Exception ex)
            {
                var tx = new RichTextBlock();
                Paragraph paragraph = new Paragraph();
                Run run = new Run() { Text = txt };
                paragraph.Inlines.Add(run);
                tx.Blocks.Add(paragraph);
                return tx;

            }

        }
        /// <summary>
        /// 处理表情
        /// </summary>
        private static string HandleEmoji(string input, List<DynamicCardDisplayEmojiInfoItemModel> emote)
        {

            if (emote == null) return input;
            //替换表情
            MatchCollection mc = Regex.Matches(input, @"\[.*?\]");
            foreach (Match item in mc)
            {
                if (emote != null && emote.Count > 0)
                {
                    var name = item.Groups[0].Value;
                    var emoji = emote.FirstOrDefault(x => x.emoji_name.Equals(name));
                    if (emoji != null)
                    {
                        input = input.Replace(item.Groups[0].Value, string.Format(@"<InlineUIContainer><Border Margin=""0 -4 4 -4""><Image Source=""{0}"" Width=""{1}"" Height=""{1}""/></Border></InlineUIContainer>",
                       emoji.url, 24));
                    }

                }
            }
            return input;
        }
        /// <summary>
        /// 处理标签
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandleTag(string input)
        {
            //处理话题
            MatchCollection av = Regex.Matches(input, @"\#(.*?)\#");
            List<string> Handle = new List<string>();
            foreach (Match item in av)
            {
                if (!Handle.Contains(item.Groups[0].Value))
                {
                    var data = @"<InlineUIContainer><HyperlinkButton Command=""{Binding TagCommand}""  IsEnabled=""True"" Margin=""0 -4 4 -4"" Padding=""0"" " + string.Format(@" Tag=""{1}""  CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>",
                   item.Groups[0].Value, item.Groups[1].Value);
                    Handle.Add(item.Groups[0].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }

            }



            return input;
        }

        /// <summary>
        /// 处理URL链接
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandleUrl(string input)
        {
            List<string> keyword = new List<string>();
            MatchCollection url = Regex.Matches(input, @"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            foreach (Match item in url)
            {
                if (keyword.Contains(item.Groups[0].Value))
                {
                    continue;
                }
                keyword.Add(item.Groups[0].Value);
                var u = item.Groups[0].Value;
                var display = Utils.ProcessURL(u);
                var data = @"<InlineUIContainer><HyperlinkButton x:Name=""btn"" Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                    string.Format(@" CommandParameter=""{0}"" ><TextBlock>🔗{1}</TextBlock></HyperlinkButton></InlineUIContainer>", u, display);
                input = input.Replace(item.Groups[0].Value, data);
            }


            return input;
        }
        /// <summary>
        /// 处理At及投票
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandleAtAndVote(string input, string origin_content, JObject extendJson)
        {
            var content = origin_content;
            List<CtrlItem> ctrls = new List<CtrlItem>();
            if (extendJson.ContainsKey("ctrl"))
            {
                ctrls = JsonConvert.DeserializeObject<List<CtrlItem>>(extendJson["ctrl"].ToString());
            }
            if (extendJson.ContainsKey("at_control"))
            {
                ctrls = JsonConvert.DeserializeObject<List<CtrlItem>>(extendJson["at_control"].ToString());
            }
            if (ctrls == null) return input;


            foreach (var item in ctrls)
            {
                //@
                if (item.type == 1)
                {
                    try
                    {
                        var d = content.Substring(item.location, item.length);
                        var index = input.IndexOf(d);
                        input = input.Remove(index, item.length);
                        var run = @"<InlineUIContainer>\
<HyperlinkButton Command=""{Binding UserCommand}""  IsEnabled=""True"" Margin=""0 -4 4 -4"" Padding=""0"" " + string.Format(@" Tag=""{1}""  CommandParameter=""{1}"" >\
<TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>", d.Replace("@", "^x$%^"), item.data);
                        input = input.Insert(index, run);
                    }
                    catch (Exception)
                    {
                    }

                }
                //投票
                if (item.type == 3)
                {
                    var d = content.Substring(item.location, content.Length - item.location);
                    var index = input.IndexOf(d);
                    input = input.Remove(index, content.Length - item.location);
                    var run = @"<InlineUIContainer><HyperlinkButton Command=""{Binding VoteCommand}""  IsEnabled=""True"" Margin=""0 -4 4 -4"" Padding=""0"" " + string.Format(@" Tag=""{1}""  CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>",
                        "📊" + d, extendJson["vote"]?["vote_id"]?.ToInt32() ?? 0);
                    input = input.Insert(index, run);
                }
            }
            return input;
        }



        /// <summary>
        /// 处理抽奖
        /// </summary>
        /// <param name="input"></param>
        /// <param name="extendJson"></param>
        /// <returns></returns>
        private static string HandleLottery(string input, string id, JObject extendJson)
        {
            if (!extendJson.ContainsKey("lott")) return input;

            if (input.IndexOf("互动抽奖") == 1)
            {
                input = input.Remove(1, 4);
            }
            input = input.Insert(0, $@"<InlineUIContainer><HyperlinkButton Command=""{{Binding LotteryCommand}}""  CommandParameter=""{id}"" IsEnabled=""True"" Margin=""0 -4 4 -4"" Padding=""0"" ><TextBlock>🎁互动抽奖</TextBlock></HyperlinkButton></InlineUIContainer>");
            return input;
        }


        /// <summary>
        /// 处理直播
        /// </summary>
        /// <param name="input"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        private static string HandleCard(string input, JArray card)
        {
            var xaml = "";
            if (card == null) return xaml;
            foreach (var i in card)
            {
                // 处理直播卡片
                //https://api.vc.bilibili.com/dynamic_mix/v1/dynamic_mix/reserve_attach_card_button
                //cur_btn_status=1&dynamic_id=797438860053184599&attach_card_type=reserve&reserve_total=4866&reserve_id=1549931&spmid=&csrf=7e87c3ad033f16c3b806cc5feb6b194d
                //
                var title = i["title"];
                var live_time = i["time"];
                var live_people = i["people"];
                xaml += $@"<Paragraph>
                            <InlineUIContainer>
                                <Grid Margin=""10"" HorizontalAlignment=""Stretch"">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width=""*"" />
                                        <ColumnDefinition Width=""Auto"" />
                                    </Grid.ColumnDefinitions>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height=""Auto"" />
                                        <RowDefinition Height=""Auto"" />
                                    </Grid.RowDefinitions>
                                    <TextBlock Grid.Row=""0"" Grid.Column=""0"" Text=""{title}"" FontSize=""20"" FontWeight=""Bold"" Margin=""0,0,0,10"" IsTextSelectionEnabled=""True""/>
                                    <StackPanel Grid.Row=""1"" Grid.Column=""0"" Orientation=""Horizontal"" Margin=""0,10,0,0"">
                                        <TextBlock Text=""{live_time}"" Margin=""0,0,20,0"" IsTextSelectionEnabled=""True""/>
                                        <TextBlock Text=""{live_people}"" Margin=""0,0,20,0"" IsTextSelectionEnabled=""True""/>
                                        <Button Content=""预约"" Visibility=""Collapsed"" />
                                    </StackPanel>
                                </Grid>
                            </InlineUIContainer>
                        </Paragraph>";
            }
            return xaml;
        }
        /// <summary>
        /// 处理视频AVID,BVID,CVID
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string HandleVideoID(string input)
        {
            List<string> keyword = new List<string>();
            //如果是链接就不处理了
            if (!Regex.IsMatch(input, @"/[aAbBcC][vV]([a-zA-Z0-9]+)"))
            {
                //处理AV号
                MatchCollection av = Regex.Matches(input, @"[aA][vV](\d+)");
                foreach (Match item in av)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }
                    keyword.Add(item.Groups[0].Value);
                    var data = @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " + string.Format(@" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>", item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }




                //处理BV号

                MatchCollection bv = Regex.Matches(input, @"[bB][vV]([a-zA-Z0-9]{8,})");
                foreach (Match item in bv)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }
                    keyword.Add(item.Groups[0].Value);
                    var data = @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " + string.Format(@" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>", item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }

                //处理CV号

                MatchCollection cv = Regex.Matches(input, @"[cC][vV](\d+)");
                foreach (Match item in cv)
                {
                    if (keyword.Contains(item.Groups[0].Value))
                    {
                        continue;
                    }
                    keyword.Add(item.Groups[0].Value);
                    var data = @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " + string.Format(@" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>", item.Groups[0].Value, "bilibili://article/" + item.Groups[1].Value);
                    input = input.Replace(item.Groups[0].Value, data);
                }
            }
            keyword.Clear();
            keyword = null;
            return input;
        }
    }

    public class CtrlItem
    {
        public string data { get; set; }
        public int length { get; set; }
        public int location { get; set; }
        public int type { get; set; }
    }
}
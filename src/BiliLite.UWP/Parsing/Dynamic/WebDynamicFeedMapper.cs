using System;
using System.Collections.Generic;
using System.Linq;
using BiliLite.Helpers;
using BiliLite.Modules;
using Newtonsoft.Json.Linq;
using static BiliLite.Parsing.Dynamic.WireScalars;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// web 动态流的卡片映射：把聚合流的 JSON 填进渲染层已经在用的 2024 卡片模型。
    /// </summary>
    public static class WebDynamicFeedMapper
    {
        /// <summary>
        /// web 聚合动态流（x/polymer/web-dynamic/v1/feed/all）的卡片映射。字段名与 gRPC 的
        /// DynamicItem/Module* 一一对应，所以逐类填进同一套 2024 卡片模型，渲染层不必区分来源。
        /// 不认识的类型也给一张"不支持"的卡：一页动态不能因为多了一种卡片类型就整体失败。
        /// </summary>
        public static List<DynamicCardModel2024> MapToUserFeedCards(JArray feedItems)
        {
            var result = new List<DynamicCardModel2024>();
            if (feedItems == null)
            {
                return result;
            }

            foreach (var token in feedItems)
            {
                var source = token as JObject;
                if (source == null)
                {
                    continue;
                }

                result.Add(MapWebCard(source));
            }

            return result;
        }

        /// <summary>
        /// 首页视频动态：这条流只认投稿视频（desc.type=8），其它类型没有对应的模板，由调用方跳过。
        /// </summary>
        public static List<DynamicItemModel> MapToHomeVideoItems(JArray feedItems)
        {
            var result = new List<DynamicItemModel>();
            if (feedItems == null)
            {
                return result;
            }

            foreach (var token in feedItems)
            {
                var source = token as JObject;
                if (source == null || source["type"]?.ToString() != "DYNAMIC_TYPE_AV")
                {
                    continue;
                }

                var modules = Obj(source["modules"]);
                var author = Obj(modules?["module_author"]);
                var major = Obj(Obj(modules?["module_dynamic"])?["major"]);
                var archive = Obj(major?["archive"]);
                if (archive == null)
                {
                    continue;
                }

                var id = source["id_str"]?.ToString() ?? "";
                var play = ParseCount(Obj(archive["stat"])?["play"]?.ToString());
                result.Add(new DynamicItemModel
                {
                    desc = new DynamicDescModel
                    {
                        uid = author?["mid"]?.ToString() ?? "",
                        dynamic_id = id,
                        dynamic_id_str = id,
                        time_text = author?["pub_time"]?.ToString() ?? "",
                        type = 8,
                        rid = archive["aid"]?.ToString() ?? "",
                        view = play,
                    },
                    Video = new DynamicVideoCardModel
                    {
                        aid = archive["aid"]?.ToString() ?? "",
                        duration = ParseDuration(archive["duration_text"]?.ToString()),
                        pic = archive["cover"]?.ToString() ?? "",
                        title = archive["title"]?.ToString() ?? "",
                        desc = archive["desc"]?.ToString() ?? "",
                        jump_url = NormalizeWebUrl(archive["jump_url"]?.ToString()),
                        owner = new DynamicVideoCardOwnerModel
                        {
                            face = author?["face"]?.ToString() ?? "",
                            mid = author?["mid"]?.ToString() ?? "",
                            name = author?["name"]?.ToString() ?? "",
                        },
                        stat = new DynamicVideoCardStatModel
                        {
                            view = play,
                            danmaku = ParseCount(Obj(archive["stat"])?["danmaku"]?.ToString()),
                        },
                    },
                });
            }

            return result;
        }

        /// <summary>
        /// 服务端下发的字段类型并不稳（emoji 可能是字符串、modules 可能是字符串、major.archive 可能是数组）。
        /// JToken 的索引器遇到 JValue 会直接抛异常，`x?["a"]` 里的 `?.` 只挡 null，所以嵌套取值一律先过这里。
        /// </summary>
        private static JObject Obj(JToken token)
        {
            return token as JObject;
        }

        private static DynamicCardModel2024 MapWebCard(JObject source)
        {
            var id = source["id_str"]?.ToString() ?? "";
            var itemType = source["type"]?.ToString() ?? "";
            var basic = Obj(source["basic"]);
            var modules = Obj(source["modules"]);
            var moduleDynamic = Obj(modules?["module_dynamic"]);
            var major = Obj(moduleDynamic?["major"]);
            // 新版图文动态的图片在 major.opus.pics 里：拿到图片才算图片动态，没图就是纯文字
            var opusPictures = ReadOpusPictures(Obj(major?["opus"]));
            var cardType = MapWebCardType(itemType);
            if (itemType == "DYNAMIC_TYPE_OPUS")
            {
                cardType = opusPictures.Count > 0 ? "DYNAMIC_TYPE_DRAW" : "DYNAMIC_TYPE_WORD";
            }
            if (cardType == "DYNAMIC_TYPE_UNSUPPORTED")
            {
                LogHelper.Log("Dynamic feed mapping stage=unsupported-type type=" + itemType + " dynamicId=" + id, LogType.DEBUG);
            }
            // opus 动态没有 module_dynamic.desc，正文只在 major.opus.summary 里
            var desc = MapWebDescription(Obj(moduleDynamic?["desc"]));
            if (desc == null)
            {
                desc = MapWebDescription(Obj(Obj(major?["opus"])?["summary"]));
            }

            var card = new DynamicCardModel2024
            {
                type = cardType,
                id_str = id,
                visible = true,
                basic = new DynamicCardModel2024_basic
                {
                    comment_id_str = basic?["comment_id_str"]?.ToString() ?? id,
                    comment_type = ToInt(basic?["comment_type"]?.ToString()),
                    rid_str = basic?["rid_str"]?.ToString() ?? id,
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_author = MapWebAuthor(Obj(modules?["module_author"])),
                    module_stat = MapWebStat(Obj(modules?["module_stat"])),
                    module_dynamic = new DynamicCardModel2024_dynamic
                    {
                        desc = desc,
                        major = MapWebMajor(major, opusPictures),
                    },
                },
            };

            if (itemType == "DYNAMIC_TYPE_FORWARD")
            {
                // 原动态可能是任何类型，映射不出来时用占位卡，父卡片照样可读
                var orig = Obj(source["orig"]);
                card.orig = orig == null ? MissingCard(card.basic.rid_str) : MapWebCard(orig);
            }

            return card;
        }

        private static DynamicCardModel2024_author MapWebAuthor(JObject source)
        {
            if (source == null)
            {
                return null;
            }

            var vip = Obj(source["vip"]);
            var officialVerify = Obj(source["official_verify"]);
            var pendant = Obj(source["pendant"]);
            return new DynamicCardModel2024_author
            {
                mid = ToLong(source["mid"]?.ToString()),
                name = source["name"]?.ToString() ?? "",
                face = source["face"]?.ToString() ?? "",
                pub_time = source["pub_time"]?.ToString() ?? "",
                // web 下发的 pub_ts 是字符串，卡片的时间显示与排序都靠它
                pub_ts = ToLong(source["pub_ts"]?.ToString()),
                vip = vip == null ? null : new DynamicCardModel2024_vip
                {
                    status = ToInt(vip["status"]?.ToString()),
                    type = ToInt(vip["type"]?.ToString()),
                    theme_type = ToInt(vip["theme_type"]?.ToString()),
                    nickname_color = vip["nickname_color"]?.ToString(),
                },
                official_verify = officialVerify == null ? null : new DynamicCardModel2024_off
                {
                    type = ToInt(officialVerify["type"]?.ToString()),
                    desc = officialVerify["desc"]?.ToString() ?? "",
                },
                pendant = pendant == null ? null : new DynamicCardModel2024_pendant
                {
                    pid = ToLong(pendant["pid"]?.ToString()),
                    name = pendant["name"]?.ToString() ?? "",
                    image = pendant["image"]?.ToString() ?? "",
                    expire = ToInt(pendant["expire"]?.ToString()),
                },
            };
        }

        private static DynamicCardModel2024_stat MapWebStat(JObject source)
        {
            if (source == null)
            {
                return new DynamicCardModel2024_stat();
            }

            var comment = Obj(source["comment"]);
            var forward = Obj(source["forward"]);
            var like = Obj(source["like"]);
            return new DynamicCardModel2024_stat
            {
                comment = new DynamicCardModel2024_comment
                {
                    count = ToInt(comment?["count"]?.ToString()),
                    forbidden = ToBool(comment?["forbidden"]),
                },
                forward = new DynamicCardModel2024_forward
                {
                    count = ToInt(forward?["count"]?.ToString()),
                    forbidden = ToBool(forward?["forbidden"]),
                },
                like = new DynamicCardModel2024_like
                {
                    count = ToInt(like?["count"]?.ToString()),
                    status = ToBool(like?["status"]),
                },
            };
        }

        private static DynamicCardModel2024_desc MapWebDescription(JObject source)
        {
            if (source == null)
            {
                return null;
            }

            var nodes = new List<DynamicCardModel2024_node>();
            var richNodes = source["rich_text_nodes"] as JArray;
            if (richNodes != null)
            {
                foreach (var token in richNodes)
                {
                    var node = Obj(token);
                    if (node == null)
                    {
                        continue;
                    }
                    // emoji 有时是字符串（老版本动态）：守不住就是整页 InvalidOperationException
                    var emoji = Obj(node["emoji"]);
                    nodes.Add(new DynamicCardModel2024_node
                    {
                        text = node["text"]?.ToString() ?? "",
                        orig_text = node["orig_text"]?.ToString() ?? "",
                        type = MapDescriptionType(node["type"]?.ToString()),
                        jump_url = node["jump_url"]?.ToString() ?? "",
                        icon_name = node["icon_name"]?.ToString() ?? "",
                        rid = node["rid"]?.ToString() ?? "",
                        emoji = emoji == null ? null : new DynamicCardModel2024_emojiinfo
                        {
                            icon_url = emoji?["icon_url"]?.ToString() ?? node["icon_url"]?.ToString() ?? "",
                            text = emoji?["text"]?.ToString() ?? "",
                            size = ToInt(emoji?["size"]?.ToString()),
                            type = ToInt(emoji?["type"]?.ToString()),
                        },
                    });
                }
            }

            var text = source["text"]?.ToString() ?? "";
            if (nodes.Count == 0 && !string.IsNullOrEmpty(text))
            {
                nodes.Add(new DynamicCardModel2024_node
                {
                    text = text,
                    orig_text = text,
                    type = "RICH_TEXT_NODE_TYPE_TEXT",
                });
            }

            return new DynamicCardModel2024_desc
            {
                text = text,
                rich_text_nodes = nodes,
            };
        }

        /// <summary>
        /// major.opus.pics 的字段是 url/width/height；拿不到 url 的条目直接丢掉，否则渲染层会得到一个点不开的空图。
        /// </summary>
        private static List<DynamicCardModel2024_pic> ReadOpusPictures(JObject opus)
        {
            var pictures = new List<DynamicCardModel2024_pic>();
            var pics = opus?["pics"] as JArray;
            if (pics == null)
            {
                return pictures;
            }
            foreach (var token in pics)
            {
                var pic = Obj(token);
                if (pic == null)
                {
                    continue;
                }
                var src = pic["url"]?.ToString();
                if (string.IsNullOrEmpty(src))
                {
                    src = pic["src"]?.ToString();
                }
                if (string.IsNullOrEmpty(src))
                {
                    continue;
                }
                pictures.Add(new DynamicCardModel2024_pic
                {
                    src = src,
                    width = ToInt(pic["width"]?.ToString()),
                    height = ToInt(pic["height"]?.ToString()),
                });
            }
            return pictures;
        }
        /// <summary>
        /// web 的 major 是"字段全在、只有一个非空"的结构，与 gRPC 的 ModuleDynamic 一一对应，
        /// 所以这里按同样的优先级挑出非空的那一路。
        /// </summary>
        private static DynamicCardModel2024_major MapWebMajor(JObject major, List<DynamicCardModel2024_pic> opusPictures)
        {
            var result = new DynamicCardModel2024_major();
            if (major == null)
            {
                return result;
            }

            var archive = Obj(major["archive"]);
            if (archive != null)
            {
                result.archive = new DynamicCardModel2024_archive
                {
                    aid = archive["aid"]?.ToString() ?? "",
                    bvid = archive["bvid"]?.ToString() ?? "",
                    cover = archive["cover"]?.ToString() ?? "",
                    desc = archive["desc"]?.ToString() ?? "",
                    duration_text = archive["duration_text"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(archive["jump_url"]?.ToString()),
                    title = archive["title"]?.ToString() ?? "",
                    stat = new DynamicCardModel2024_astat
                    {
                        play = Obj(archive["stat"])?["play"]?.ToString() ?? "",
                        danmaku = Obj(archive["stat"])?["danmaku"]?.ToString() ?? "",
                    },
                };
                return result;
            }

            var pgc = Obj(major["pgc"]);
            if (pgc != null)
            {
                result.pgc = new DynamicCardModel2024_season
                {
                    aid = pgc["aid"]?.ToString(),
                    cover = pgc["cover"]?.ToString() ?? "",
                    epid = ToInt(pgc["epid"]?.ToString()),
                    season_id = ToInt(pgc["season_id"]?.ToString()),
                    sub_type = ToInt(pgc["sub_type"]?.ToString()),
                    jump_url = NormalizeWebUrl(pgc["jump_url"]?.ToString()),
                    title = pgc["title"]?.ToString() ?? "",
                    stat = new DynamicCardModel2024_astat
                    {
                        play = Obj(pgc["stat"])?["play"]?.ToString() ?? "",
                        danmaku = Obj(pgc["stat"])?["danmaku"]?.ToString() ?? "",
                    },
                };
                return result;
            }

            var draw = Obj(major["draw"]);
            if (draw != null)
            {
                var pics = new List<DynamicCardModel2024_pic>();
                var drawItems = draw["items"] as JArray;
                if (drawItems != null)
                {
                    foreach (var token in drawItems)
                    {
                        var pic = Obj(token);
                        if (pic == null)
                        {
                            continue;
                        }
                        pics.Add(new DynamicCardModel2024_pic
                        {
                            src = pic["src"]?.ToString() ?? "",
                            width = ToInt(pic["width"]?.ToString()),
                            height = ToInt(pic["height"]?.ToString()),
                        });
                    }
                }
                result.draw = new DynamicCardModel2024_draw
                {
                    id = ToLong(draw["id"]?.ToString()),
                    items = pics,
                };
                return result;
            }

            var article = Obj(major["article"]);
            if (article != null)
            {
                result.article = new DynamicCardModel2024_article
                {
                    id = ToInt(article["id"]?.ToString()),
                    covers = ReadStringArray(article["covers"] as JArray),
                    desc = article["desc"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(article["jump_url"]?.ToString()),
                    label = article["label"]?.ToString() ?? "",
                    title = article["title"]?.ToString() ?? "",
                };
                return result;
            }

            var opus = Obj(major["opus"]);
            if (opus != null && opusPictures != null && opusPictures.Count > 0)
            {
                // 新版图文动态的图片就是图片动态的图片：交给 draw 那套渲染，才有九宫格和点开查看
                result.draw = new DynamicCardModel2024_draw
                {
                    id = ToLong(opus["id"]?.ToString()),
                    items = opusPictures,
                };
                return result;
            }
            if (opus != null)
            {
                // 没有图片的图文当作专栏那张卡：封面取首图，正文取 summary.text
                var covers = new List<string>();
                var opusPics = opus["pics"] as JArray;
                if (opusPics != null)
                {
                    covers.AddRange(opusPics
                        .Select(x => Obj(x)?["url"]?.ToString())
                        .Where(x => !string.IsNullOrEmpty(x)));
                }
                result.article = new DynamicCardModel2024_article
                {
                    covers = covers,
                    desc = Obj(opus["summary"])?["text"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(opus["jump_url"]?.ToString()),
                    title = opus["title"]?.ToString() ?? "",
                };
                return result;
            }

            var live = Obj(major["live"]);
            if (live != null)
            {
                result.live = new DynamicCardModel2024_live
                {
                    id = live["id"]?.ToString() ?? "",
                    cover = live["cover"]?.ToString() ?? "",
                    title = live["title"]?.ToString() ?? "",
                    desc_first = live["area_name"]?.ToString() ?? "",
                    desc_second = Obj(live["watched_show"])?["text_large"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(live["jump_url"]?.ToString()),
                    live_state = ToInt(live["live_state"]?.ToString()),
                };
                return result;
            }

            var liveRcmd = Obj(major["live_rcmd"]);
            if (liveRcmd != null)
            {
                var content = liveRcmd["content"]?.ToString() ?? "";
                result.live_rcmd = new DynamicCardModel2024_live_rcmd
                {
                    content = content,
                };
                try
                {
                    var playInfo = Obj(JObject.Parse(content)?["live_play_info"]);
                    result.live = new DynamicCardModel2024_live
                    {
                        id = playInfo?["room_id"]?.ToString() ?? "",
                        cover = playInfo?["cover"]?.ToString() ?? "",
                        title = playInfo?["title"]?.ToString() ?? "",
                        desc_first = playInfo?["area_name"]?.ToString() ?? "",
                        desc_second = Obj(playInfo?["watched_show"])?["text_large"]?.ToString() ?? "",
                        jump_url = playInfo?["room_id"] == null
                            ? ""
                            : "https://live.bilibili.com/" + playInfo["room_id"],
                    };
                }
                catch
                {
                    // 推荐直播的 content 是嵌套的 JSON 字符串：解不开就只留原样内容给占位卡
                }
                return result;
            }

            var music = Obj(major["music"]);
            if (music != null)
            {
                // 音乐卡借的是专栏那张卡，与 gRPC 的 DynMusic 映射一致
                result.article = new DynamicCardModel2024_article
                {
                    id = ToInt(music["id"]?.ToString()),
                    covers = new List<string> { music["cover"]?.ToString() ?? "" },
                    label = music["label"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(music["jump_url"]?.ToString()),
                    title = music["title"]?.ToString() ?? "",
                };
                return result;
            }

            return result;
        }

        private static string MapWebCardType(string itemType)
        {
            switch (itemType)
            {
                case "DYNAMIC_TYPE_WORD": return "DYNAMIC_TYPE_WORD";
                case "DYNAMIC_TYPE_FORWARD": return "DYNAMIC_TYPE_FORWARD";
                case "DYNAMIC_TYPE_DRAW": return "DYNAMIC_TYPE_DRAW";
                case "DYNAMIC_TYPE_AV": return "DYNAMIC_TYPE_AV";
                case "DYNAMIC_TYPE_MUSIC": return "DYNAMIC_TYPE_MUSIC";
                case "DYNAMIC_TYPE_LIVE": return "DYNAMIC_TYPE_LIVE";
                case "DYNAMIC_TYPE_LIVE_RCMD": return "DYNAMIC_TYPE_LIVE_RCMD";
                case "DYNAMIC_TYPE_NONE": return "DYNAMIC_TYPE_NONE";
                case "DYNAMIC_TYPE_PGC":
                case "DYNAMIC_TYPE_PGC_UNION": return "DYNAMIC_TYPE_PGC_UNION";
                case "DYNAMIC_TYPE_ARTICLE": return "DYNAMIC_TYPE_ARTICLE";
                // DYNAMIC_TYPE_OPUS 在 MapWebCard 里按有没有图先定成 DRAW/WORD，走不到这张表
                default: return "DYNAMIC_TYPE_UNSUPPORTED";
            }
        }
    }
}

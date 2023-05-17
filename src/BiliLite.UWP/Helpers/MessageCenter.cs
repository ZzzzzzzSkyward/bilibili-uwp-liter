﻿using BiliLite.Controls;
using BiliLite.Models;
using BiliLite.Pages;
using BiliLite.Pages.Other;
using BiliLite.Pages.User;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
//using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.Web.Http.Filters;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 页面跳转
    /// </summary>
    public static class MessageCenter
    {
        public static event EventHandler<bool> MiniWindowEvent;
        public static event EventHandler<NavigationInfo> NavigateToPageEvent;
        public static event EventHandler<string> ChangeTitleEvent;
        public static event EventHandler<object> LoginedEvent;
        public static event EventHandler<ImageViewerParameter> ViewImageEvent;
        public static event EventHandler LogoutedEvent;
        public static void NavigateToPage(object sender, NavigationInfo navigationInfo)
        {
            NavigateToPageEvent?.Invoke(sender, navigationInfo);
        }
        public static void SetMiniWindow(bool mini)
        {
            MiniWindowEvent?.Invoke(null, mini);
        }
        public static void ChangeTitle(string title)
        {
            ChangeTitleEvent?.Invoke(null, title);
        }
        /// <summary>
        /// 发送登录完成事件
        /// </summary>
        public async static void SendLogined()
        {
            LoginedEvent?.Invoke(null, null);
            //同步弹幕屏蔽信息
            await new Modules.SettingVM().SyncDanmuFilter();
        }
        /// <summary>
        /// 发送注销事件
        /// </summary>
        public static void SendLogout()
        {
            SettingHelper.SetValue<string>(SettingHelper.Account.ACCESS_KEY, null);
            SettingHelper.SetValue<long>(SettingHelper.Account.USER_ID, 0);
            SettingHelper.SetValue<DateTime>(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now);
            SettingHelper.SetValue<string>(SettingHelper.Account.REFRESH_KEY, null);
            SettingHelper.SetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, null);
            ClaerCookie();
            LogoutedEvent?.Invoke(null, null);
        }

        private static void ClaerCookie()
        {
            try
            {
                var domains = new string[] {
                    "http://bilibili.com",
                    "http://biligame.com",
                    "http://bigfun.cn",
                    "http://bigfunapp.cn",
                    "http://dreamcast.hk"
                };
                //删除Cookie
                HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
                foreach (var domain in domains)
                {
                    var cookies = httpBaseProtocolFilter.CookieManager.GetCookies(new Uri(domain));
                    foreach (var item in cookies)
                    {
                        httpBaseProtocolFilter.CookieManager.DeleteCookie(item);
                    }
                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("清除用户Cookie", LogType.ERROR, ex);
            }
        }

        /// <summary>
        ///统一处理Url
        /// </summary>
        /// <param name="par"></param>
        public async static Task<bool> HandelUrl(string url)
        {
            if (url.First() == '@')
            {
                return false;
            }
            //短链接处理
            if (url.Contains("b23.tv"))
            {
                url = await Utils.GetShortLinkLocation(url);
            }
            /*
             * 视频
             * https://www.bilibili.com/video/av3905642
             * https://m.bilibili.com/video/av3905642.html
             * https://www.bilibili.com/playlist/video/pl688?aid=19827477
             * bilibili://video/19239064
             * bilibili://?av=4284663
             * https://m.bilibili.com/playlist/pl733016988?avid=68818070
             * bilibili://story/722919541
             */

            var video = Utils.RegexMatch(url.Replace("aid", "av").Replace("/", "").Replace("=", ""), @"av(\d+)");
            if (video != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(VideoDetailPage),
                    title = "视频加载中...",
                    parameters = video
                });
                return true;
            }
            video = Utils.RegexMatch(url, @"bilibili://video/(\d+)");
            if (video != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(VideoDetailPage),
                    title = "视频加载中...",
                    parameters = video
                });
                return true;
            }
            video = Utils.RegexMatch(url, @"bilibili://story/(\d+)");
            if (video != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(VideoDetailPage),
                    title = "视频加载中...",
                    parameters = video
                });
                return true;
            }
            video = Utils.RegexMatch(url, @"avid=(\d+)");
            if (video != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(VideoDetailPage),
                    title = "视频加载中...",
                    parameters = video
                });
                return true;
            }

            /*
            * 视频BV号
            * https://www.bilibili.com/video/BV1EE411w75R
            */
            var video_bv = Utils.RegexMatch(url, @"BV([a-zA-Z0-9]{5,})");
            if (video_bv != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(VideoDetailPage),
                    title = "视频加载中...",
                    parameters = video_bv
                });
                return true;
            }

            /* 
             * 番剧/影视
             * https://bangumi.bilibili.com/anime/21680
             * https://www.bilibili.com/bangumi/play/ss21715
             * https://www.bilibili.com/bangumi/play/ep150706
             * https://m.bilibili.com/bangumi/play/ep150706
             * http://m.bilibili.com/bangumi/play/ss21715
             * bilibili://bangumi/season/21715
             * https://bangumi.bilibili.com/movie/12364
             */

            var bangumi = Utils.RegexMatch(url.Replace("movie", "ss").Replace("anime", "ss").Replace("season", "ss").Replace("/", ""), @"ss(\d{4,})");
            if (bangumi != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(SeasonDetailPage),
                    title = "剧集加载中...",
                    parameters = bangumi
                });
                return true;
            }
            bangumi = Utils.RegexMatch(url, @"ep(\d+)");
            if (bangumi != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(SeasonDetailPage),
                    title = "剧集加载中...",
                    parameters = new object[] {
                        await Utils.BangumiEpidToSid(bangumi),
                            bangumi
                    }
                });
                return true;
            }


            /*
             * 点评
             * https://www.bilibili.com/bangumi/media/md11592/
             * https://bangumi.bilibili.com/review/media/11592
             * bilibili://pgc/review/11592
             */

            //var review = Utils.RegexMatch(url.Replace("media", "md").Replace("review", "md").Replace("/", ""), @"md(\d+)");
            //if (review != "")
            //{
            //    //InfoNavigateToEvent(typeof(BanInfoPage), review);
            //    await new Windows.UI.Popups.MessageDialog("请求打开点评" + review).ShowAsync();
            //    return true;
            //}



            /*
            * 直播
            * http://live.bilibili.com/live/5619438.html
            * http://live.bilibili.com/h5/5619438
            * http://live.bilibili.com/5619438
            * bilibili://live/5619438
            */

            var live = Utils.RegexMatch(url.Replace("h5", "live").Replace("live.bilibili.com", "live").Replace("/", ""), @"live(\d+)");
            if (live != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Video,
                    page = typeof(LiveDetailPage),
                    title = "直播间加载中...",
                    parameters = live
                });
                return true;
            }

            ///*
            // * 小视频
            // * http://vc.bilibili.com/mobile/detail?vc=1399466&bilifrom=1
            // * http://vc.bilibili.com/video/1357956
            // * bilibili://clip/1399466
            // */

            //var clip = Utils.RegexMatch(url.Replace("vc=", "clip").Replace("vc.bilibili.com/video", "clip").Replace("/", ""), @"clip(\d+)");
            //if (clip != "")
            //{
            //    MiniVideoDialog miniVideoDialog = new MiniVideoDialog();
            //    miniVideoDialog.ShowAsync(clip);
            //    return true;
            //}


            /*
            * 专栏
            * http://www.bilibili.com/read/cv242568
            * https://www.bilibili.com/read/mobile/242568
            * bilibili://article/242568
            */

            var article = Utils.RegexMatch(url.Replace("read/mobile/", "article").Replace("read/cv", "article").Replace("/", ""), @"article(\d+)");
            if (article != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Document,
                    page = typeof(WebPage),
                    title = "专栏加载中...",
                    parameters = "https://www.bilibili.com/read/cv" + article
                });
                return true;
            }


            /*
             * 音频
             * https://m.bilibili.com/audio/au247991
             * bilibili://music/detail/247991
             */

            var music = Utils.RegexMatch(url.Replace("music/detail/", "au").Replace("/", ""), @"au(\d+)");
            if (music != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.MusicInfo,
                    page = typeof(WebPage),
                    title = "音乐",
                    parameters = "https://m.bilibili.com/audio/au" + music
                });

                return true;
            }
            /*
             * 歌单
             * https://m.bilibili.com/audio/am78723
             * bilibili://music/menu/detail/78723
             */

            var musicmenu = Utils.RegexMatch(url.Replace("menu/detail/", "am").Replace("/", ""), @"am(\d+)");
            if (musicmenu != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.MusicInfo,
                    page = typeof(WebPage),
                    title = "歌单",
                    parameters = "https://m.bilibili.com/audio/am" + musicmenu
                });
                //InfoNavigateToEvent(typeof(MusicMenuPage), musicmenu);
                return true;
            }


            /*
             * 相簿及动态
             * http://h.bilibili.com/ywh/h5/2403422
             * http://h.bilibili.com/2403422
             * bilibili://album/2403422
             * https://t.bilibili.com/84935538081511530
             * bilibili://following/detail/314560419758546547
             */
            var album = Utils.RegexMatch(url.Replace("bilibili://following/detail/", "album").Replace("h.bilibili.com/ywh/h5/", "album").Replace("h.bilibili.com", "album").Replace("t.bilibili.com", "album").Replace("/", ""), @"album(\d+)");
            if (album != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Comment,
                    page = typeof(DynamicDetailPage),
                    title = "动态",
                    parameters = album
                });
                //InfoNavigateToEvent(typeof(DynamicInfoPage), album);
                return true;
            }


            /*
            * 用户中心
            * http://space.bilibili.com/7251681
            * https://m.bilibili.com/space/7251681
            * https://space.bilibili.com/1360010
            * bilibili://author/2622476
            */
            var user = Utils.RegexMatch(url.Replace("space.bilibili.com", "space").Replace("author", "space").Replace("/", ""), @"space(\d+)");
            if (user != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Contact,
                    page = typeof(UserInfoPage),
                    title = "用户信息",
                    parameters = user
                });
                return true;
            }
            /*
            * 话题/频道
            * https://www.bilibili.com/tag/7868838/feed
            * bilibili://tag/0/?name=bilibili%e5%a5%bd%e4%b9%a1%e9%9f%b3
            */
            var topic = Utils.RegexMatch(url, @"tag/(.*?)/feed");
            if (topic != "")
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Tag,
                    page = typeof(TagDetailPage),
                    title = "话题",
                    parameters = new object[] { "", topic }
                });
                return true;
            }
            var topic1 = Utils.RegexMatch(url + "/", @"tag/.*?/\?name=(.*?)/");
            if (topic1 != "")
            {
                var data = Uri.UnescapeDataString(topic1);
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Tag,
                    page = typeof(TagDetailPage),
                    title = "话题",
                    parameters = new object[] { data, "" }
                });
                return true;
            }


            /*
             * 播单
             * https://www.bilibili.com/playlist/detail/pl792
             * https://www.bilibili.com/medialist/detail/ml159001856?type=1
             */
            var medialist_id = Utils.RegexMatch(url, @"ml(\d+)");
            if (!string.IsNullOrEmpty(medialist_id))
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.OutlineStar,
                    page = typeof(FavoriteDetailPage),
                    title = "收藏夹",
                    parameters = new FavoriteDetailArgs()
                    {
                        Id = medialist_id,
                    }
                });

                return true;
            }



            /*
             * 投稿
             * bilibili://uper/user_center/add_archive/
             */
            var add_archive = url.Contains("/add_archive");
            if (add_archive)
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.World,
                    page = typeof(WebPage),
                    title = "投稿",
                    parameters = "https://member.bilibili.com/v2#/upload/video/frame"
                });

                return true;
            }

            /*
             * 我的追番
             * bilibili://main/favorite?tab=bangumi&fav_sub_tab=watching&from=21
             */
            if (url.Contains("favorite?tab=bangumi"))
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.Comment,
                    page = typeof(FavoritePage),
                    title = "我的收藏",
                    parameters = OpenFavoriteType.Bangumi
                });
                return true;
            }

            /*
             * 赛事
             * bilibili://pegasus/channel/v2/9222?tab=5709
             */
            if (url.Contains("bilibili://pegasus/channel/v2/9222"))
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.World,
                    page = typeof(WebPage),
                    title = "赛事",
                    parameters = "https://www.bilibili.com/v/game/match"
                });
                return true;
            }
            if (url.Contains("bilibili://pegasus/channel/v2/9222"))
            {
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.World,
                    page = typeof(WebPage),
                    title = "赛事",
                    parameters = "https://www.bilibili.com/v/game/match"
                });
                return true;
            }




            if (url.Contains("http://") || url.Contains("https://"))
            {
                if (SettingHelper.GetValue<bool>(SettingHelper.UI.OPEN_URL_BROWSER, false))
                {
                    await Launcher.LaunchUriAsync(new Uri(url));
                    return true;
                }
                NavigateToPage(null, new NavigationInfo()
                {
                    icon = Symbol.World,
                    page = typeof(WebPage),
                    title = "加载中...",
                    parameters = url
                });
                return true;
            }

            return false;

        }

        public async static Task<string> HandelSeasonID(string url)
        {
            var bangumi = Utils.RegexMatch(url.Replace("movie", "ss").Replace("anime", "ss").Replace("season", "ss").Replace("/", ""), @"ss(\d+)");
            if (bangumi != "")
            {
                return bangumi;
            }
            bangumi = Utils.RegexMatch(url, @"ep(\d+)");
            if (bangumi != "")
            {
                return await Utils.BangumiEpidToSid(bangumi);
            }
            return "";
        }

        public static void OpenImageViewer(List<string> images, int index)
        {
            var par = new Controls.ImageViewerParameter()
            {
                Images = images,
                Index = index
            };
            if (SettingHelper.GetValue<bool>(SettingHelper.UI.NEW_WINDOW_PREVIEW_IMAGE, false))
            {
                OpenWindow(typeof(ImageViewerPage), par);
            }
            else
            {
                ViewImageEvent?.Invoke(null, par);
            }

        }
        public async static void OpenWindow(Type page, object par)
        {

            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;

            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(page, par);
                Window.Current.Content = frame;
                Window.Current.Activate();
                newViewId = ApplicationView.GetForCurrentView().Id;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(800, 800));
                ApplicationView.GetForCurrentView().Consolidated += (sender, args) =>
                {
                    frame.Navigate(typeof(BlankPage));
                    CoreWindow.GetForCurrentThread().Close();
                };
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);



        }

    }

    public class NavigationInfo
    {
        public Symbol icon { get; set; } = Symbol.Document;
        public Type page { get; set; }
        public string title { get; set; }
        public object parameters { get; set; }
    }


}

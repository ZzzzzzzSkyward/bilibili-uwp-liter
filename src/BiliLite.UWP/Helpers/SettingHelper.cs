﻿using BiliLite.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliLite.Helpers
{
    public class SettingHelper
    {

        public static LocalObjectStorageHelper storageHelper = new LocalObjectStorageHelper();
        public static T GetValue<T>(string key, T _default)
        {
            if (storageHelper.KeyExists(key))
            {
                return storageHelper.Read<T>(key);
            }
            else
            {
                return _default;
            }
        }
        public static void SetValue<T>(string key, T value)
        {
            storageHelper.Save<T>(key, value);
        }
        public class UI
        {
            /// <summary>
            /// 加载原图
            /// </summary>
            public const string ORTGINAL_IMAGE = "originalImage";
            public static bool? _loadOriginalImage = null;
            public static bool LoadOriginalImage
            {
                get
                {
                    if (_loadOriginalImage == null)
                    {
                        _loadOriginalImage = GetValue(ORTGINAL_IMAGE, false);
                    }
                    return _loadOriginalImage.Value;
                }
            }
            /// <summary>
            /// 主题颜色
            /// </summary>
            public const string THEME_COLOR = "themeColor";

            /// <summary>
            /// 主题,0为默认，1为浅色，2为深色
            /// </summary>
            public const string THEME = "theme";

            /// <summary>
            /// 显示模式,0为多标签，1为单窗口，2为多窗口
            /// </summary>
            public const string DISPLAY_MODE = "displayMode";

            /// <summary>
            /// 缓存首页
            /// </summary>
            public const string CACHE_HOME = "cacheHome";

            /// <summary>
            /// 首页排序
            /// </summary>
            public const string HOEM_ORDER = "homePageOrder";

            /// <summary>
            /// 右侧详情宽度
            /// </summary>
            public const string RIGHT_DETAIL_WIDTH = "PlayerRightDetailWidth";

            /// <summary>
            /// 图片圆角半径
            /// </summary>
            public const string IMAGE_CORNER_RADIUS = "ImageCornerRadius";

            /// <summary>
            /// 视频详情显示封面
            /// </summary>
            public const string SHOW_DETAIL_COVER = "showDetailCover";
            /// <summary>
            /// 新窗口打开图片预览
            /// </summary>
            public const string NEW_WINDOW_PREVIEW_IMAGE = "newWindowPreviewImage";
            /// <summary>
            /// 动态显示样式
            /// </summary>
            public const string DYNAMIC_DISPLAY_MODE = "dynamicDiaplayMode";
            /// <summary>
            /// 首页推荐样式
            /// </summary>
            public const string RECMEND_DISPLAY_MODE = "recomendDiaplayMode";
            /// <summary>
            /// 右侧选项卡
            /// </summary>
            public const string DETAIL_DISPLAY = "detailDisplay";
            /// <summary>
            /// 动态显示样式
            /// </summary>
            public const string BACKGROUND_IMAGE = "BackgroundImage";
            /// <summary>
            /// 鼠标功能键返回、关闭页面
            /// </summary>
            public const string MOUSE_BACK = "MouseBack";
            /// <summary>
            /// 隐藏赞助按钮
            /// </summary>
            public const string HIDE_SPONSOR = "HideSponsor";
            /// <summary>
            /// 隐藏广告按钮
            /// </summary>
            public const string HIDE_AD = "HideAD";
            /// <summary>
            /// 浏览器打开无法处理的链接
            /// </summary>
            public const string OPEN_URL_BROWSER = "OpenUrlWithBrowser";
        }
        public class Account
        {
            /// <summary>
            /// 登录后ACCESS_KEY
            /// </summary>
            public const string ACCESS_KEY = "accesskey";
            /// <summary>
            /// 登录后REFRESH_KEY
            /// </summary>
            public const string REFRESH_KEY = "refreshkey";
            /// <summary>
            /// 到期时间
            /// </summary>
            public const string ACCESS_KEY_EXPIRE_DATE = "expireDate";
            /// <summary>
            /// 用户ID
            /// </summary>
            public const string USER_ID = "uid";
            /// <summary>
            /// 到期时间
            /// </summary>
            public const string USER_PROFILE = "userProfile";
            public static MyProfileModel Profile
            {
                get
                {
                    return storageHelper.Read<MyProfileModel>(USER_PROFILE);
                }
            }
            public static bool Logined
            {
                get
                {
                    return storageHelper.KeyExists(Account.ACCESS_KEY) && !string.IsNullOrEmpty(storageHelper.Read<string>(Account.ACCESS_KEY, null));
                }
            }
            public static string AccessKey
            {
                get
                {
                    return GetValue(ACCESS_KEY, "");
                }
            }
            public static int UserID
            {
                get
                {
                    return GetValue(USER_ID, 0);
                }
            }
        }
        public class VideoDanmaku
        {
            /// <summary>
            /// 显示弹幕 Visibility
            /// </summary>
            public const string SHOW = "VideoDanmuShow";
            /// <summary>
            /// 弹幕缩放 double
            /// </summary>
            public const string FONT_ZOOM = "VideoDanmuFontZoom";
            /// <summary>
            /// 弹幕显示区域
            /// </summary>
            public const string AREA = "VideoDanmuArea";
            /// <summary>
            /// 弹幕速度 int
            /// </summary>
            public const string SPEED = "VideoDanmuSpeed";
            /// <summary>
            /// 弹幕加粗 bool
            /// </summary>
            public const string BOLD = "VideoDanmuBold";
            /// <summary>
            /// 弹幕边框样式 int
            /// </summary>
            public const string BORDER_STYLE = "VideoDanmuStyle";
            /// <summary>
            /// 弹幕合并 bool
            /// </summary>
            public const string MERGE = "VideoDanmuMerge";
            /// <summary>
            /// 弹幕半屏显示 bool
            /// </summary>
            public const string DOTNET_HIDE_SUBTITLE = "VideoDanmuDotHide";
            /// <summary>
            /// 弹幕透明度 double，0-1
            /// </summary>
            public const string OPACITY = "VideoDanmuOpacity";
            /// <summary>
            /// 隐藏顶部 bool
            /// </summary>
            public const string HIDE_TOP = "VideoDanmuHideTop";
            /// <summary>
            /// 隐藏底部 bool
            /// </summary>
            public const string HIDE_BOTTOM = "VideoDanmuHideBottom";
            /// <summary>
            /// 隐藏滚动 bool
            /// </summary>
            public const string HIDE_ROLL = "VideoDanmuHideRoll";
            /// <summary>
            /// 隐藏高级弹幕 bool
            /// </summary>
            public const string HIDE_ADVANCED = "VideoDanmuHideAdvanced";

            /// <summary>
            /// 关键词屏蔽 ObservableCollection<string>
            /// </summary>
            public const string SHIELD_WORD = "VideoDanmuShieldWord";

            /// <summary>
            /// 用户屏蔽 ObservableCollection<string>
            /// </summary>
            public const string SHIELD_USER = "VideoDanmuShieldUser";

            /// <summary>
            /// 正则屏蔽 ObservableCollection<string>
            /// </summary>
            public const string SHIELD_REGULAR = "VideoDanmuShieldRegular";

            /// <summary>
            /// 顶部距离
            /// </summary>
            public const string TOP_MARGIN = "VideoDanmuTopMargin";
            /// <summary>
            /// 最大数量
            /// </summary>
            public const string MAX_NUM = "VideoDanmuMaxNum";
            /// <summary>
            /// 弹幕云屏蔽等级
            /// </summary>
            public const string SHIELD_LEVEL = "VideoDanmuShieldLevel";
        }
        public class Live
        {
            /// <summary>
            /// 直播默认清晰度
            /// </summary>
            public const string DEFAULT_QUALITY = "LiveDefaultQuality";
            /// <summary>
            /// 显示弹幕 Visibility
            /// </summary>
            public const string SHOW = "LiveDanmuShow";
            public const string AREA = "LiveDanmuArea";
            /// <summary>
            /// 弹幕缩放 double
            /// </summary>
            public const string FONT_ZOOM = "LiveDanmuFontZoom";
            /// <summary>
            /// 弹幕速度 int
            /// </summary>
            public const string SPEED = "LiveDanmuSpeed";
            /// <summary>
            /// 弹幕加粗 bool
            /// </summary>
            public const string BOLD = "LiveDanmuBold";
            /// <summary>
            /// 弹幕边框样式 int
            /// </summary>
            public const string BORDER_STYLE = "LiveDanmuStyle";
            /// <summary>
            /// 弹幕半屏显示 bool
            /// </summary>
            public const string DOTNET_HIDE_SUBTITLE = "LiveDanmuDotHide";
            /// <summary>
            /// 弹幕透明度 double，0-1
            /// </summary>
            public const string OPACITY = "LiveDanmuOpacity";
            /// <summary>
            /// 关键词屏蔽 ObservableCollection<string>
            /// </summary>
            public const string SHIELD_WORD = "LiveDanmuShieldWord";

            /// <summary>
            /// 硬解 bool
            /// </summary>
            public const string HARDWARE_DECODING = "LiveHardwareDecoding";

            /// <summary>
            /// 自动开启宝箱 bool
            /// </summary>
            public const string AUTO_OPEN_BOX = "LiveAutoOpenBox";

            /// <summary>
            /// 直播弹幕延迟
            /// </summary>
            public const string DELAY = "LiveDelay";

            /// <summary>
            /// 直播弹幕清理
            /// </summary>
            public const string DANMU_CLEAN_COUNT = "LiveCleanCount";

            /// <summary>
            /// 隐藏进场
            /// </summary>
            public const string HIDE_WELCOME = "LiveHideWelcome";

            /// <summary>
            /// 隐藏礼物
            /// </summary>
            public const string HIDE_GIFT = "LiveHideGift";

            /// <summary>
            /// 隐藏公告
            /// </summary>
            public const string HIDE_SYSTEM = "LiveSystemMessage";
            /// <summary>
            /// 隐藏抽奖
            /// </summary>
            public const string HIDE_LOTTERY = "LiveHideLottery";
        }
        public class Player
        {
            /// <summary>
            /// 使用外站视频替换无法播放的视频 bool
            /// </summary>
            public const string USE_OTHER_SITEVIDEO = "PlayerUseOther";

            /// <summary>
            /// 硬解 bool
            /// </summary>
            public const string HARDWARE_DECODING = "PlayerHardwareDecoding";

            /// <summary>
            /// 自动播放 bool
            /// </summary>
            public const string AUTO_PLAY = "PlayerAutoPlay";
            /// <summary>
            /// 自动切换下一个视频
            /// </summary>
            public const string AUTO_NEXT = "PlayerAutoNext";
            /// <summary>
            /// 默认清晰度 int
            /// </summary>
            public const string DEFAULT_QUALITY = "PlayerDefaultQuality";

            /// <summary>
            /// 比例 int
            /// </summary>
            public const string RATIO = "PlayerDefaultRatio";

            /// <summary>
            /// 默认视频类型 int flv=0, dash=1,dash_hevc=2
            /// </summary>
            public const string DEFAULT_VIDEO_TYPE = "PlayerDefaultVideoType";
            public static List<double> VideoSpeed = new List<double>() { 2.0d, 1.5d, 1.25d, 1.0d, 0.75d, 0.5d };

            /// <summary>
            /// 默认视频类型 int 1.0
            /// </summary>
            public const string DEFAULT_VIDEO_SPEED = "PlayerDefaultSpeed";

            /// <summary>
            /// 播放模式 int 0=顺序播放，1=单集循环，2=列表循环
            /// </summary>
            public const string DEFAULT_PLAY_MODE = "PlayerDefaultPlayMode";
            /// <summary>
            /// 音量
            /// </summary>
            public const string PLAYER_VOLUME = "PlayerVolume";
            /// <summary>
            /// 亮度
            /// </summary>
            public const string PLAYER_BRIGHTNESS = "PlayeBrightness";
            /// <summary>
            /// A-B 循环播放模式的播放记录
            /// </summary>
            public const string PLAYER_ABPLAY_HISTORIES = "PlayerABPlayHistories";

            /// <summary>
            /// 字幕颜色
            /// </summary>
            public const string SUBTITLE_COLOR = "subtitleColor";
            /// <summary>
            /// 字幕背景颜色
            /// </summary>
            public const string SUBTITLE_BORDER_COLOR = "subtitleBorderColor";
            /// <summary>
            /// 字幕大小
            /// </summary>
            public const string SUBTITLE_SIZE = "subtitleSize";
            /// <summary>
            /// 字幕显示
            /// </summary>
            public const string SUBTITLE_SHOW = "subtitleShow";
            /// <summary>
            /// 字幕透明度
            /// </summary>
            public const string SUBTITLE_OPACITY = "subtitleOpacity";
            /// <summary>
            /// 字幕底部距离
            /// </summary>
            public const string SUBTITLE_BOTTOM = "subtitleBottom";
            /// <summary>
            /// 字幕加粗
            /// </summary>
            public const string SUBTITLE_BOLD = "subtitleBold";
            /// <summary>
            /// 字幕对齐
            /// 0=居中对齐，1=左对齐，2=右对齐
            /// </summary>
            public const string SUBTITLE_ALIGN = "subtitleAlign";
            /// <summary>
            /// 自动跳转进度
            /// </summary>
            public const string AUTO_TO_POSITION = "PlayerAutoToPosition";
            /// <summary>
            /// 自动铺满窗口
            /// </summary>
            public const string AUTO_FULL_WINDOW = "PlayerAutoToFullWindow";
            /// <summary>
            /// 自动铺满全屏
            /// </summary>
            public const string AUTO_FULL_SCREEN = "PlayerAutoToFullScreen";
            /// <summary>
            /// 双击全屏
            /// </summary>
            public const string DOUBLE_CLICK_FULL_SCREEN = "PlayerDoubleClickFullScreen";
            /// <summary>
            /// 自动打开AI字幕
            /// </summary>
            public const string AUTO_OPEN_AI_SUBTITLE = "PlayerAutoOpenAISubtitle";
         

            /// <summary>
            /// 替换CDN
            /// </summary>
            public const string REPLACE_CDN = "PlayerReplaceCDN";

            /// <summary>
            /// CDN服务器
            /// </summary>
            public const string CDN_SERVER = "PlayerCDNServer";
        }
        public class Roaming
        {
            /// <summary>
            /// 自定义服务器
            /// </summary>
            public const string CUSTOM_SERVER = "RoamingCustomServer";
            /// <summary>
            /// 自定义服务器链接
            /// </summary>
            public const string CUSTOM_SERVER_URL = "RoamingCustomServerUrl";

            /// <summary>
            /// 自定义香港服务器链接
            /// </summary>
            public const string CUSTOM_SERVER_URL_HK = "RoamingCustomServerUrlHK";

            /// <summary>
            /// 自定义台湾服务器链接
            /// </summary>
            public const string CUSTOM_SERVER_URL_TW = "RoamingCustomServerUrlTW";

            /// <summary>
            /// 自定义大陆服务器链接
            /// </summary>
            public const string CUSTOM_SERVER_URL_CN= "RoamingCustomServerUrlCN";

            /// <summary>
            /// 简体中文
            /// </summary>
            public const string TO_SIMPLIFIED = "RoamingSubtitleToSimplified";
            /// <summary>
            /// 只使用AkamaiCDN链接
            /// </summary>
            //public const string AKAMAI_CDN = "RoamingAkamaiCDN";

           

        }
        public class Download
        {
            /// <summary>
            /// 下载目录
            /// </summary>
            public const string DOWNLOAD_PATH = "downloadPath";
            public const string DEFAULT_PATH = "视频库/哔哩哔哩下载";
            /// <summary>
            /// 旧版下载目录
            /// </summary>
            public const string OLD_DOWNLOAD_PATH = "downloadOldPath";
            public const string DEFAULT_OLD_PATH = "视频库/BiliBiliDownload";
            /// <summary>
            /// 允许付费网络下载
            /// </summary>
            public const string ALLOW_COST_NETWORK = "allowCostNetwork";

            /// <summary>
            /// 并行下载
            /// </summary>
            public const string PARALLEL_DOWNLOAD = "parallelDownload";

            /// <summary>
            /// 并行下载
            /// </summary>
            public const string SEND_TOAST = "sendToast";

            /// <summary>
            /// 加载旧版下载视频
            /// </summary>
            public const string LOAD_OLD_DOWNLOAD = "loadOldDownload";

            /// <summary>
            /// 下载视频类型
            /// </summary>
            public const string DEFAULT_VIDEO_TYPE = "DownloadDefaultVideoType";

        }
    }
}

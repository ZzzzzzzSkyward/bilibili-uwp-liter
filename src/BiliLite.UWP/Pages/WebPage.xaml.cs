using BiliLite.Controls;
using BiliLite.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebPage : BasePage
    {
        public WebPage()
        {
            this.InitializeComponent();
            Title = "网页浏览";
            this.Loaded += WebPage_Loaded;
        }
        private async void WebPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is MyFrame)
            {
                (this.Parent as MyFrame).ClosedPage -= WebPage_ClosedPage;
                (this.Parent as MyFrame).ClosedPage += WebPage_ClosedPage;
            }
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = true;
            webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        }

        private void WebPage_ClosedPage(object sender, EventArgs e)
        {
            webView.NavigateToString("");
            (this.Content as Grid).Children.Remove(webView);
            webView = null;
            GC.Collect();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                var uri = e.Parameter.ToString();
                if (uri.Contains("h5/vlog"))
                {
                    webView.MaxWidth = 500;
                }

                if (uri.Contains("read/cv"))
                {
                    //如果是专栏，内容加载完成再显示
                    webView.Visibility = Visibility.Collapsed;
                }
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.Navigate(uri);
                UrlBox.Text = uri;

            }

        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back || e.SourcePageType == typeof(BlankPage))
            {
                NavigationCacheMode = NavigationCacheMode.Disabled;
                webView.NavigateToString("");
                (this.Content as Grid).Children.Remove(webView);
                webView = null;
                GC.Collect();
            }
            base.OnNavigatingFrom(e);
        }


        private void btnForword_Click(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoForward)
            {
                webView.GoForward();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            webView.CoreWebView2.Reload();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        private async void webView_NavigationCompleted(WebView2 sender,CoreWebView2NavigationCompletedEventArgs args)
        {
            if (this.Parent != null)
            {
                if ((this.Parent as Frame).Parent is TabViewItem)
                {
                    if (!string.IsNullOrEmpty(webView.CoreWebView2?.DocumentTitle))
                    {
                        ((this.Parent as Frame).Parent as TabViewItem).Header = webView.CoreWebView2.DocumentTitle;
                        UrlBox.Text = webView.Source.AbsoluteUri;
                    }
                }
                else
                {
                    MessageCenter.ChangeTitle(webView.CoreWebView2?.DocumentTitle??"");
                }
            }
            try
            {

                //专栏阅读设置
                if (webView.Source != null && webView.Source.AbsolutePath.Contains("read/cv"))
                {
                    await webView?.CoreWebView2.ExecuteScriptWithResultAsync(
                    @"$('#internationalHeader').hide();
$('.unlogin-popover').hide();
$('.up-info-holder').hide();
$('.nav-tab-bar').hide();
$('.international-footer').hide();
$('.page-container').css('padding-right','0');
$('.no-login').hide();
$('.author-container').show();
$('.author-container').css('margin','12px 0px -12px 0px');"
                );
                    //将专栏图片替换成jpg
                    await webView?.CoreWebView2.ExecuteScriptWithResultAsync(
                        @"document.getElementsByClassName('img-box').forEach(element => {
                element.getElementsByTagName('img').forEach(image => {
                    image.src=image.getAttribute('data-src')+'@progressive.jpg';
               });
            });"
                   );
                }
                await webView?.CoreWebView2.ExecuteScriptWithResultAsync("$('.h5-download-bar').hide()");
            }
            catch (Exception)
            {

            }
            finally
            {
                if (webView != null) webView.Visibility = Visibility.Visible;
            }
        }

        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            Utils.SetClipboard(webView.Source.ToString());
        }

        private async void webView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            var re = await MessageCenter.HandleUrl(args.Uri.AbsoluteUri);
            if (!re)
            {
                var md = new MessageDialog("是否使用外部浏览器打开此链接？");
                md.Commands.Add(new UICommand("确定", new UICommandInvokedHandler(async (e) => { await Utils.LaunchUri(args.Uri); })));
                md.Commands.Add(new UICommand("取消", new UICommandInvokedHandler((e) => { })));
                await md.ShowAsync();
            }
        }

        private async void btnOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            await Utils.LaunchUri(webView.Source);
        }

        private void webView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri != null && args.Uri.Contains("read/cv"))
            {
                // args.Cancel = true;
                // return;
            }
        }

        private async void webView_UnsupportedUriSchemeIdentified(WebView sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
        {
            if (args.Uri.AbsoluteUri.Contains("article"))
            {
                args.Handled = true;
                return;
            }
            if (args.Uri.AbsoluteUri.Contains("bilibili://"))
            {
                args.Handled = true;
                var re = await MessageCenter.HandleUrl(args.Uri.AbsoluteUri);
                if (!re)
                {
                    Utils.ShowMessageToast("不支持打开的链接" + args.Uri.AbsoluteUri);
                }
            }

        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageToast("虽然看起来像个浏览器，但这完全这不是个浏览器啊！ ╰（‵□′）╯");
        }
    }
}

﻿using BiliLite.Helpers;
using BiliLite.Modules.User;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliLite.Dialogs
{
    public sealed partial class LoginDialog : ContentDialog
    {
        JSBridge.biliapp _biliapp = new JSBridge.biliapp();
        JSBridge.secure _secure = new JSBridge.secure();
        private LoginVM loginVM;
        public LoginDialog()
        {
            this.InitializeComponent();
            this.Loaded += SMSLoginDialog_Loaded;
            loginVM=new LoginVM();
            loginVM.OpenWebView += LoginVM_OpenWebView;
            loginVM.CloseDialog += LoginVM_CloseDialog;
            loginVM.SetWebViewVisibility += LoginVM_SetWebViewVisibility;
            _biliapp.CloseBrowserEvent += _biliapp_CloseBrowserEvent;
            _biliapp.ValidateLoginEvent += _biliapp_ValidateLoginEvent;
           
        }

        private void LoginVM_CloseDialog(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void LoginVM_SetWebViewVisibility(object sender, bool e)
        {
            webView.Visibility = e ? Visibility.Visible : Visibility.Collapsed;
        }

        private void _biliapp_CloseBrowserEvent(object sender, string e)
        {
            this.Hide();
        }
        private  void _biliapp_ValidateLoginEvent(object sender, string e)
        {
            loginVM.ValidateLogin(JObject.Parse(e));

        }
        private void LoginVM_OpenWebView(object sender, Uri e)
        {
            webView.Source=e;
        }

        private void SMSLoginDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _ = loginVM.LoadCountry();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            loginVM.DoLogin();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if(webView.Visibility == Visibility.Visible)
            {
                webView.Visibility=Visibility.Collapsed;
                args.Cancel = true;
                return;
            }
        }

       

        private void txt_Password_GotFocus(object sender, RoutedEventArgs e)
        {
            hide.Visibility = Visibility.Visible;
        }

        private void txt_Password_LostFocus(object sender, RoutedEventArgs e)
        {
            hide.Visibility = Visibility.Collapsed;
        }
        private List<string> ParseJiYan(string uri)
        {
            var challenge = Regex.Match(uri, "geetest_challenge=(.*?)&").Groups[1].Value;
            var validate = Regex.Match(uri, "geetest_validate=(.*?)&").Groups[1].Value;
            var seccode = Regex.Match(uri, "geetest_seccode=(.*?)&").Groups[1].Value;
            var recaptcha_token = Regex.Match(uri, "recaptcha_token=(.*?)&").Groups[1].Value;
            var ret = new List<string>();
            ret.Add(challenge);
            ret.Add(validate);
            ret.Add(seccode);
            ret.Add(recaptcha_token);
            return ret;
        }
        private void TryRelogin(string uri)
        {
            var parsed = ParseJiYan(uri);
            var challenge = parsed[0];
            var validate = parsed[1];
            var seccode = parsed[2];
            var recaptcha_token = parsed[3];
            //重新登录
            if (loginVM.LoginType == 0)
            {
                loginVM.DoPasswordLogin(seccode, validate, challenge, recaptcha_token);
            }
            //发送短信
            if (loginVM.LoginType == 1)
            {
                loginVM.SendSMSCodeWithCaptcha(seccode, validate, challenge, recaptcha_token);
            }
            //Login(seccode, validate, challenge, recaptcha_token);
        }
        private async void webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.AbsoluteUri.Contains("access_key="))
            {
                var access = Regex.Match(args.Uri.AbsoluteUri, "access_key=(.*?)&").Groups[1].Value;
                var mid = Regex.Match(args.Uri.AbsoluteUri, "mid=(.*?)&").Groups[1].Value;
                await loginVM.account.SaveLogin(access, "", 0, long.Parse(mid),null,null);
                this.Hide();
                return;
            }
            if (args.Uri.AbsoluteUri.Contains("geetest.result"))
            {
                var success = (Regex.Match(args.Uri.AbsoluteUri, @"success=(\d)&").Groups[1].Value).ToInt32();
                if (success == 0)
                {
                    //验证失败
                    webView.Visibility = Visibility.Collapsed;
                    Utils.ShowMessageToast("验证失败");
                }
                else if (success == 1)
                {
                    webView.Visibility = Visibility.Collapsed;
                    //验证成功
                    TryRelogin(args.Uri.AbsoluteUri);
                }
                else if (success == 2)
                {
                    //关闭验证码
                    IsPrimaryButtonEnabled = true;
                   
                    webView.Visibility = Visibility.Collapsed;
                }
                return;
            }
            try
            {
                //await webView.CoreWebView2.ExecuteScriptAsync(text);
                this.webView.AddWebAllowedObject("biliapp", _biliapp);
                this.webView.AddWebAllowedObject("secure", _secure);
            }
            catch (Exception ex)
            {
                LogHelper.Log("注入JS对象失败", LogType.ERROR, ex);
            }
        }

        private async void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            //await webView.AddScriptToExecuteOnDocumentCreatedAsync("window.chrome.webview.postMessage({'command': 'trustAllCertificates'})");
            if (args.Uri.AbsoluteUri == "https://passport.bilibili.com/ajax/miniLogin/redirect" || args.Uri.AbsoluteUri == "https://www.bilibili.com/")
            {
                var results = await HttpHelper.GetString("https://passport.bilibili.com/login/app/third?appkey=27eb53fc9058f8c3&api=http%3A%2F%2Flink.acg.tv%2Fforum.php&sign=67ec798004373253d60114caaad89a8c");
                var obj = JObject.Parse(results);
                if (obj["code"].ToInt32() == 0)
                {
                    webView.Navigate(new Uri(obj["data"]["confirm_uri"].ToString()));
                }
                else
                {
                    Utils.ShowMessageToast("登录失败，请重试");
                }
                return;
            }
        }

        private void ManualLogin(object sender, TextChangedEventArgs e)
        {
            var result = ManualResult.Text;
            if (result != "" && Regex.Match(result, "geetest").Success)
            {
                TryRelogin(result);
            }
        }
    }
}

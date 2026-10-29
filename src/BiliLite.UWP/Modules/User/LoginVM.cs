using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Windows.System;

namespace BiliLite.Modules.User
{
    public class LoginVM : IModules
    {
        public Account account;
        Timer smsTimer;
        Timer qrTimer;
        Api.AccountApi accountApi;
        private readonly PasswordLoginSessionCoordinator passwordSessionCoordinator = new PasswordLoginSessionCoordinator();
        private long passwordSessionOperation;
        private string passwordVerificationTmpToken;
        private string passwordVerificationRequestId;
        private string passwordVerificationCaptchaKey;
        private string passwordVerificationChallenge;
        private string passwordVerificationGt;
        private string passwordVerificationValidate;
        private string passwordVerificationSeccode;
        private string passwordVerificationRecaptchaToken;
        private bool passwordVerificationPending;
        private bool passwordVerificationCaptchaActive;
        private bool passwordLoginDialogClosed;
        public LoginVM()
        {
            account = new Account();
            accountApi = new Api.AccountApi();
            Countries = new ObservableCollection<CountryItemModel>();
            LoginTypeCommand = new RelayCommand<int>(ChangeLoginType);
            SendSMSCommand = new RelayCommand(SendSMSCode);
            SendPasswordVerificationSMSCommand = new RelayCommand(SendPasswordVerificationSMS);
            RefreshQRCommand = new RelayCommand(RefreshQRCode);
            smsTimer = new Timer(1000);
            smsTimer.Elapsed += SmsTimer_Elapsed;
            MessageCenter.LogoutedEvent += MessageCenter_LogoutedEvent;
            Manual = false;
        }
        private void MessageCenter_LogoutedEvent(object sender, EventArgs e)
        {
            if (!passwordLoginDialogClosed) CancelPasswordVerification();
        }
        public event EventHandler<Uri> OpenWebView;
        public event EventHandler<bool> SetWebViewVisibility;
        public event EventHandler CloseDialog;
        public ICommand LoginTypeCommand { get; private set; }
        public ICommand SendSMSCommand { get; private set; }
        public ICommand SendPasswordVerificationSMSCommand { get; private set; }
        public ICommand RefreshQRCommand { get; private set; }

        public bool PasswordVerificationPending
        {
            get => passwordVerificationPending;
            private set
            {
                if (passwordVerificationPending == value) return;
                passwordVerificationPending = value;
                DoPropertyChanged(nameof(PasswordVerificationPending));
                DoPropertyChanged(nameof(PasswordVerificationPanelHidden));
                DoPropertyChanged(nameof(PasswordCredentialsHidden));
            }
        }

        public bool PasswordVerificationPanelHidden => !PasswordVerificationPending;
        public bool PasswordCredentialsHidden => PasswordVerificationPending;
        public bool PasswordVerificationCaptchaActive
        {
            get => passwordVerificationCaptchaActive;
            private set
            {
                if (passwordVerificationCaptchaActive == value) return;
                passwordVerificationCaptchaActive = value;
                DoPropertyChanged(nameof(PasswordVerificationCaptchaActive));
            }
        }
        public bool IsPasswordVerificationFlow => !passwordLoginDialogClosed && !string.IsNullOrEmpty(passwordVerificationTmpToken);
        public long PasswordVerificationOperation => passwordSessionOperation;
        public bool IsPasswordLoginDialogClosed => passwordLoginDialogClosed;

        public int loginType = 1;
        /// <summary>
        /// 登录类型
        /// 0=账号密码，1=短信登录，2=二维码登录
        /// </summary>
        public int LoginType
        {
            get { return loginType; }
            set { loginType = value; DoPropertyChanged("LoginType"); }
        }

        private string title = "短信登录";
        public string Title
        {
            get { return title; }
            set { title = value; DoPropertyChanged("Title"); }
        }

        private bool _primaryButtonEnable = true;

        public bool PrimaryButtonEnable
        {
            get { return _primaryButtonEnable; }
            set { _primaryButtonEnable = value; DoPropertyChanged("PrimaryButtonEnable"); }
        }        
        private bool _Manual = false;

        public bool Manual
        {
            get { return _Manual; }
            set { _Manual = value; DoPropertyChanged("Manual"); }
        }

        public void OpenManualPage()
        {
            var url= "https://l78z.nsapps.cn/bili_gt.html";
            Utils.LaunchUri(url);
        }

        public void ChangeLoginType(int type)
        {
            if (type != LoginType) CancelPasswordVerification();
            LoginType = type;

            switch (type)
            {
                case 0:
                    Title = "密码登录";
                    if (qrTimer != null)
                    {
                        qrTimer.Stop();
                        qrTimer.Dispose();
                    }
                    Utils.ShowMessageToast("为了您的账号安全,建议扫描二维码登录");
                    Manual = true;
                    break;
                case 1:
                    Title = "短信登录";
                    if (qrTimer != null)
                    {
                        qrTimer.Stop();
                        qrTimer.Dispose();
                    }
                    Manual = true;
                    break;
                case 2:
                    Title = "二维码登录";
                    GetQRAuthInfo();
                    Manual = false;
                    break;
                case 3:
                    Title = "手动登录";
                    Utils.ShowMessageToast("记得调整一下appkey！");
                    Manual = true;
                    break;
                default:
                    break;
            }
        }

        public void DoLogin()
        {
            if (PasswordVerificationCaptchaActive) return;
            if (loginType == 1)
            {
                DoSMSLogin();
            }
            if (loginType == 0)
            {
                if (PasswordVerificationPending)
                {
                    CompletePasswordLoginCheck();
                    return;
                }
                DoPasswordLogin();
            }

        }

        public void CancelPasswordVerification()
        {
            passwordSessionCoordinator.Invalidate();
            PasswordVerificationPending = false;
            PasswordVerificationCaptchaActive = false;
            passwordVerificationTmpToken = null;
            passwordVerificationRequestId = null;
            passwordVerificationCaptchaKey = null;
            passwordVerificationChallenge = null;
            passwordVerificationGt = null;
            passwordVerificationValidate = null;
            passwordVerificationSeccode = null;
            passwordVerificationRecaptchaToken = null;
            passwordSessionOperation = 0;
            HideTel = "";
            Code = "";
            EnableSendSMS = true;
            SMSCountDown = 60;
            if (smsTimer != null) smsTimer.Stop();
            PrimaryButtonEnable = true;
            SetWebViewVisibility?.Invoke(this, false);
        }

        public void EndLoginDialog()
        {
            MessageCenter.LogoutedEvent -= MessageCenter_LogoutedEvent;
            passwordLoginDialogClosed = true;
            CancelPasswordVerification();
        }

        public async void ValidateLogin(JObject jObject)
        {
            if (passwordLoginDialogClosed || IsPasswordVerificationFlow) return;
            try
            {
                if (jObject["access_token"] != null)
                {
                    var m = await account.SaveLogin(jObject["access_token"].ToString(), jObject["refresh_token"].ToString(), jObject["expires_in"].ToInt32(), Convert.ToInt64(jObject["mid"].ToString()), null,null);

                    if (m)
                    {
                        CloseDialog?.Invoke(this, null);
                        Utils.ShowMessageToast("登录成功");
                    }
                    else
                    {
                        PrimaryButtonEnable = true;
                        SetWebViewVisibility?.Invoke(this, false);
                        Utils.ShowMessageToast("登录失败,请重试");
                    }
                    //await UserManage.LoginSucess(jObject["access_token"].ToString());
                }
                else
                {
                    PrimaryButtonEnable = true;
                    SetWebViewVisibility?.Invoke(this, false);
                    Utils.ShowMessageToast("登录失败,请重试");
                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("登录二次验证失败", LogType.ERROR, ex);
            }
        }

        #region 短信登录
        private ObservableCollection<CountryItemModel> _countries;
        public ObservableCollection<CountryItemModel> Countries
        {
            get { return _countries; }
            set { _countries = value; DoPropertyChanged("Countries"); }
        }
        private CountryItemModel currentCountry;
        public CountryItemModel CurrentCountry
        {
            get { return currentCountry; }
            set { currentCountry = value; DoPropertyChanged("CurrentCountry"); }
        }
        private string phone;
        public string Phone
        {
            get { return phone; }
            set { phone = value; DoPropertyChanged("Phone"); }
        }

        private string code;
        public string Code
        {
            get { return code; }
            set { code = value; DoPropertyChanged("Code"); }
        }

        private bool enableSendSMS = true;
        public bool EnableSendSMS
        {
            get { return enableSendSMS; }
            set { enableSendSMS = value; DoPropertyChanged("EnableSendSMS"); }
        }


        private int _SMSCountDown = 60;
        public int SMSCountDown
        {
            get { return _SMSCountDown; }
            set { _SMSCountDown = value; DoPropertyChanged("SMSCountDown"); }
        }


        public async Task LoadCountry()
        {
            try
            {

                var results = await accountApi.Country().Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        var ls = JsonConvert.DeserializeObject<ObservableCollection<CountryItemModel>>(data.data["list"].ToString());
                        if (ls != null && ls.Count > 0)
                        {
                            Countries = ls;
                            CurrentCountry = Countries.First();
                        }
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }

        }
        private async void SmsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (SMSCountDown <= 1)
                {
                    EnableSendSMS = true;
                    smsTimer.Stop();
                }
                else
                {
                    SMSCountDown--;
                }
            });
        }
        string sessionId = "";
        string captchaKey = "";
        public async void SendSMSCode()
        {
            if (CurrentCountry == null)
            {
                Utils.ShowMessageToast("请选择国家/地区");
                return;
            }
            if (Phone.Length == 0)
            {
                Utils.ShowMessageToast("请输入手机号");
                return;
            }

            try
            {
                sessionId = Guid.NewGuid().ToString().Replace("-", "");
                var results = await accountApi.SendSMS(CurrentCountry.country_code, Phone, sessionId).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<SMSResultModel>>();
                    if (data.success)
                    {
                        if (data.data.recaptcha_url != null && data.data.recaptcha_url.Length > 0)
                        {
                            var uri = new Uri(data.data.recaptcha_url);
                            SetWebViewVisibility?.Invoke(this, true);
                            var newuri = new Uri("ms-appx-web:///Asset/JiYan/%E6%9E%81%E9%AA%8C.htm" + uri.Query + "&app=uwp");
                            if(Manual)
                            Utils.LaunchUri(newuri);
                            else
                            OpenWebView?.Invoke(this, newuri);
                        }
                        else
                        {
                            captchaKey = data.data.captcha_key;
                            EnableSendSMS = false;
                            //验证码发送成功，倒计时
                            SMSCountDown = 60;
                            smsTimer.Start();
                        }
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }

            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast(ex.Message);
                EnableSendSMS = true;
            }
        }
        public async void SendSMSCodeWithCaptcha(string seccode = "", string validate = "", string challenge = "", string recaptcha_token = "")
        {

            try
            {
                var results = await accountApi.SendSMSWithCaptcha(CurrentCountry.country_code, Phone, sessionId, seccode, validate, challenge, recaptcha_token).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<SMSResultModel>>();
                    if (data.success)
                    {
                        if (data.data.recaptcha_url != null && data.data.recaptcha_url.Length > 0)
                        {
                            var uri = new Uri(data.data.recaptcha_url);
                            Utils.ShowMessageToast(uri.AbsoluteUri);
                            SetWebViewVisibility?.Invoke(this, true);
                            var newuri = new Uri("https://l78z.nsapps.cn/bili_gt.html" + uri.Query + "&app=uwp");
                            if (Manual)
                                Utils.LaunchUri(newuri);
                            else
                                OpenWebView?.Invoke(this, newuri);

                        }
                        else
                        {
                            captchaKey = data.data.captcha_key;
                            //验证码发送成功，倒计时
                            EnableSendSMS = false;
                            SMSCountDown = 60;
                            smsTimer.Start();
                        }
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }

            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast(ex.Message);
                EnableSendSMS = true;
            }
        }

        public async void DoSMSLogin()
        {
            if (CurrentCountry == null)
            {
                Utils.ShowMessageToast("请选择国家/地区");
                return;
            }
            if (Phone.Length == 0)
            {
                Utils.ShowMessageToast("请输入手机号");
                return;
            }
            if (Code.Length == 0)
            {
                Utils.ShowMessageToast("请输入验证码");
                return;
            }
            try
            {

                var results = await accountApi.SMSLogin(CurrentCountry.country_code, Phone, Code, sessionId, captchaKey).Request();
                if (results.status)
                {
                    var data = await results.GetData<LoginResultModel>();
                    var result = await HandleLoginResult(data.code, data.message, data.data);
                    HandleResult(result);
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }

            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast(ex.Message);
                EnableSendSMS = true;
            }
        }


        #endregion

        #region 密码登录

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value; DoPropertyChanged("UserName"); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; DoPropertyChanged("Password"); }
        }
        private string hideTel = "";
        public string HideTel
        {
            get { return hideTel; }
            private set { hideTel = value; DoPropertyChanged(nameof(HideTel)); }
        }
        string pwdSessionId = "";
        public async void DoPasswordLogin(string seccode = "", string validate = "", string challenge = "", string recaptcha_token = "")
        {
            if (passwordLoginDialogClosed) return;
            if (string.IsNullOrWhiteSpace(UserName))
            {
                Utils.ShowMessageToast("请输入用户名");
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                Utils.ShowMessageToast("请输入密码");
                return;
            }

            var username = UserName;
            var clearPassword = Password;
            // A new password attempt abandons any incomplete secondary flow.
            // Keep pwdSessionId untouched here so the existing manual captcha
            // retry can continue using its original request session.
            CancelPasswordVerification();
            PrimaryButtonEnable = false;
            var operation = passwordSessionCoordinator.BeginOperation();
            passwordSessionOperation = operation;
            try
            {
                if (seccode == "")
                {
                    pwdSessionId = Guid.NewGuid().ToString().Replace("-", "");
                }


                var pwd = await account.EncryptedPassword(clearPassword);
                if (!passwordSessionCoordinator.IsCurrent(operation) || passwordLoginDialogClosed || LoginType != 0) return;
                var results = await accountApi.LoginV3(username, pwd, pwdSessionId, seccode, validate, challenge, recaptcha_token).Request();
                if (!passwordSessionCoordinator.IsCurrent(operation) || passwordLoginDialogClosed || LoginType != 0) return;
                if (results.status)
                {
                    var data = await results.GetData<LoginResultModel>();
                    if (!passwordSessionCoordinator.IsCurrent(operation) || passwordLoginDialogClosed || LoginType != 0) return;
                    var result = await HandleLoginResult(data.code, data.message, data.data, operation, true);
                    if (!passwordSessionCoordinator.IsCurrent(operation) || passwordLoginDialogClosed || LoginType != 0) return;
                    HandleResult(result);
                }
                else
                {
                    Utils.ShowMessageToast("密码登录请求失败，请重试");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("Password login failed at login stage. exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                Utils.ShowMessageToast("密码登录失败，请重试");

            }
            finally
            {
                if (!passwordLoginDialogClosed && LoginType == 0 && !PasswordVerificationCaptchaActive && passwordSessionCoordinator.IsCurrent(operation))
                    PrimaryButtonEnable = true;
            }
        }

        #endregion

        #region 二维码登录

        private bool qrLoadding;

        public bool QRLoadding
        {
            get { return qrLoadding; }
            set { qrLoadding = value; DoPropertyChanged("QRLoadding"); }
        }

        private Windows.UI.Xaml.Media.ImageSource qrImageSource;

        public Windows.UI.Xaml.Media.ImageSource QRImageSource
        {
            get { return qrImageSource; }
            set { qrImageSource = value; DoPropertyChanged("QRImageSource"); }
        }


        QRAuthInfo qrAuthInfo;
        private async void GetQRAuthInfo()
        {
            try
            {
                QRLoadding = true;
                if (qrTimer != null)
                {
                    qrTimer.Stop();
                    qrTimer.Dispose();
                }
                var result = await account.GetQRAuthInfo();
                if (result.success)
                {
                    qrAuthInfo = result.data;
                    ZXing.BarcodeWriter barcodeWriter = new ZXing.BarcodeWriter();
                    barcodeWriter.Format = ZXing.BarcodeFormat.QR_CODE;
                    barcodeWriter.Options = new ZXing.Common.EncodingOptions()
                    {
                        Margin = 1,
                        Height = 200,
                        Width = 200
                    };
                    var img = barcodeWriter.Write(qrAuthInfo.url);
                    QRImageSource = img;
                    qrTimer = new Timer();
                    qrTimer.Interval = 3000;
                    qrTimer.Elapsed += QrTimer_Elapsed;
                    qrTimer.Start();
                }
                else
                {
                    Utils.ShowMessageToast(result.message);
                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("读取和加载登录二维码失败", LogType.ERROR, ex);
                Utils.ShowMessageToast("加载二维码失败");
            }
            finally
            {
                QRLoadding = false;
            }

        }

        private void RefreshQRCode()
        {
            if (QRLoadding)
                return;
            GetQRAuthInfo();
        }


        private async void QrTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var result = await account.PollQRAuthInfo(qrAuthInfo.auth_code);
                  if (result.status == LoginStatus.Success)
                  {
                      qrTimer.Stop();
                    CloseDialog?.Invoke(this, null);
                  }
              });
        }

        #endregion

        private async Task<LoginCallbackModel> HandleLoginResult(int code, string message, LoginResultModel result,
            long passwordOperation = 0, bool passwordLogin = false)
        {
            if (code == 0)
            {
                if (result.status == 0)
                {
                    if (result.token_info == null || string.IsNullOrWhiteSpace(result.token_info.access_token) || result.token_info.mid <= 0)
                        return new LoginCallbackModel { status = LoginStatus.Fail, message = "登录响应缺少有效会话信息" };
                    var saved = await account.SaveLogin(result.token_info.access_token, result.token_info.refresh_token,
                        result.token_info.expires_in, result.token_info.mid, result.sso, result.cookie_info);
                    if (!saved)
                        return new LoginCallbackModel { status = LoginStatus.Fail, message = "登录信息保存失败，请重试" };
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Success,
                        message = "登录成功"
                    };
                }
                if (result.status == 1 || result.status == 2)
                {
                    if (passwordLogin)
                    {
                        passwordVerificationTmpToken = GetQueryParameter(result.url, "tmp_token") ?? GetQueryParameter(result.url, "tmp_code");
                        passwordVerificationRequestId = GetQueryParameter(result.url, "request_id");
                        if (!passwordSessionCoordinator.IsCurrent(passwordOperation) ||
                            string.IsNullOrWhiteSpace(passwordVerificationTmpToken) || string.IsNullOrWhiteSpace(passwordVerificationRequestId))
                            return new LoginCallbackModel { status = LoginStatus.Fail, message = "登录验证信息已失效，请重新登录" };

                        var captcha = await StartGeetestCaptcha();
                        if (!passwordSessionCoordinator.IsCurrent(passwordOperation))
                            return new LoginCallbackModel { status = LoginStatus.Fail, message = "登录验证信息已失效，请重新登录" };
                        if (captcha == null || string.IsNullOrWhiteSpace(captcha.gee_gt) || string.IsNullOrWhiteSpace(captcha.gee_challenge))
                            return new LoginCallbackModel { status = LoginStatus.Fail, message = "无法加载安全验证，请重试" };
                        passwordVerificationGt = captcha.gee_gt;
                        passwordVerificationChallenge = captcha.gee_challenge;
                        passwordVerificationRecaptchaToken = captcha.recaptcha_token ?? "";
                        PasswordVerificationCaptchaActive = true;
                        return new LoginCallbackModel
                        {
                            status = LoginStatus.NeedCaptcha,
                            message = "本次登录需要安全验证",
                            url = "http://fake/?gee_gt=" + Uri.EscapeDataString(captcha.gee_gt) +
                                "&gee_challenge=" + Uri.EscapeDataString(captcha.gee_challenge) +
                                "&recaptcha_token=" + Uri.EscapeDataString(captcha.recaptcha_token ?? "")
                        };
                    }
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.NeedValidate,
                        message = "本次登录需要安全验证",
                        url = result.url
                    };
                }
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Fail,
                    message = result.message
                };
            }
            else if (code == -105)
            {
                return new LoginCallbackModel()
                {
                    status = LoginStatus.NeedCaptcha,
                    url = result.url,
                    message = "登录需要验证码"
                };
            }
            else
            {
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Fail,
                    message = message
                };
            }

        }
        private void HandleResult(LoginCallbackModel result)
        {
            var uri = new Uri(string.IsNullOrEmpty(result.url) ? "https://www.bilibili.com" : result.url);
            switch (result.status)
            {
                case LoginStatus.Success:
                    CloseDialog?.Invoke(this, null);
                    break;
                case LoginStatus.Fail:
                case LoginStatus.Error:
                    PrimaryButtonEnable = true;
                    Utils.ShowMessageToast(result.message);
                    break;
                case LoginStatus.NeedCaptcha:
                    SetWebViewVisibility?.Invoke(this, true);
                    if (PasswordVerificationCaptchaActive)
                    {
                        PrimaryButtonEnable = false;
                        var captchaPage = new Uri("ms-appx-web:///Assets/Auth/PasswordLoginCaptcha.html?gee_gt=" +
                            Uri.EscapeDataString(passwordVerificationGt ?? "") + "&gee_challenge=" +
                            Uri.EscapeDataString(passwordVerificationChallenge ?? "") + "&recaptcha_token=" +
                            Uri.EscapeDataString(passwordVerificationRecaptchaToken ?? "") + "&operation=" + passwordSessionOperation);
                        OpenWebView?.Invoke(this, captchaPage);
                        break;
                    }
                    //验证码重定向
                    //源码:https://github.com/xiaoyaocz/some_web
                    var newuri = new Uri("https://l78z.nsapps.cn/bili_gt.html" + uri.Query + "&app=uwp");
                    if (Manual)
                    {
                        Utils.LaunchUri(newuri);
                        string gt = Regex.Match(uri.Query, "gt=(.*)&").Groups[1].Value;
                        string cha= Regex.Match(uri.Query, "challenge=(.*)&").Groups[1].Value;
                        Utils.SetClipboard(gt + " " + cha);
                    }
                    else
                        OpenWebView?.Invoke(this, newuri);
                    break;
                case LoginStatus.NeedValidate:
                    SetWebViewVisibility?.Invoke(this, true);
                    if (Manual)
                        Utils.LaunchUri(uri);
                    else
                        OpenWebView?.Invoke(this, uri);
                    break;
                default:
                    break;
            }
        }

        private async Task<GeetestRequestModel> StartGeetestCaptcha()
        {
            try
            {
                var response = await IsolatedPasswordLoginTransport.SendVerificationAsync(accountApi.GeetestCaptcha());
                if (!response.status) return null;
                var body = response.GetJObject();
                if ((int?)body["code"] != 0 || body["data"] == null) return null;
                return JsonConvert.DeserializeObject<GeetestRequestModel>(body["data"].ToString());
            }
            catch (Exception ex)
            {
                LogHelper.Log("Password login captcha setup failed. exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                return null;
            }
        }

        public async void CompletePasswordVerificationCaptcha(string challenge, string validate, string seccode,
            string recaptchaToken, long operation)
        {
            if (!PasswordVerificationCaptchaActive || operation != passwordSessionOperation ||
                !passwordSessionCoordinator.IsCurrent(operation) || LoginType != 0 || passwordLoginDialogClosed) return;
            PasswordVerificationCaptchaActive = false;
            PrimaryButtonEnable = false;
            SetWebViewVisibility?.Invoke(this, false);
            if (string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(validate) || string.IsNullOrWhiteSpace(seccode))
            {
                CancelPasswordVerification();
                Utils.ShowMessageToast("安全验证失败，请重试");
                PrimaryButtonEnable = true;
                return;
            }

            passwordVerificationChallenge = challenge;
            passwordVerificationValidate = validate;
            passwordVerificationSeccode = seccode;
            if (!string.IsNullOrWhiteSpace(recaptchaToken)) passwordVerificationRecaptchaToken = recaptchaToken;

            try
            {
                var phoneResponse = await IsolatedPasswordLoginTransport.SendVerificationAsync(accountApi.FetchHideTel(passwordVerificationTmpToken));
                if (!IsCurrentPasswordOperation(operation)) return;
                if (!phoneResponse.status) throw new FormatException("Password login masked phone lookup failed.");
                var phoneBody = phoneResponse.GetJObject();
                if ((int?)phoneBody["code"] != 0)
                    throw new FormatException("Password login masked phone lookup returned an error.");
                var maskedPhone = (string)phoneBody["data"]?["account_info"]?["hide_tel"];
                if (string.IsNullOrWhiteSpace(maskedPhone))
                    throw new FormatException("Password login masked phone lookup was incomplete.");
                HideTel = maskedPhone;

                await SendPasswordVerificationSMSCore(operation);
            }
            catch (Exception ex)
            {
                if (IsCurrentPasswordOperation(operation))
                {
                    LogHelper.Log("Password login secondary verification failed at SMS stage. exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                    Utils.ShowMessageToast("无法发送安全验证码，请重试");
                }
            }
            finally
            {
                if (IsCurrentPasswordOperation(operation)) PrimaryButtonEnable = true;
            }
        }

        public async void SendPasswordVerificationSMS()
        {
            var operation = passwordSessionOperation;
            if (!PasswordVerificationPending || !EnableSendSMS || !IsCurrentPasswordOperation(operation)) return;
            EnableSendSMS = false;
            PrimaryButtonEnable = false;
            try
            {
                await SendPasswordVerificationSMSCore(operation);
            }
            catch (Exception ex)
            {
                if (IsCurrentPasswordOperation(operation))
                {
                    EnableSendSMS = true;
                    LogHelper.Log("Password login SMS resend failed. exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                    Utils.ShowMessageToast("无法发送安全验证码，请重试");
                }
            }
            finally
            {
                if (IsCurrentPasswordOperation(operation)) PrimaryButtonEnable = true;
            }
        }

        private async Task SendPasswordVerificationSMSCore(long operation)
        {
            var response = await IsolatedPasswordLoginTransport.SendVerificationAsync(accountApi.SendVerifySMS(
                passwordVerificationTmpToken, passwordVerificationRecaptchaToken, passwordVerificationChallenge,
                passwordVerificationGt, passwordVerificationValidate, passwordVerificationSeccode));
            if (!IsCurrentPasswordOperation(operation)) return;
            if (!response.status) throw new FormatException("Password login SMS request failed.");
            var body = response.GetJObject();
            if ((int?)body["code"] != 0) throw new FormatException("Password login SMS request returned an error.");
            var key = (string)body["data"]?["captcha_key"];
            if (string.IsNullOrWhiteSpace(key)) throw new FormatException("Password login SMS request omitted its confirmation key.");
            passwordVerificationCaptchaKey = key;
            PasswordVerificationPending = true;
            Code = "";
            EnableSendSMS = false;
            SMSCountDown = 60;
            smsTimer.Start();
        }

        private async void CompletePasswordLoginCheck()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                Utils.ShowMessageToast("请输入验证码");
                return;
            }
            var operation = passwordSessionOperation;
            if (!IsCurrentPasswordOperation(operation)) return;
            PrimaryButtonEnable = false;
            try
            {
                var response = await IsolatedPasswordLoginTransport.SendVerificationAsync(accountApi.SubmitPwdLoginSMSCheck(
                    Code, passwordVerificationTmpToken, passwordVerificationRequestId, passwordVerificationCaptchaKey));
                if (!IsCurrentPasswordOperation(operation)) return;
                if (!response.status) throw new FormatException("Password login SMS confirmation failed.");
                var body = response.GetJObject();
                if ((int?)body["code"] != 0)
                    throw new FormatException("Password login SMS confirmation returned an error.");
                var exchangeCode = (string)body["data"]?["code"];
                if (string.IsNullOrWhiteSpace(exchangeCode))
                    throw new FormatException("Password login SMS confirmation omitted its exchange code.");

                PasswordLoginSession committedSession = null;
                var committed = await passwordSessionCoordinator.CompleteAsync(operation,
                    () => new IsolatedPasswordLoginTransport(accountApi, Api.ApiHelper.LoginKey).ExchangeAsync(exchangeCode),
                    session =>
                    {
                        if (!account.CommitCookieLogin(session))
                            throw new InvalidOperationException("Password login session could not be persisted.");
                        committedSession = session;
                    });
                if (!committed || committedSession == null || passwordLoginDialogClosed) return;

                PasswordVerificationPending = false;
                PasswordVerificationCaptchaActive = false;
                _ = account.FinishCookieLoginAsync(committedSession);
                if (!passwordLoginDialogClosed && passwordSessionOperation == operation && LoginType == 0)
                    CloseDialog?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                if (IsCurrentPasswordOperation(operation))
                {
                    LogHelper.Log("Password login failed at verified cookie exchange stage. exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                    Utils.ShowMessageToast("安全验证未完成，请检查验证码后重试");
                }
            }
            finally
            {
                if (IsCurrentPasswordOperation(operation)) PrimaryButtonEnable = true;
            }
        }

        private bool IsCurrentPasswordOperation(long operation) =>
            !passwordLoginDialogClosed && LoginType == 0 && passwordSessionCoordinator.IsCurrent(operation);

        private static string GetQueryParameter(string url, string name)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            foreach (var pair in uri.Query.TrimStart('?').Split('&'))
            {
                var separator = pair.IndexOf('=');
                if (separator < 0) continue;
                var key = Uri.UnescapeDataString(pair.Substring(0, separator).Replace('+', ' '));
                if (string.Equals(key, name, StringComparison.Ordinal))
                    return Uri.UnescapeDataString(pair.Substring(separator + 1).Replace('+', ' '));
            }
            return null;
        }

    }
    public class CountryItemModel
    {
        public long id { get; set; }
        public string country_code { get; set; }
        public string countryCode { get { return country_code; } }
        public string cname { get; set; }
    }
    public class SMSResultModel
    {
        public bool is_new { get; set; }
        public string captcha_key { get; set; }

        public string recaptcha_url { get; set; }
    }

    public class LoginResultModel
    {
        public int status { get; set; }
        public string message { get; set; }
        public List<string> sso { get; set; }
        public string url { get; set; }
        public LoginResultTokenModel token_info { get; set; }
        public LoginCookieInfo cookie_info { get; set; }
    }
    public class LoginResultTokenModel
    {
        public long mid { get; set; }
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }
    public class GeetestRequestModel
    {
        public string recaptcha_token { get; set; }
        public string gee_challenge { get; set; }
        public string gee_gt { get; set; }
        public string gee_validate { get; set; }
        public string gee_seccode { get; set; }
    }
   
}

﻿using System;
using Windows.Foundation.Metadata;

namespace BiliLite.JSBridge
{
    [AllowForWeb]
    public sealed class secure
    {
        public event EventHandler<string> CaptchaEvent;
        public void Captcha()
        {
            if (CaptchaEvent != null)
            {
                CaptchaEvent(this, "");
            }
        }

        public event EventHandler<string> CloseCaptchaEvent;
        public void CloseCaptcha()
        {
            if (CloseCaptchaEvent != null)
            {
                CloseCaptchaEvent(this, "");
            }
        }
    }
}

using System;
using Windows.Foundation.Metadata;
using Windows.UI.Popups;

namespace BiliLite.JSBridge
{
    [AllowForWeb]
    public sealed class biliapp
    {
        public void Alert(string message)
        {
            MessageDialog md;
            md = new MessageDialog(message);
            md.ShowAsync();
        }

        public event EventHandler<string> ValidateLoginEvent;

        public void ValidateLogin(string data)
        {
            if (ValidateLoginEvent != null)
            {
                ValidateLoginEvent(this, data);
            }
        }

        public event EventHandler<string> CloseBrowserEvent;

        public void CloseBrowser()
        {
            if (CloseBrowserEvent != null)
            {
                CloseBrowserEvent(this, "");
            }
        }
    }
}
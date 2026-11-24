// XAML/platform primitives only: no send, recovery, cancellation or decoding decisions here.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Windows.UI.Xaml
{
    public class RoutedEventArgs : EventArgs { }
    public enum FocusState { Programmatic }
}
namespace Windows.UI.Xaml.Controls
{
    public class ContentDialog
    {
        public bool IsPrimaryButtonEnabled { get; set; } = true;
        public int HideCount;
        public event Action<ContentDialog, ContentDialogClosingEventArgs> Closing;
        public event Action<ContentDialog, ContentDialogClosedEventArgs> Closed;
        public void Hide() { HideCount++; }
        // Close-start and close-finish are deliberately separate; Hide does not manufacture either.
        public void RaiseClosing() { Closing?.Invoke(this, new ContentDialogClosingEventArgs()); }
        public void RaiseClosed() { Closed?.Invoke(this, new ContentDialogClosedEventArgs()); }
    }
    public class ContentDialogButtonClickEventArgs : EventArgs { public bool Cancel { get; set; } }
    public class ContentDialogClosingEventArgs : EventArgs { }
    public class ContentDialogClosedEventArgs : EventArgs { }
    public class Button { public bool IsEnabled { get; set; } = true; }
    public class TextBox
    {
        public string Text { get; set; } = "";
        public bool IsReadOnly { get; set; }
        public int FocusCount;
        public bool Focus(Windows.UI.Xaml.FocusState state) { FocusCount++; return true; }
    }
    public class TextBlock { public string Text { get; set; } = ""; }
    public class ProgressBar { public Windows.UI.Xaml.Visibility Visibility { get; set; } = Windows.UI.Xaml.Visibility.Collapsed; }
    public class Flyout
    {
        public int ShowCount;
        public void ShowAt(object target) { ShowCount++; }
    }
    public class ItemClickEventArgs : EventArgs { public object ClickedItem { get; set; } }
}
namespace BiliLite.Dialogs
{
    public sealed partial class SendCommentDialog
    {
        public readonly Windows.UI.Xaml.Controls.TextBox txt_Comment = new Windows.UI.Xaml.Controls.TextBox();
        public readonly Windows.UI.Xaml.Controls.Button btnOpenFace = new Windows.UI.Xaml.Controls.Button();
        public readonly Windows.UI.Xaml.Controls.Flyout FaceFlyout = new Windows.UI.Xaml.Controls.Flyout();
        public readonly Windows.UI.Xaml.Controls.ProgressBar SendProgress = new Windows.UI.Xaml.Controls.ProgressBar();
        public readonly Windows.UI.Xaml.Controls.TextBlock SendStatus = new Windows.UI.Xaml.Controls.TextBlock();
        private void InitializeComponent() { }
    }
}
namespace BiliLite.Modules
{
    public enum EmoteBusiness { reply }
    public class EmotePackageItemModel { public string text; }
    public class EmoteVM
    {
        public List<object> Packages;
        public static Func<Task> Delivery = () => Task.CompletedTask;
        public Task GetEmote(EmoteBusiness business) { return Delivery(); }
    }
}

using BiliLite.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Dialogs
{
    public sealed partial class CommentDialog : UserControl
    {
        public bool isvisible;
        private Popup popup;
        public CommentDialog()
        {
            isvisible = true;
            this.InitializeComponent();
            popup = new Popup();
            this.Width = Window.Current.Bounds.Width;
            this.Height = Window.Current.Bounds.Height;
            popup.Child = this;
            this.Loaded += CommentDialog_Loaded;
            this.Unloaded += CommentDialog_Unloaded;
        }

        private void CommentDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }
        public void Show(string oid,int commentMode, Api.CommentApi.CommentSort commentSort)
        {
            this.popup.IsOpen = true;
            comment.LoadComment(new Controls.LoadCommentInfo()
            {
                CommentMode = commentMode,
                CommentSort = commentSort,
                Oid = oid,
                IsDialog=true
            });
        }
        public async void Close()
        {
            await RootBorder.FadeOutAsync(duration: 200);
            this.popup.IsOpen = false;
        }
        private async void CommentDialog_Loaded(object sender, RoutedEventArgs e)
        {


            Window.Current.SizeChanged += Current_SizeChanged;

            await RootBorder.FadeInAsync(duration: 200);


        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.Width = e.Size.Width;
            this.Height = e.Size.Height;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); 
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Close();
        }

        private void RootBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private bool isDragging = false;
        private Point mousePosition;

        private void RootBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            mousePosition = e.GetCurrentPoint(RootBorder).Position;
            isDragging = true;
            RootBorder.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void RootBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDragging = false;
            RootBorder.ReleasePointerCapture(e.Pointer);
            e.Handled = false;
        }
        private void RootBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                // 获取控件的 RenderTransform 对象，如果不存在则创建一个新的 TranslateTransform 对象
                TranslateTransform translateTransform = RootBorder.RenderTransform as TranslateTransform;
                if (translateTransform == null)
                {
                    translateTransform = new TranslateTransform();
                    RootBorder.RenderTransform = translateTransform;
                }

                // 计算当前鼠标位置与上一次记录的鼠标位置之间的偏移量
                Point currentPosition = e.GetCurrentPoint(RootBorder).Position;
                double offsetX = currentPosition.X - mousePosition.X;
                double offsetY = currentPosition.Y - mousePosition.Y;

                // 将偏移量应用于 TranslateTransform 对象
                translateTransform.X += offsetX;
                translateTransform.Y += offsetY;

                // 更新控件的位置
                Canvas.SetLeft(RootBorder, Canvas.GetLeft(RootBorder) + offsetX);
                Canvas.SetTop(RootBorder, Canvas.GetTop(RootBorder) + offsetY);

                // 更新鼠标位置
                mousePosition = currentPosition;
            }
        }
    }
}

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls.Dynamic
{
    public sealed partial class DynamicItemControl : UserControl
    {
        public DynamicItemControl()
        {
            this.InitializeComponent();
        }

        public DynamicItemDisplayModel Model
        {
            get { return (DynamicItemDisplayModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(DynamicItemDisplayModel), typeof(DynamicItemControl), new PropertyMetadata(new DynamicItemDisplayModel()));

        public FrameworkElement CardContent
        {
            get { return (FrameworkElement)GetValue(CardContentProperty); }
            set { SetValue(CardContentProperty, value); }
        }

        public static readonly DependencyProperty CardContentProperty =
            DependencyProperty.Register("CardContent", typeof(FrameworkElement), typeof(DynamicItemControl), new PropertyMetadata(null));
    }
}
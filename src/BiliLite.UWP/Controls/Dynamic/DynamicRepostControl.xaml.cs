using BiliLite.Modules.User;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls
{
    public sealed partial class DynamicRepostControl : UserControl
    {
        public readonly DynamicRepostVM dynamicRepostVM;

        public DynamicRepostControl()
        {
            this.InitializeComponent();
            dynamicRepostVM = new DynamicRepostVM();
        }

        public async void LoadData(string id)
        {
            dynamicRepostVM.ID = id;
            await dynamicRepostVM.GetDynamicItems();
        }
    }
}
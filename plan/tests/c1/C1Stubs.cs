using System.ComponentModel;

namespace BiliLite.Modules
{
    /// <summary>
    /// The product IModules also carries the error-reporting helper, which needs the UWP logging
    /// stack. The detail models only use the change notification part, so that part is stubbed.
    /// </summary>
    public class IModules : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

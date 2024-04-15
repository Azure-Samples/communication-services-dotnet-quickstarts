using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace RawVideo
{
    public static class UIHelper
    {
        public static Task RunOnUIThread(this Page p, Action a)
        {
            return p.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { a(); }).AsTask();
        }
    }
}

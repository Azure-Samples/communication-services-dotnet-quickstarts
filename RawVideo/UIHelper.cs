using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace RawVideo
{
    public static class UIHelper
    {
        public static Task RunOnUIThread(this Page p, Action a)
        {
#if WINDOWS_UWP
            return p.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { a(); }).AsTask();
#else
            var tcs = new TaskCompletionSource();

            var queue = p.DispatcherQueue;
            var result =  queue.TryEnqueue(() =>
                {
                    try
                    {
                        a();
                        tcs.SetResult();
                    }
                    catch(Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                if (!result) tcs.TrySetException(new InvalidOperationException("Failed to queue operation on UI thread"));
            
            return tcs.Task;
#endif
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FlashForgeTimeLapse.ThreadExecuters
{
    public sealed class WpfUiThreadExecutor : IUiThreadExecutor
    {
        public void ExecuteInUiThreadSync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Application application = Application.Current;

            if (application == null || action == null)
            {
                return;
            }

            if (application.CheckAccess())
            {
                action();
                return;
            }

            _ = (application.Dispatcher?.BeginInvoke(priority, action));
        }

        public async Task ExecuteInUiThreadAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Application application = Application.Current;

            if (application == null || action == null)
            {
                return;
            }

            if (application.CheckAccess())
            {
                action();
                return;
            }

            await application.Dispatcher?.InvokeAsync(action, priority);
        }
    }
}

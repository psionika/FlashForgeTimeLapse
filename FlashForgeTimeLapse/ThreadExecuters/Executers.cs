using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FlashForgeTimeLapse.ThreadExecuters
{
    public static class Executers
    {

        private static readonly IUiThreadExecutor _uiThreadExecutor = new WpfUiThreadExecutor();

        public static void ExecuteInUiThreadSync(this Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            try
            {
                _uiThreadExecutor.ExecuteInUiThreadSync(action, priority);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public static async Task ExecuteInUiThreadAsync(this Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            ExceptionDispatchInfo capturedException = null;

            try
            {
                await _uiThreadExecutor.ExecuteInUiThreadAsync(action, priority);
            }
            catch (AggregateException aex)
            {
                Debug.WriteLine(aex);
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);

                Debug.WriteLine(capturedException.SourceException);
            }
        }
    }
}

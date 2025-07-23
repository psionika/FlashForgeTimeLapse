using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FlashForgeTimeLapse.ThreadExecuters
{
    public interface IUiThreadExecutor
    {
        void ExecuteInUiThreadSync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        Task ExecuteInUiThreadAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);
    }
}

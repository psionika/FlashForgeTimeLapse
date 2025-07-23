using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashForgeTimeLapse.ViewModel
{
    public class ViewModelLocator
    {
        public static MainViewModel MainViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    }
}

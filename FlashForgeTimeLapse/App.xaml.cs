using CommunityToolkit.Mvvm.DependencyInjection;
using FlashForgeTimeLapse.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FlashForgeTimeLapse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddSingleton(typeof(MainViewModel));

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            base.OnStartup(e);
        }
    }
}

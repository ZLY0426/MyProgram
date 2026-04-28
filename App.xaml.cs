using MyProgram.Data;
using MyProgram.Interface;
using MyProgram.Services;
using MyProgram.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

namespace MyProgram
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IPasswordService, PasswordService>();
            containerRegistry.Register<IDailyImageService, DailyImageService>();
            containerRegistry.Register<ILogService, LogService>();

            containerRegistry.Register<AppDbContext>();

            containerRegistry.RegisterForNavigation<LoginView>();
            containerRegistry.RegisterForNavigation<RegisterView>();
            containerRegistry.RegisterForNavigation<DashboardView>();
            containerRegistry.RegisterForNavigation<LogView>();

        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
        }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            IRegionManager regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("ContentRegion", nameof(LoginView));  
        }
    }
}
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
            containerRegistry.RegisterSingleton<IPasswordService, PasswordService>();
            containerRegistry.RegisterSingleton<IDailyImageService, DailyImageService>();
            containerRegistry.RegisterSingleton<ICurrentUserService, CurrentUserService>();
            containerRegistry.RegisterSingleton<ILogService, LogService>();
            containerRegistry.RegisterSingleton<IModbusRtuService, ModbusRtuService>();
            containerRegistry.RegisterSingleton<IModbusTcpService, ModbusTcpService>();

            containerRegistry.Register<AppDbContext>();

            containerRegistry.RegisterForNavigation<LoginView>();
            containerRegistry.RegisterForNavigation<RegisterView>();
            containerRegistry.RegisterForNavigation<DashboardView>();
            containerRegistry.RegisterForNavigation<ModbusRtuView>();
            containerRegistry.RegisterForNavigation<ModbusTcpView>();
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
        protected override void OnExit(ExitEventArgs e)
        {
            // 1. 从容器中获取服务
            var currentUser = Container.Resolve<ICurrentUserService>();
            var logService = Container.Resolve<ILogService>();

            // 2. 如果用户已登录，记录【退出】日志
            if (currentUser.IsLoggedIn)
            {
                // 注意：OnExit 是同步的，我们这里使用 .GetAwaiter().GetResult() 强制等待
                // 因为程序马上要关了，必须等日志写完
                logService.LogAsync(currentUser.UserId, currentUser.Username, "退出上位机程序")
                          .GetAwaiter()
                          .GetResult();
            }

            base.OnExit(e);
        }
    }
}
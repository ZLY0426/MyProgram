﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using MyProgram.Data;
using MyProgram.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MyProgram.ViewModels
{
    public class LoginViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;
        private readonly IPasswordService _passwordService;
        private readonly IDailyImageService _imageService;
        private readonly AppDbContext _dbContext;
        // 绑定属性
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }
        
        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public BitmapImage BackgroundImageSource { get; private set; }

        // 命令
        public DelegateCommand LoginCommand { get; }
        public DelegateCommand GoToRegisterCommand { get; }

        public LoginViewModel(IRegionManager regionManager,
                             IPasswordService passwordService,
                             IDailyImageService imageService,
                             AppDbContext dbContext)
        {
            _regionManager = regionManager;
            _passwordService = passwordService;
            _imageService = imageService;
            _dbContext = dbContext;

            LoginCommand = new DelegateCommand(ExecuteLogin);
            GoToRegisterCommand = new DelegateCommand(() =>
                            _regionManager.RequestNavigate("ContentRegion", "RegisterView"));
            // 确保数据库已创建
            _dbContext.Database.EnsureCreated();
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 初始化时加载背景图
            await LoadBackgroundAsync();
        }

        // 异步加载背景
        private async Task LoadBackgroundAsync()
        {
            BackgroundImageSource = await _imageService.GetTodaysBackgroundAsync();
            RaisePropertyChanged(nameof(BackgroundImageSource));
        }

        private void ExecuteLogin()
        {
            // 1. 简单验证
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("请输入用户名和密码", "提示");
                return;
            }

            // 2. 查找用户
            var user = _dbContext.Users.FirstOrDefault(u => u.Username == Username);
            if (user == null)
            {
                MessageBox.Show("用户不存在", "错误");
                return;
            }

            // 3. 验证哈希密码 (关键！不比对明文)
            if (!_passwordService.VerifyPassword(Password, user.PasswordHash))
            {
                MessageBox.Show("密码错误", "错误");
                return;
            }

            // 4. 登录成功
            _regionManager.RequestNavigate("ContentRegion", "DashboardView");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
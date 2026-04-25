using MyProgram.Data;
using MyProgram.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MyProgram.ViewModels
{
    public class RegisterViewModel : BindableBase, INavigationAware
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

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public BitmapImage BackgroundImageSource { get; private set; }

        // 命令
        public DelegateCommand RegisterCommand { get; }
        public DelegateCommand GoToLoginCommand { get; }

        public RegisterViewModel(IRegionManager regionManager,
                               IPasswordService passwordService,
                               IDailyImageService imageService,
                               AppDbContext dbContext)
        {
            _regionManager = regionManager;
            _passwordService = passwordService;
            _imageService = imageService;
            _dbContext = dbContext;

            RegisterCommand = new DelegateCommand(ExecuteRegister);
            GoToLoginCommand = new DelegateCommand(() =>
                _regionManager.RequestNavigate("ContentRegion", "LoginView"));

            // 初始化时加载背景图
            LoadBackground();
            // 确保数据库已创建
            _dbContext.Database.EnsureCreated();
        }

        private void LoadBackground()
        {
            BackgroundImageSource = _imageService.GetTodaysBackground();
            RaisePropertyChanged(nameof(BackgroundImageSource));
        }

        private void ExecuteRegister()
        {
            // 1. 简单验证
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("请输入用户名和密码", "提示");
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("两次输入的密码不一致", "错误");
                return;
            }

            // 2. 检查用户是否已存在
            if (_dbContext.Users.Any(u => u.Username == Username))
            {
                MessageBox.Show("用户名已存在", "错误");
                return;
            }

            // 3. 创建新用户
            var newUser = new Models.User
            {
                Username = Username,
                PasswordHash = _passwordService.HashPassword(Password)
            };

            // 4. 保存到数据库
            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

            // 5. 注册成功，跳转到登录页面
            MessageBox.Show("注册成功，请登录", "成功");
            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }

        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
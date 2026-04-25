using MyProgram.ViewModels;
using System.Windows.Controls;

namespace MyProgram.Views
{
    /// <summary>
    /// RegisterView.xaml 的交互逻辑
    /// </summary>
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
            // 加载完成后设置背景
            Loaded += (s, e) =>
            {
                if (DataContext is RegisterViewModel vm)
                {
                    BackgroundImage.Source = vm.BackgroundImageSource;
                }
            };
        }
    }
}
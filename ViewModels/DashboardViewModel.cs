using Prism.Commands;
using Prism.Mvvm;
using System;

namespace MyProgram.ViewModels
{
    public class DashboardViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public DashboardViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(ExecuteNavigate);
        }

        public DelegateCommand<string> NavigateCommand { get; }

        private void ExecuteNavigate(string viewName)
        {
            if (!string.IsNullOrEmpty(viewName))
            {
                // 核心：请求导航到指定的 View
                _regionManager.RequestNavigate("DashboardRegion", viewName);
            }
        }
    }
}
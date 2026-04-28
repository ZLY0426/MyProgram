using MyProgram.Dtos;
using MyProgram.Interface;
using MyProgram.Models;
using MyProgram.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyProgram.ViewModels
{
    public class LogViewModel : BindableBase
    {
        private readonly ILogService _logService;
        private const int PageSize = 10; // 固定每页10条

        public LogViewModel(ILogService logService)
        {
            _logService = logService;

            // 初始化命令
            LoadedCommand = new DelegateCommand(async () => await ExecuteLoadAsync());
            PrevPageCommand = new DelegateCommand(async () => await ExecutePrevPageAsync(), CanExecutePrevPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            Logs = new ObservableCollection<LogEntry>();
            _currentPage = 1;
        }

        // --- 数据绑定属性 ---

        private ObservableCollection<LogEntry> _logs;
        public ObservableCollection<LogEntry> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        private int _currentPage;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 可选：用于控制加载状态（如显示Loading圈或禁用按钮）
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // --- 命令 ---

        public DelegateCommand LoadedCommand { get; }
        public DelegateCommand PrevPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        // --- 核心方法 (全异步) ---

        /// <summary>
        /// 页面加载触发
        /// </summary>
        private async Task ExecuteLoadAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task ExecutePrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        private bool CanExecutePrevPage()
        {
            return CurrentPage > 1 && !IsLoading;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task ExecuteNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages && !IsLoading;
        }

        /// <summary>
        /// 统一数据加载逻辑
        /// </summary>
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            // 刷新命令状态，防止重复点击
            PrevPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();

            try
            {
                // 调用异步服务获取数据
                var result = await _logService.GetPagedLogsAsync(CurrentPage, PageSize);

                // 更新总页数
                TotalPages = result.TotalPages;

                // 更新 UI 列表 (async/await 自动回到 UI 线程)
                Logs.Clear();
                if (result.Items != null)
                {
                    foreach (var log in result.Items)
                    {
                        Logs.Add(log);
                    }
                }
            }
            catch (Exception ex)
            {
                // 实际项目中这里应该弹窗提示用户
                System.Diagnostics.Debug.WriteLine($"加载日志失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                // 恢复命令状态
                PrevPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
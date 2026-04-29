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
    /// <summary>
    /// 日志页面的 ViewModel，负责日志数据的展示、分页、搜索逻辑
    /// 遵循 Prism MVVM 模式，通过 ViewModelLocator 自动与 LogView 绑定
    /// </summary>
    public class LogViewModel : BindableBase
    {
        #region 私有字段与常量

        /// <summary>
        /// 日志服务接口，负责与数据库交互
        /// 通过依赖注入 (DI) 注入，实现解耦
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// 常量：固定每页显示的日志条数
        /// 配合自定义 FillDataGrid 的行高自适应逻辑使用
        /// </summary>
        private const int PageSize = 10;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数：初始化 ViewModel
        /// </summary>
        /// <param name="logService">由 Prism 容器注入的日志服务实例</param>
        public LogViewModel(ILogService logService)
        {
            _logService = logService;

            // --- 初始化命令 ---
            // 页面加载命令：触发初始数据加载
            LoadedCommand = new DelegateCommand(async () => await ExecuteLoadAsync());

            // 上一页命令：带 CanExecute 条件判断，防止在第一页时点击
            PrevPageCommand = new DelegateCommand(async () => await ExecutePrevPageAsync(), CanExecutePrevPage);

            // 下一页命令：带 CanExecute 条件判断，防止在最后一页时点击
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            // --- 新增：搜索相关命令 ---
            // 执行搜索命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            // 取消搜索命令：恢复显示全部数据
            CancelSearchCommand = new DelegateCommand(async () => await ExecuteCancelSearchAsync());

            // --- 初始化数据集合 ---
            Logs = new ObservableCollection<LogEntry>();

            // 初始化搜索类型下拉框选项
            SearchTypes = new ObservableCollection<string> { "时间", "用户ID", "用户名" };

            // 默认选中“时间”作为搜索类型
            _selectedSearchType = "时间";

            // 初始化当前页码为第一页
            _currentPage = 1;

            // 默认处于非搜索模式
            _isSearchMode = false;
        }

        #endregion

        #region 数据绑定属性 (Bindable Properties)

        /// <summary>
        /// 日志列表集合：UI 上 DataGrid 的 ItemsSource
        /// ObservableCollection 实现了 INotifyCollectionChanged，
        /// 当添加/删除元素时，UI 会自动更新
        /// </summary>
        private ObservableCollection<LogEntry> _logs;
        public ObservableCollection<LogEntry> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        /// <summary>
        /// 当前页码：用于分页逻辑
        /// </summary>
        private int _currentPage;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 总页数：由数据库返回的总条数计算得出
        /// </summary>
        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 加载状态标志位：防止重复点击按钮导致重复请求
        /// </summary>
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 搜索相关属性

        /// <summary>
        /// 搜索类型下拉框的数据源：提供“时间/用户ID/用户名”三个选项
        /// </summary>
        public ObservableCollection<string> SearchTypes { get; }

        /// <summary>
        /// 当前选中的搜索类型：与 ComboBox 的 SelectedItem 绑定
        /// </summary>
        private string _selectedSearchType;
        public string SelectedSearchType
        {
            get => _selectedSearchType;
            set => SetProperty(ref _selectedSearchType, value);
        }

        /// <summary>
        /// 搜索框输入的文本：与 TextBox 的 Text 绑定
        /// UpdateSourceTrigger=PropertyChanged 表示输入时立即更新，而不是失去焦点才更新
        /// </summary>
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 搜索模式标志位：
        /// true = 当前显示的是搜索结果
        /// false = 当前显示的是全部数据
        /// </summary>
        private bool _isSearchMode;
        public bool IsSearchMode
        {
            get => _isSearchMode;
            set => SetProperty(ref _isSearchMode, value);
        }

        #endregion

        #region 命令 (Commands)

        /// <summary>
        /// 页面加载完成时触发的命令
        /// </summary>
        public DelegateCommand LoadedCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PrevPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        /// <summary>
        /// 执行搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 取消搜索命令
        /// </summary>
        public DelegateCommand CancelSearchCommand { get; }

        #endregion

        #region 核心业务逻辑方法

        /// <summary>
        /// 页面加载时的入口方法
        /// </summary>
        private async Task ExecuteLoadAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 【搜索核心逻辑】执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            // 边界检查：如果搜索框为空，视为取消搜索
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await ExecuteCancelSearchAsync();
                return;
            }

            // 1. 标记为搜索模式
            IsSearchMode = true;

            // 2. 重置到第一页（搜索结果从第一页开始看）
            CurrentPage = 1;

            // 3. 重新加载数据（此时会走搜索分支）
            await LoadDataAsync();
        }

        /// <summary>
        /// 【搜索核心逻辑】取消搜索，恢复显示全部数据
        /// </summary>
        private async Task ExecuteCancelSearchAsync()
        {
            // 1. 退出搜索模式
            IsSearchMode = false;

            // 2. 清空搜索框文本
            SearchText = string.Empty;

            // 3. 重置到第一页
            CurrentPage = 1;

            // 4. 重新加载数据（此时会走普通查询分支）
            await LoadDataAsync();
        }

        /// <summary>
        /// 上一页逻辑
        /// </summary>
        private async Task ExecutePrevPageAsync()
        {
            // 只有当前页大于1时才执行
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// 上一页按钮的可用状态判断
        /// </summary>
        /// <returns>true=可以点击，false=禁用</returns>
        private bool CanExecutePrevPage()
        {
            // 条件：不在第一页 且 不在加载中
            return CurrentPage > 1 && !IsLoading;
        }

        /// <summary>
        /// 下一页逻辑
        /// </summary>
        private async Task ExecuteNextPageAsync()
        {
            // 只有当前页小于总页数时才执行
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// 下一页按钮的可用状态判断
        /// </summary>
        /// <returns>true=可以点击，false=禁用</returns>
        private bool CanExecuteNextPage()
        {
            // 条件：不在最后一页 且 不在加载中
            return CurrentPage < TotalPages && !IsLoading;
        }

        /// <summary>
        /// 【核心中的核心】统一数据加载逻辑
        /// 根据 IsSearchMode 标志位，自动决定是调用“搜索接口”还是“普通查询接口”
        /// </summary>
        private async Task LoadDataAsync()
        {
            // 防重复点击：如果正在加载中，直接返回
            if (IsLoading) return;

            // 1. 开始加载，设置标志位
            IsLoading = true;

            // 2. 刷新命令状态（禁用按钮，防止重复点击）
            PrevPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();

            try
            {
                // 声明结果变量
                PagedResult<LogEntry> result;

                // 3. 分支判断：搜索模式 OR 普通模式
                if (IsSearchMode)
                {
                    // 搜索模式：调用 SearchLogsAsync
                    result = await _logService.SearchLogsAsync(SelectedSearchType, SearchText, CurrentPage, PageSize);
                }
                else
                {
                    // 普通模式：调用 GetPagedLogsAsync
                    result = await _logService.GetPagedLogsAsync(CurrentPage, PageSize);
                }

                // 4. 更新总页数
                TotalPages = result.TotalPages;

                // 5. 更新 UI 列表
                // 注意：async/await 会自动将上下文切回 UI 线程，
                // 所以这里可以直接操作 ObservableCollection，不会报跨线程异常
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
                // 异常处理：实际项目中这里应该使用弹窗或消息通知用户
                // 这里仅输出到调试窗口
                System.Diagnostics.Debug.WriteLine($"加载日志失败: {ex.Message}");
            }
            finally
            {
                // 6. 无论成功或失败，最后都要恢复状态
                IsLoading = false;

                // 7. 恢复按钮可用状态
                PrevPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}
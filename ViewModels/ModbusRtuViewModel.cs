using MyProgram.Interface;
using MyProgram.Models;
using MyProgram.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MyProgram.ViewModels
{
    public class ModbusRtuViewModel : BindableBase
    {
        private readonly IModbusRtuService _modbusService;
        private readonly ILogService _logService;         // 新增：日志服务
        private readonly ICurrentUserService _currentUser; // 新增：当前用户服务
        private DispatcherTimer _pollingTimer;

        public ModbusRtuViewModel(IModbusRtuService modbusService,
                                  ILogService logService,        // 构造函数注入
                                  ICurrentUserService currentUser)
        {
            _modbusService = modbusService;
            _logService = logService;
            _currentUser = currentUser;

            // 订阅通信日志事件
            _modbusService.OnLogReceived += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    CommunicationLogs.Insert(0, e);
                    if (CommunicationLogs.Count > 100)
                        CommunicationLogs.RemoveAt(CommunicationLogs.Count - 1);
                });
            };

            // 初始化命令
            ConnectCommand = new DelegateCommand(async () => await ExecuteConnectAsync(), CanExecuteConnect);
            DisconnectCommand = new DelegateCommand(async () => await ExecuteDisconnectAsync(), CanExecuteDisconnect);
            ReadCommand = new DelegateCommand(async () => await ExecuteReadAsync(), CanExecuteReadWrite);
            WriteCommand = new DelegateCommand(async () => await ExecuteWriteAsync(), CanExecuteReadWrite);
            ToggleAutoPollingCommand = new DelegateCommand(ToggleAutoPolling);

            // 初始化数据
            SerialPorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            BaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };
            DataBitsOptions = new ObservableCollection<int> { 7, 8 };
            StopBitsOptions = new ObservableCollection<StopBits> { StopBits.One, StopBits.Two };
            ParityOptions = new ObservableCollection<Parity> { Parity.None, Parity.Odd, Parity.Even };

            Registers = new ObservableCollection<ModbusRegister>();
            CommunicationLogs = new ObservableCollection<CommunicationLog>();

            // 默认值
            _selectedBaudRate = 9600;
            _selectedDataBits = 8;
            _selectedStopBits = StopBits.One;
            _selectedParity = Parity.None;
            _slaveId = 1;
            _startAddress = 0;
            _registerCount = 10;
            _writeValueInput = string.Empty; // 修改：改为字符串输入
            _pollingInterval = 1000;
            _isAutoPolling = false;

            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Tick += async (s, e) => await ExecuteReadAsync();
        }

        #region 串口配置属性 (保持不变)
        public ObservableCollection<string> SerialPorts { get; }
        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set { SetProperty(ref _selectedPort, value); ConnectCommand.RaiseCanExecuteChanged(); }
        }

        public ObservableCollection<int> BaudRates { get; }
        private int _selectedBaudRate;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        public ObservableCollection<int> DataBitsOptions { get; }
        private int _selectedDataBits;
        public int SelectedDataBits
        {
            get => _selectedDataBits;
            set => SetProperty(ref _selectedDataBits, value);
        }

        public ObservableCollection<StopBits> StopBitsOptions { get; }
        private StopBits _selectedStopBits;
        public StopBits SelectedStopBits
        {
            get => _selectedStopBits;
            set => SetProperty(ref _selectedStopBits, value);
        }

        public ObservableCollection<Parity> ParityOptions { get; }
        private Parity _selectedParity;
        public Parity SelectedParity
        {
            get => _selectedParity;
            set => SetProperty(ref _selectedParity, value);
        }
        #endregion

        #region 连接状态属性 (保持不变)
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                ConnectCommand.RaiseCanExecuteChanged();
                DisconnectCommand.RaiseCanExecuteChanged();
                ReadCommand.RaiseCanExecuteChanged();
                WriteCommand.RaiseCanExecuteChanged();
            }
        }

        public string ConnectionStatusText => IsConnected ? "已连接" : "未连接";
        public string ConnectionStatusColor => IsConnected ? "#FF4CAF50" : "#FFF44336";
        #endregion

        #region 采集配置属性 (修改写入值为字符串)
        private byte _slaveId;
        public byte SlaveId
        {
            get => _slaveId;
            set => SetProperty(ref _slaveId, value);
        }

        private ushort _startAddress;
        public ushort StartAddress
        {
            get => _startAddress;
            set => SetProperty(ref _startAddress, value);
        }

        private ushort _registerCount;
        public ushort RegisterCount
        {
            get => _registerCount;
            set => SetProperty(ref _registerCount, value);
        }

        // 修改：从 ushort 改为 string，支持逗号分隔
        private string _writeValueInput;
        public string WriteValueInput
        {
            get => _writeValueInput;
            set => SetProperty(ref _writeValueInput, value);
        }
        #endregion

        #region 定时采集属性 (保持不变)
        private bool _isAutoPolling;
        public bool IsAutoPolling
        {
            get => _isAutoPolling;
            set { SetProperty(ref _isAutoPolling, value); RaisePropertyChanged(nameof(AutoPollingButtonText)); }
        }

        private int _pollingInterval;
        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        public string AutoPollingButtonText => IsAutoPolling ? "停止采集" : "启动采集";
        #endregion

        #region 数据展示属性 (保持不变)
        public ObservableCollection<ModbusRegister> Registers { get; }
        public ObservableCollection<CommunicationLog> CommunicationLogs { get; }
        #endregion

        #region 命令 (保持不变)
        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand DisconnectCommand { get; }
        public DelegateCommand ReadCommand { get; }
        public DelegateCommand WriteCommand { get; }
        public DelegateCommand ToggleAutoPollingCommand { get; }
        #endregion

        #region 核心方法 (修改连接/断开/写入逻辑)

        // 修改：连接成功后记录日志
        private async Task ExecuteConnectAsync()
        {
            try
            {
                await _modbusService.ConnectAsync(SelectedPort, SelectedBaudRate, SelectedDataBits, SelectedStopBits, SelectedParity);
                IsConnected = true;
                RaisePropertyChanged(nameof(ConnectionStatusText));
                RaisePropertyChanged(nameof(ConnectionStatusColor));

                // 新增：记录用户行为日志
                if (_currentUser.IsLoggedIn)
                {
                    await _logService.LogAsync(_currentUser.UserId, _currentUser.Username,
                        $"Modbus RTU 连接成功 (端口: {SelectedPort})");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"连接失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteConnect() => !IsConnected && !string.IsNullOrEmpty(SelectedPort);

        // 修改：断开连接前记录日志
        private async Task ExecuteDisconnectAsync()
        {
            // 先停止定时采集
            if (IsAutoPolling) ToggleAutoPolling();

            // 新增：记录用户行为日志
            if (_currentUser.IsLoggedIn)
            {
                await _logService.LogAsync(_currentUser.UserId, _currentUser.Username,
                    $"Modbus RTU 断开连接 (端口: {SelectedPort})");
            }

            await _modbusService.DisconnectAsync();
            IsConnected = false;
            RaisePropertyChanged(nameof(ConnectionStatusText));
            RaisePropertyChanged(nameof(ConnectionStatusColor));
        }

        private bool CanExecuteDisconnect() => IsConnected;

        private async Task ExecuteReadAsync()
        {
            try
            {
                var values = await _modbusService.ReadHoldingRegistersAsync(SlaveId, StartAddress, RegisterCount);

                Registers.Clear();
                for (int i = 0; i < values.Length; i++)
                {
                    Registers.Add(new ModbusRegister
                    {
                        Address = StartAddress + i,
                        Value = values[i],
                        Description = GetRegisterDescription(StartAddress + i)
                    });
                }
            }
            catch (Exception ex)
            {
                // 错误已在 Service 中记录
            }
        }

        // 修改：解析逗号分隔的输入，支持单/多个写入
        private async Task ExecuteWriteAsync()
        {
            try
            {
                // 1. 解析输入
                if (string.IsNullOrWhiteSpace(WriteValueInput))
                {
                    System.Windows.MessageBox.Show("请输入写入值", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 2. 分割字符串并转换为 ushort 数组
                string[] parts = WriteValueInput.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                ushort[] values = new ushort[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    if (!ushort.TryParse(parts[i].Trim(), out ushort val))
                    {
                        System.Windows.MessageBox.Show($"输入格式错误: '{parts[i].Trim()}' 不是有效的数值", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    values[i] = val;
                }

                // 3. 调用统一写入方法
                await _modbusService.WriteRegistersAsync(SlaveId, StartAddress, values);

                // 4. 写入成功后重新读取
                await ExecuteReadAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"写入失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteReadWrite() => IsConnected;

        private void ToggleAutoPolling()
        {
            if (IsAutoPolling)
            {
                _pollingTimer.Stop();
                IsAutoPolling = false;
            }
            else
            {
                _pollingTimer.Interval = TimeSpan.FromMilliseconds(PollingInterval);
                _pollingTimer.Start();
                IsAutoPolling = true;
            }
        }

        private string GetRegisterDescription(int address)
        {
            return address switch
            {
                0 => "设备状态",
                1 => "温度 (℃)",
                2 => "湿度 (%)",
                3 => "压力 (kPa)",
                4 => "流量 (L/min)",
                _ => "保留寄存器"
            };
        }

        #endregion
    }
}
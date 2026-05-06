using MyProgram.Interface;
using MyProgram.Models;
using MyProgram.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MyProgram.ViewModels
{
    public class ModbusTcpViewModel : BindableBase
    {
        private readonly IModbusTcpService _modbusService;
        private DispatcherTimer _pollingTimer;
        private int _currentPollingIndex = 0;

        public ModbusTcpViewModel(IModbusTcpService modbusService)
        {
            _modbusService = modbusService;

            // 初始化命令（完全匹配XAML绑定，无遗漏）
            LoadedCommand = new DelegateCommand(async () => await ExecuteLoadAsync());
            ConnectCommand = new DelegateCommand(async () => await ExecuteConnectAsync(), CanExecuteConnect);
            DisconnectCommand = new DelegateCommand(async () => await ExecuteDisconnectAsync(), CanExecuteDisconnect);
            ScanSlavesCommand = new DelegateCommand(async () => await ExecuteScanSlavesAsync(), CanExecuteScan);
            ReadCommand = new DelegateCommand(async () => await ExecuteManualReadAsync(), CanExecuteReadWrite);
            WriteCommand = new DelegateCommand(async () => await ExecuteWriteAsync(), CanExecuteReadWrite);
            ToggleAutoPollingCommand = new DelegateCommand(ToggleAutoPolling, CanExecuteTogglePolling);
            RemoveSlaveCommand = new DelegateCommand<SlaveDevice>(ExecuteRemoveSlave, CanExecuteRemoveSlave);

            // 初始化数据
            Registers = new ObservableCollection<ModbusRegister>();
            SlaveDevices = new ObservableCollection<SlaveDevice>();

            // 默认配置
            _ipAddress = "127.0.0.1";
            _port = 502;
            _startAddress = 0;
            _registerCount = 10;
            _writeValueInput = string.Empty;
            _pollingInterval = 2000;
            _isAutoPolling = false;
            _isScanning = false;
            _scanStartId = 1;
            _scanEndId = 10;

            // 轮询定时器
            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Tick += async (s, e) => await ExecutePollingCycleAsync();
        }

        #region 从站模型
        public class SlaveDevice : BindableBase
        {
            private byte _slaveId;
            public byte SlaveId
            {
                get => _slaveId;
                set => SetProperty(ref _slaveId, value);
            }

            private string _name;
            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            private bool _isOnline;
            public bool IsOnline
            {
                get => _isOnline;
                set => SetProperty(ref _isOnline, value);
            }
        }
        #endregion

        #region TCP 配置属性
        private string _ipAddress;
        public string IPAddress
        {
            get => _ipAddress;
            set { SetProperty(ref _ipAddress, value); ConnectCommand.RaiseCanExecuteChanged(); }
        }

        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }
        #endregion

        #region 扫描范围配置
        private byte _scanStartId;
        public byte ScanStartId
        {
            get => _scanStartId;
            set => SetProperty(ref _scanStartId, value);
        }

        private byte _scanEndId;
        public byte ScanEndId
        {
            get => _scanEndId;
            set => SetProperty(ref _scanEndId, value);
        }
        #endregion

        #region 连接状态属性
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                // 所有命令的启用状态同步更新
                ConnectCommand.RaiseCanExecuteChanged();
                DisconnectCommand.RaiseCanExecuteChanged();
                ScanSlavesCommand.RaiseCanExecuteChanged();
                ReadCommand.RaiseCanExecuteChanged();
                WriteCommand.RaiseCanExecuteChanged();
                RemoveSlaveCommand.RaiseCanExecuteChanged();
                ToggleAutoPollingCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(ConnectionStatusText));
                RaisePropertyChanged(nameof(ConnectionStatusColor));
            }
        }

        public string ConnectionStatusText => IsConnected ? "已连接" : "未连接";
        public string ConnectionStatusColor => IsConnected ? "#FF4CAF50" : "#FFF44336";
        #endregion

        #region 扫描状态属性
        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                SetProperty(ref _isScanning, value);
                ScanSlavesCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(ScanButtonText));
            }
        }

        public string ScanButtonText => IsScanning ? "扫描中..." : "扫描从站";
        #endregion

        #region 从站列表属性
        public ObservableCollection<SlaveDevice> SlaveDevices { get; }

        private SlaveDevice _selectedSlave;
        public SlaveDevice SelectedSlave
        {
            get => _selectedSlave;
            set
            {
                SetProperty(ref _selectedSlave, value);
                // 选中从站时，立即刷新数据（同步UI）
                if (value != null && IsConnected)
                {
                    _= ExecuteManualReadAsync();
                }
                // 命令状态同步更新
                ReadCommand.RaiseCanExecuteChanged();
                WriteCommand.RaiseCanExecuteChanged();
                RemoveSlaveCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region 采集配置属性
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

        private string _writeValueInput;
        public string WriteValueInput
        {
            get => _writeValueInput;
            set => SetProperty(ref _writeValueInput, value);
        }
        #endregion

        #region 定时采集属性
        private bool _isAutoPolling;
        public bool IsAutoPolling
        {
            get => _isAutoPolling;
            set
            {
                SetProperty(ref _isAutoPolling, value);
                RaisePropertyChanged(nameof(AutoPollingButtonText));
                ToggleAutoPollingCommand.RaiseCanExecuteChanged();
            }
        }

        private int _pollingInterval;
        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        public string AutoPollingButtonText => IsAutoPolling ? "停止轮询" : "启动轮询";
        #endregion

        #region 数据展示属性（修正地址映射，和Modbus Slave完全对应）
        public ObservableCollection<ModbusRegister> Registers { get; }
        // 地址映射：地址1=温度，地址2=湿度，地址3=压力，地址4=流量
        public int Temperature => Math.Clamp(GetRegisterValue(1), 0, 200);
        public int Humidity => Math.Clamp(GetRegisterValue(2), 0, 100);
        public int Pressure => Math.Clamp(GetRegisterValue(3), 0, 1000);
        public int Flow => Math.Clamp(GetRegisterValue(4), 0, 500);

        private int GetRegisterValue(int addressOffset)
        {
            int targetAddress = StartAddress + addressOffset;
            var reg = Registers.FirstOrDefault(r => r.Address == targetAddress);
            return reg?.Value ?? 0;
        }
        #endregion

        #region 命令
        public DelegateCommand LoadedCommand { get; }
        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand DisconnectCommand { get; }
        public DelegateCommand ScanSlavesCommand { get; }
        public DelegateCommand ReadCommand { get; }
        public DelegateCommand WriteCommand { get; }
        public DelegateCommand ToggleAutoPollingCommand { get; }
        public DelegateCommand<SlaveDevice> RemoveSlaveCommand { get; }
        #endregion

        #region 核心业务逻辑（全修正）
        private async Task ExecuteLoadAsync() { }

        /// <summary>
        /// 连接逻辑：只建立TCP连接，不自动扫描
        /// </summary>
        private async Task ExecuteConnectAsync()
        {
            try
            {
                await _modbusService.ConnectAsync(IPAddress, Port);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"连接失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteConnect() => !IsConnected && !string.IsNullOrWhiteSpace(IPAddress);

        /// <summary>
        /// 断开逻辑：清空所有数据，重置状态
        /// </summary>
        private async Task ExecuteDisconnectAsync()
        {
            if (IsAutoPolling) ToggleAutoPolling();

            // 清空所有数据
            SlaveDevices.Clear();
            Registers.Clear();
            RaiseAllDataPropertyChanged();

            await _modbusService.DisconnectAsync();
            IsConnected = false;
        }

        private bool CanExecuteDisconnect() => IsConnected;

        /// <summary>
        /// 手动读取：核心读取逻辑，选中从站/手动刷新都会调用
        /// </summary>
        private async Task ExecuteManualReadAsync()
        {
            if (SelectedSlave == null || !IsConnected) return;

            try
            {
                // 读取当前选中从站的全量寄存器
                var values = await _modbusService.ReadHoldingRegistersAsync(SelectedSlave.SlaveId, StartAddress, RegisterCount);

                // 清空并更新寄存器列表（UI同步）
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

                // 强制刷新仪表盘所有属性
                RaiseAllDataPropertyChanged();
                SelectedSlave.IsOnline = true;
            }
            catch (Exception ex)
            {
                SelectedSlave.IsOnline = false;
                System.Windows.MessageBox.Show($"读取失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteReadWrite() => IsConnected && SelectedSlave != null;

        /// <summary>
        /// 扫描从站逻辑：核心修正，加ID校验，只识别真实从站
        /// </summary>
        private async Task ExecuteScanSlavesAsync()
        {
            if (!IsConnected || ScanStartId > ScanEndId) return;

            IsScanning = true;
            SlaveDevices.Clear();
            Registers.Clear();
            RaiseAllDataPropertyChanged();

            try
            {
                // 遍历扫描范围
                for (byte id = ScanStartId; id <= ScanEndId; id++)
                {
                    try
                    {
                        // 核心：读取地址0，判断返回值是否等于Slave ID（校验真实从站）
                        var result = await _modbusService.ReadHoldingRegistersAsync(id, 0, 1);
                        if (result.Length > 0 && result[0] == id)
                        {
                            // 校验通过，加入从站列表
                            var newSlave = new SlaveDevice
                            {
                                SlaveId = id,
                                Name = $"从站设备 {id}",
                                IsOnline = true
                            };
                            SlaveDevices.Add(newSlave);

                            // 自动选中第一个扫描到的从站
                            if (SelectedSlave == null)
                                SelectedSlave = newSlave;
                        }
                    }
                    catch
                    {
                        // 校验不通过，跳过该ID
                    }
                }
            }
            finally
            {
                IsScanning = false;
            }
        }

        private bool CanExecuteScan() => IsConnected && !IsScanning;

        /// <summary>
        /// 删除从站逻辑
        /// </summary>
        private void ExecuteRemoveSlave(SlaveDevice slave)
        {
            if (slave == null) return;

            // 删除选中的从站，清空UI
            if (slave == SelectedSlave)
            {
                Registers.Clear();
                RaiseAllDataPropertyChanged();
                SelectedSlave = null;
            }
            SlaveDevices.Remove(slave);
        }

        private bool CanExecuteRemoveSlave(SlaveDevice slave)
        {
            return IsConnected && slave != null;
        }

        /// <summary>
        /// 写入寄存器逻辑
        /// </summary>
        private async Task ExecuteWriteAsync()
        {
            if (SelectedSlave == null) return;

            try
            {
                if (string.IsNullOrWhiteSpace(WriteValueInput))
                {
                    System.Windows.MessageBox.Show("请输入写入值", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 解析写入值
                string[] parts = WriteValueInput.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                ushort[] values = new ushort[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    if (!ushort.TryParse(parts[i].Trim(), out ushort val))
                    {
                        System.Windows.MessageBox.Show($"输入格式错误: '{parts[i].Trim()}'", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    values[i] = val;
                }

                // 执行写入
                await _modbusService.WriteRegistersAsync(SelectedSlave.SlaveId, StartAddress, values);
                // 写入后立即刷新数据
                await ExecuteManualReadAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"写入失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 自动轮询逻辑
        /// </summary>
        private async Task ExecutePollingCycleAsync()
        {
            if (!IsConnected || SlaveDevices.Count == 0) return;

            var currentSlave = SlaveDevices[_currentPollingIndex];

            try
            {
                var values = await _modbusService.ReadHoldingRegistersAsync(currentSlave.SlaveId, StartAddress, RegisterCount);

                // 只有当前选中的从站，才更新UI
                if (currentSlave == SelectedSlave)
                {
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
                    RaiseAllDataPropertyChanged();
                }
                currentSlave.IsOnline = true;
            }
            catch
            {
                currentSlave.IsOnline = false;
            }

            // 轮询索引循环
            _currentPollingIndex = (_currentPollingIndex + 1) % SlaveDevices.Count;
        }

        private void ToggleAutoPolling()
        {
            if (IsAutoPolling)
            {
                _pollingTimer.Stop();
                IsAutoPolling = false;
            }
            else
            {
                _currentPollingIndex = 0;
                _pollingTimer.Interval = TimeSpan.FromMilliseconds(PollingInterval);
                _pollingTimer.Start();
                IsAutoPolling = true;
            }
        }

        private bool CanExecuteTogglePolling() => IsConnected && SlaveDevices.Count > 0;

        /// <summary>
        /// 寄存器描述（修正，和地址映射完全对应）
        /// </summary>
        private string GetRegisterDescription(int address)
        {
            return address switch
            {
                0 => "从站ID标识",
                1 => "温度 (℃)",
                2 => "湿度 (%)",
                3 => "压力 (kPa)",
                4 => "流量 (L/min)",
                _ => "保留寄存器"
            };
        }

        /// <summary>
        /// 辅助方法：强制刷新所有仪表盘属性
        /// </summary>
        private void RaiseAllDataPropertyChanged()
        {
            RaisePropertyChanged(nameof(Temperature));
            RaisePropertyChanged(nameof(Humidity));
            RaisePropertyChanged(nameof(Pressure));
            RaisePropertyChanged(nameof(Flow));
        }
        #endregion
    }
}
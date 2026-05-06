using MyProgram.Interface;
using MyProgram.Models;
using NModbus;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyProgram.Services
{
    public class ModbusTcpService : IModbusTcpService
    {
        private TcpClient _tcpClient;
        private IModbusMaster _modbusMaster;
        private readonly SemaphoreSlim _communicationLock = new SemaphoreSlim(1, 1);
        private readonly ILogService _logService;
        private readonly ICurrentUserService _currentUser;

        public ModbusTcpService(ILogService logService, ICurrentUserService currentUser)
        {
            _logService = logService;
            _currentUser = currentUser;
        }

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            if (IsConnected) await DisconnectAsync();

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ipAddress, port);

                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateMaster(_tcpClient);

                await LogUserActionAsync($"Modbus TCP 连接成功 ({ipAddress}:{port})");
            }
            catch (Exception ex)
            {
                await LogUserActionAsync($"Modbus TCP 连接失败 ({ipAddress}:{port}): {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            await _communicationLock.WaitAsync();
            try
            {
                if (_modbusMaster != null)
                {
                    _modbusMaster.Dispose();
                    _modbusMaster = null;
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                    _tcpClient = null;
                    await LogUserActionAsync("Modbus TCP 断开连接");
                }
            }
            finally
            {
                _communicationLock.Release();
            }
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接到设备");

            await _communicationLock.WaitAsync();
            try
            {
                var result = await _modbusMaster.ReadHoldingRegistersAsync(slaveId, startAddress, count);
                return result;
            }
            catch (Exception ex)
            {
                await LogUserActionAsync($"Modbus TCP 读取失败: {ex.Message}");
                throw;
            }
            finally
            {
                _communicationLock.Release();
            }
        }

        public async Task WriteRegistersAsync(byte slaveId, ushort startAddress, ushort[] values)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接到设备");
            if (values == null || values.Length == 0) throw new ArgumentException("写入值不能为空");

            await _communicationLock.WaitAsync();
            try
            {
                string valueStr = string.Join(", ", values);
                await _modbusMaster.WriteMultipleRegistersAsync(slaveId, startAddress, values);
                await LogUserActionAsync($"Modbus TCP 写入 [Slave {slaveId}] 起始地址 {startAddress}, 值=[{valueStr}]");
            }
            catch (Exception ex)
            {
                await LogUserActionAsync($"Modbus TCP 写入失败: {ex.Message}");
                throw;
            }
            finally
            {
                _communicationLock.Release();
            }
        }

        private async Task LogUserActionAsync(string action)
        {
            if (_currentUser.IsLoggedIn)
            {
                try
                {
                    await _logService.LogAsync(_currentUser.UserId, _currentUser.Username, action);
                }
                catch { }
            }
        }
    }
}
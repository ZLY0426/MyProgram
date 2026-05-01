using MyProgram.Interface;
using MyProgram.Models;
using NModbus;
using NModbus.Serial;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace MyProgram.Services
{
    public class ModbusRtuService : IModbusRtuService
    {
        private SerialPort _serialPort;
        private IModbusMaster _modbusMaster;

        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public event EventHandler<CommunicationLog> OnLogReceived;

        public async Task ConnectAsync(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
        {
            if (IsConnected) await DisconnectAsync();

            try
            {
                _serialPort = new SerialPort(portName)
                {
                    BaudRate = baudRate,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    Parity = parity
                };

                _serialPort.Open();

                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateRtuMaster(_serialPort);

                Log("连接", $"成功连接到 {portName}");
            }
            catch (Exception ex)
            {
                Log("错误", $"连接失败: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_modbusMaster != null)
            {
                _modbusMaster.Dispose();
                _modbusMaster = null;
            }

            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
                Log("断开", "连接已关闭");
            }
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接到设备");

            try
            {
                Log("发送", $"读取 从站={slaveId}, 起始地址={startAddress}, 数量={count}");

                // 注意：NModbus 是同步的，我们用 Task.Run 包装以不阻塞 UI
                var result = await _modbusMaster.ReadHoldingRegistersAsync(slaveId, startAddress, count);
                Log("接收", $"收到数据: [{string.Join(", ", result)}]");
                return result;
            }
            catch (Exception ex)
            {
                Log("错误", $"读取失败: {ex.Message}");
                throw;
            }
        }

        public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接到设备");

            try
            {
                Log("发送", $"写入 从站={slaveId}, 地址={address}, 值={value}");

                await _modbusMaster.WriteSingleRegisterAsync(slaveId, address, value);

                Log("接收", "写入成功");
            }
            catch (Exception ex)
            {
                Log("错误", $"写入失败: {ex.Message}");
                throw;
            }
        }

        private void Log(string type, string message)
        {
            OnLogReceived?.Invoke(this, new CommunicationLog { Type = type, Message = message });
        }
    }
}
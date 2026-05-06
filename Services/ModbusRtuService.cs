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

                var result = await Task.Run(() => _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, count));

                Log("接收", $"收到数据: [{string.Join(", ", result)}]");
                return result;
            }
            catch (Exception ex)
            {
                Log("错误", $"读取失败: {ex.Message}");
                throw;
            }
        }

        // 修改：统一写入方法，支持单/多个寄存器
        public async Task WriteRegistersAsync(byte slaveId, ushort startAddress, ushort[] values)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接到设备");
            if (values == null || values.Length == 0) throw new ArgumentException("写入值不能为空");

            try
            {
                string valueStr = string.Join(", ", values);
                Log("发送", $"写入 从站={slaveId}, 起始地址={startAddress}, 数量={values.Length}, 值=[{valueStr}]");

                // 核心：统一使用 WriteMultipleRegisters
                // 注意：NModbus 的 WriteMultipleRegisters 完全支持写入单个寄存器
                await Task.Run(() => _modbusMaster.WriteMultipleRegisters(slaveId, startAddress, values));

                Log("接收", $"写入成功 (数量: {values.Length})");
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
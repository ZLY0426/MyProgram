using MyProgram.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Interface
{
    public interface IModbusRtuService
    {
        bool IsConnected { get; }

        // 连接/断开
        Task ConnectAsync(string portName, int baudRate, int dataBits, System.IO.Ports.StopBits stopBits, System.IO.Ports.Parity parity);
        Task DisconnectAsync();

        // 数据采集
        Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count);
        Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value);

        // 事件：用于通知通信日志
        event EventHandler<CommunicationLog> OnLogReceived;
    }
}

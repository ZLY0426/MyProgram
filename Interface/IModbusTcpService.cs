using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Interface
{
    public interface IModbusTcpService
    {
        bool IsConnected { get; }
        Task ConnectAsync(string ipAddress, int port);
        Task DisconnectAsync();
        Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count);
        Task WriteRegistersAsync(byte slaveId, ushort startAddress, ushort[] values);
    }
}

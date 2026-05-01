using System;

namespace MyProgram.Models
{
    /// <summary>
    /// 通信日志模型
    /// </summary>
    public class CommunicationLog
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } // "发送" / "接收" / "错误"
        public string Message { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProgram.Models
{
    public class LogEntry
    {
        // 将 UserId 设为主键
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // 关键：告诉 EF Core 我们要手动生成 ID，不由数据库自增
        public long UserId { get; set; } // 使用 long 防止溢出

        public string Username { get; set; }

        public string Action { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
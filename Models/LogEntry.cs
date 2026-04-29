using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProgram.Models
{
    public class LogEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LogId { get; set; }

        // 外键属性
        [ForeignKey("User")]
        public int UserId { get; set; }
       
        public string Username { get; set; }

        // 导航属性
        public User User { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
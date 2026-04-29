using System;
using System.ComponentModel.DataAnnotations;

namespace MyProgram.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        // 注意：只存哈希值，BCrypt 会自动把 Salt 嵌入哈希中，无需单独存 Salt
        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

using MyProgram.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Services
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string rawPassword)
        {
            // 工作因子设为 12，越高越安全但验证越慢（平衡安全与性能）
            return BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);
        }

        public bool VerifyPassword(string rawPassword, string hashedPassword)
        {
            // BCrypt 会自动从哈希中提取 Salt 进行验证
            return BCrypt.Net.BCrypt.Verify(rawPassword, hashedPassword);
        }
    }
}

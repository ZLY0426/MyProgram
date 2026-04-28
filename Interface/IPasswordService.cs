using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Interface
{
    public interface IPasswordService
    {
        string HashPassword(string rawPassword);
        bool VerifyPassword(string rawPassword, string hashedPassword);
    }
}

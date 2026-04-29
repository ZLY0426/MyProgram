using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Interface
{
    public interface ICurrentUserService
    {
        int UserId { get; set; }
        string Username { get; set; }
        bool IsLoggedIn { get; }
        void Clear();
    }
}

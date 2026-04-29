using MyProgram.Interface;

namespace MyProgram.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Username);

        public void Clear()
        {
            UserId = 0;
            Username = null;
        }
    }
}
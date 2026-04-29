using MyProgram.Dtos;
using MyProgram.Models;
using System.Threading.Tasks;

namespace MyProgram.Interface
{
    public interface ILogService
    {
        Task LogAsync(int userId, string username, string action);
        Task<PagedResult<LogEntry>> GetPagedLogsAsync(int pageIndex, int pageSize);

        // 新增：搜索接口
        Task<PagedResult<LogEntry>> SearchLogsAsync(string searchType, string searchValue, int pageIndex, int pageSize);
    }
}

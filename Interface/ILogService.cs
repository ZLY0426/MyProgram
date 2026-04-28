using MyProgram.Dtos;
using MyProgram.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyProgram.Interface
{
    public interface ILogService
    {
        Task LogAsync(int userId, string username, string action); // 异步记录

        // 异步分页查询，返回 PagedResult
        Task<PagedResult<LogEntry>> GetPagedLogsAsync(int pageIndex, int pageSize);
    }
}


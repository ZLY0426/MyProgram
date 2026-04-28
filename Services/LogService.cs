using Microsoft.EntityFrameworkCore;
using MyProgram.Data;
using MyProgram.Dtos;
using MyProgram.Interface;
using MyProgram.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyProgram.Services
{
    public class LogService : ILogService
    {
        public async Task LogAsync(int userId, string username, string action)
        {
            using (var context = new AppDbContext())
            {
                long nextId;
                // 注意：使用 FirstOrDefaultAsync
                var maxId = await context.Logs
                    .OrderByDescending(l => l.UserId)
                    .FirstOrDefaultAsync();

                nextId = maxId == null ? AppDbContext.StartingUserId : maxId.UserId + 1;

                var newLog = new LogEntry
                {
                    UserId = nextId,
                    Username = username,
                    Action = action,
                    Timestamp = DateTime.Now
                };

                context.Logs.Add(newLog);
                // 注意：使用 SaveChangesAsync
                await context.SaveChangesAsync();
            }
        }

        public async Task<PagedResult<LogEntry>> GetPagedLogsAsync(int pageIndex, int pageSize)
        {
            using (var context = new AppDbContext())
            {
                // 1. 异步获取总数
                var totalCount = await context.Logs.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // 2. 异步获取分页数据
                var items = await context.Logs
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 3. 返回包装好的结果
                return new PagedResult<LogEntry>
                {
                    Items = items,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
        }
    }
}

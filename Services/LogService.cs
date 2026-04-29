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
        // 定义起始 ID (2026050000 < 2^31-1，符合 int 范围)
        private const int StartingLogId = 2026050000;

        public async Task LogAsync(int userId, string username, string action)
        {
            using (var context = new AppDbContext())
            {
                // 1. 获取当前最大 LogId
                int nextId;
                // 注意：先按 LogId 倒序，再取第一条
                var maxLog = await context.Logs
                    .OrderByDescending(l => l.LogId)
                    .FirstOrDefaultAsync();

                if (maxLog == null)
                {
                    nextId = StartingLogId;
                }
                else
                {
                    nextId = maxLog.LogId + 1;
                }

                // 2. 创建新日志
                var newLog = new LogEntry
                {
                    LogId = nextId,
                    UserId = userId,
                    Username = username,
                    Action = action,
                    Timestamp = DateTime.Now
                };

                context.Logs.Add(newLog);
                await context.SaveChangesAsync();
            }
        }

        public async Task<PagedResult<LogEntry>> GetPagedLogsAsync(int pageIndex, int pageSize)
        {
            using (var context = new AppDbContext())
            {
                var totalCount = await context.Logs.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var items = await context.Logs
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<LogEntry>
                {
                    Items = items,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
        }
        public async Task<PagedResult<LogEntry>> SearchLogsAsync(string searchType, string searchValue, int pageIndex, int pageSize)
        {
            using (var context = new AppDbContext())
            {
                IQueryable<LogEntry> query = context.Logs;

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    switch (searchType)
                    {
                        case "时间":
                            if (DateTime.TryParse(searchValue, out DateTime searchDate))
                            {
                                query = query.Where(l => l.Timestamp.Date == searchDate.Date);
                            }
                            break;

                        case "用户ID":
                            if (int.TryParse(searchValue, out int userId))
                            {
                                query = query.Where(l => l.UserId == userId);
                            }
                            break;

                        case "用户名":
                            query = query.Where(l => l.Username.Contains(searchValue));
                            break;
                    }
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var items = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

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

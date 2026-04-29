using MyProgram.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;


namespace MyProgram.Data
{
    public class AppDbContext : DbContext
    {
        // 构造函数
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext()
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // 数据库文件将创建在用户的本地应用数据文件夹中
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Join(path, "MyProgram.db");

            options.UseSqlite($"Data Source={dbPath}");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. 给 Timestamp 加索引：排序和按时间搜索会非常快
            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => l.Timestamp);

            // 2. 给 UserId 加索引：按用户ID搜索会非常快
            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => l.UserId);

            // 3. 给 Username 加索引：注意：这只能加速 "LIKE 'abc%'"，不能加速 "LIKE '%abc%'"
            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => l.Username);
        }
    }
}
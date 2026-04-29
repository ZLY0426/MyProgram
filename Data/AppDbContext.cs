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
            // 配置 UserId 为主键（虽然上面已经有 [Key]，这里显式配置更保险）
            modelBuilder.Entity<LogEntry>()
                .HasKey(l => l.LogId);
        }
    }
}
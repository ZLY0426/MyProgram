# 工业物联网监控系统 (Industrial IoT Monitoring System)

基于 **WPF + Prism MVVM** 的工业物联网上位机监控系统，支持 Modbus RTU/TCP 通信、多从站管理、实时仪表盘可视化和用户行为审计。

## 功能特性

- **用户认证** — 注册 / 登录，BCrypt 密码哈希存储
- **Modbus RTU** — 串口连接、寄存器读写、定时轮询采集
- **Modbus TCP** — 多从站扫描管理、在线状态检测、轮询采集
- **仪表盘可视化** — LiveCharts 温度/湿度/压力/流量实时仪表盘
- **运行日志** — 用户行为记录、分页查询、多条件搜索
- **每日背景** — 必应每日一图 API，本地自动缓存
- **自定义控件** — CarouselView 轮播、AnimatedContentControl 页面切换动画、FillDataGrid 自适应表格

## 技术栈

| 技术 | 说明 |
|------|------|
| .NET 10.0 + WPF | 桌面客户端框架 |
| Prism.Unity 9.0 | MVVM、依赖注入、区域导航 |
| Entity Framework Core + SQLite | ORM 数据持久化 |
| BCrypt.Net-Core | 密码哈希与验证 |
| NModbus.Serial | Modbus RTU / TCP 通信协议 |
| LiveCharts.Wpf | 工业仪表盘可视化 |
| Newtonsoft.Json | 必应 API JSON 解析 |

## 架构概览

```
Views (XAML)
    ↕  DataBinding
ViewModels (Prism BindableBase)
    ↕  DI Injection
Services (业务逻辑)
    ↕
Data Layer (EF Core + SQLite)
```

- **区域导航**: `ContentRegion`（登录/注册/主面板） → `DashboardRegion`（RTU / TCP / 日志）
- **页面切换**: AnimatedContentControl 提供滑入滑出动画
- **通信安全**: ModbusTcpService 使用 SemaphoreSlim 保证线程安全

## 项目结构

```
MyProgram/
├── Convertors/          # 值转换器（Bool→颜色、日志类型→颜色）
├── CustomControl/       # 自定义控件（CarouselView、AnimatedContentControl、FillDataGrid）
├── Data/                # EF Core DbContext
├── Dtos/                # 分页结果 DTO
├── Helpers/             # PasswordBox 绑定、TextBox 占位符
├── Interface/           # 服务接口定义
├── Models/              # 数据实体（User、LogEntry、ModbusRegister、CommunicationLog）
├── Services/            # 服务实现（认证、日志、Modbus RTU/TCP、每日一图）
├── Styles/              # XAML 样式资源字典
├── ViewModels/          # 视图模型（Login、Register、Dashboard、Modbus、Log）
└── Views/               # 视图页面（XAML + Code-Behind）
```

## 快速开始

### 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 / 11

### 运行

```bash
git clone https://github.com/your-username/MyProgram.git
cd MyProgram
dotnet run
```

首次运行会自动在 `%LocalAppData%` 下创建 SQLite 数据库 `MyProgram.db`。

### Modbus 测试

- **RTU**: 使用 [Virtual Serial Port Emulator](https://www.eterlogic.com/Products.VSPE.html) 创建虚拟串口对，配合 Modbus Slave 模拟器
- **TCP**: 使用 [Modbus Slave 模拟器](https://www.modbustools.com/modbus_slave.html) 启动 TCP Server（默认端口 502）

## 数据库

| 表 | 说明 |
|----|------|
| Users | 用户表（UserId, Username, PasswordHash, CreatedAt） |
| Logs | 日志表（LogId, UserId, Username, Action, Timestamp） |

Logs 表在 `Timestamp`、`UserId`、`Username` 上建有索引以优化查询性能。

## License

MIT

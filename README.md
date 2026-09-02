# Minecraft Player Management（mpm 玩家档案管理）

一个用于管理 **Minecraft Java 版** 存档与玩家数据的桌面工具，同时支持**单人客户端（`.minecraft`）**与**专用服务器（服务端）**两种根目录形态。图形界面（WPF）与后台引擎（C++ `mpm.exe`）通过 **Windows 共享内存** 通信，可方便地检视每个玩家在存档中的数据足迹，并安全地（移入回收站）清理玩家数据。

## 功能特性

### 目录识别与加载
- 选择 `.minecraft` 客户端根目录（含 `saves` 文件夹）或服务端根目录，**自动识别加载模式**：
  - 客户端模式：玩家列表读取根目录的 `usercache.json`，存档位于 `saves/` 下；
  - 服务端模式：存档直接位于根目录下（如无缓存文件则玩家列表为空）。
- 概览页实时显示引擎连接状态、当前根目录下的**存档数量**与**玩家数量**。
- 支持一键刷新数据、重启引擎、在资源管理器中打开当前目录。

### 存档管理
- 列出当前目录下全部存档（世界），支持刷新、打开目录。
- 进入某存档后，以“玩家 × 数据类别”的矩阵展示数据是否存在，类别包括：

  | 数据 | 文件位置 |
  | --- | --- |
  | 进度（Advancements） | `<存档>/advancements/<uuid>.json` |
  | 玩家数据 | `<存档>/playerdata/<uuid>.dat` |
  | 旧版玩家数据 | `<存档>/playerdata/<uuid>_old.dat` |
  | 装饰盔甲数据（cosarmor） | `<存档>/playerdata/<uuid>.cosa` |
  | 统计（Stats） | `<存档>/stats/<uuid>.json` |

- 删除**单个玩家**在该存档中的数据，或一键**清除本存档全部玩家数据**。

### 玩家管理
- 从 `usercache.json` / `usernamecache.json` 汇总玩家列表（昵称、UUID、令牌过期时间）。
- 按**名称或 UUID** 实时搜索。
- 查看单个玩家在**所有存档**中的足迹（在哪些存档、哪些数据类别存在文件）。
- 单独移除某玩家的缓存记录，或从**所有存档与缓存**中彻底删除该玩家。

### 数据安全
- 所有删除操作均通过 PowerShell 将文件**移入回收站**，不会直接物理删除，可随时恢复。

### 引擎与诊断
- 引擎（`mpm.exe`）路径可配置，支持启动 / 停止 / 重启。
- 内置运行日志（最近 400 条）与控制台输出，便于排查。
- 内置 `--smoke` 端到端自检：自动构造模拟客户端/服务端目录，验证握手、路径识别、列表与详情命令全链路。

## 架构

项目由三个部分组成，形成“引擎 + 图形界面 + 单文件启动器”的层次：

```
┌────────────────────────────────────────────────────────┐
│ MinecraftPlayerManagement.exe（All-in-One 启动器，Win32）│
│   自解压：内嵌 mpm.exe 与 mpm_GUI.exe（RCDATA 资源）      │
│   默认解压到 %LOCALAPPDATA%\MinecraftPlayerManagement     │
│   启动 GUI 并等待退出后清理；支持 -i 就地解压             │
└───────────────────────┬────────────────────────────────┘
                        │ 拉起进程
┌───────────────────────▼────────────────────────────────┐
│ mpm_GUI.exe（WPF 客户端，.NET 10 + MVVM）                │
│   · 创建共享内存与内核同步对象（Mutex/Event）             │
│   · 以 `mpm bg` 拉起 C++ 引擎并握手                       │
│   · 串行命令队列，结果反序列化到 UI                       │
│   · 设置存于 %AppData%\mpm_GUI\settings.json             │
└───────────────────────┬────────────────────────────────┘
                        │ 共享内存 SharedMemoryCommand
┌───────────────────────▼────────────────────────────────┐
│ mpm.exe（C++20 引擎，控制台）                             │
│   · 命令行模式：mpm [路径]  → 交互式操作                  │
│   · 后台模式：  mpm bg      → 共享内存命令循环            │
│   · 路径识别 / 列表加载 / 删除（回收站）等文件操作          │
└─────────────────────────────────────────────────────────┘
```

### 进程间通信（IPC）

- 客户端与引擎通过命名共享内存 `SharedMemoryCommand`（约 40 KB 固定布局，对齐两端的结构体偏移）传递命令与数据。
- 使用命名的 Mutex、发送 / 接收 Event 与初始化事件完成同步与握手；GUI 侧单工作线程串行收发命令，避免并发写。
- 传输的命令与枚举在 C++ `Enums.h` / `Struct.h` 与 C# `MpmProtocol.cs` / `MpmCodec.cs` 中一一对齐：
  - 命令：`M_SET_PATH`、`OPEN_WORLD`、`OPEN_PLAYER`、`LIST_WORLD`、`LIST_PLAYER`、`DEL_PLAYER`、`DEL_WORLD`、`DEL_PW`（从存档删除玩家）、`DEL_JS`（清缓存）、`REFRESH`、`EXIT`、`BREAK` 等。
  - 结构：`WDNL`（存档列表）、`WDN`、`UI`（玩家）、`PI_AS`（进度/统计）、`PI_D`（玩家数据）、`PIWI` / `PIWIL`（玩家在某存档的数据足迹矩阵）。
- 字符串编码策略：文件系统路径 / 目录名使用系统 ANSI 代码页；源自 JSON 的玩家名等使用严格 UTF-8（失败时回退 ANSI）编解码。

### 两种运行模式

| 模式 | 入口 | 说明 |
| --- | --- | --- |
| 命令行模式 | `mpm <路径>` 或无参数进入后输入路径 | 自动识别模式并列出存档与玩家，然后进入交互式提示符，支持 `open world/player`、`list world/player`、`delete player/world/pw/js`、`refresh`、`exit`、`break` 等命令 |
| 后台模式 | `mpm bg` | 进入共享内存命令循环，供 GUI 调用；无控制台界面 |

## 项目结构

```
MinecraftPlayerManagement/
├─ MinecraftPlayerManagement.slnx      # 解决方案文件
├─ mc_icon.ico                         # 应用图标
├─ MinecraftPlayerManagement/          # All-in-One 单文件启动器（Win32 C++）
│  ├─ main.cpp                         # 单实例互斥；解压 RCDATA → 启动 GUI → 退出清理；-i 就地解压
│  ├─ AppIcon.rc                       # 程序图标资源
│  ├─ EmbeddedResources.rc             # 生成目标自动生成：内嵌 mpm.exe / mpm_GUI.exe 等
│  └─ MinecraftPlayerManagement.vcxproj# 先发布 GUI 再生成并链接内嵌资源
├─ mpm/                                # 引擎（C++20，内部名 mpm.exe）
│  ├─ main.cpp                         # 入口：带路径进入命令行模式；`bg` 进入后台模式
│  ├─ CC.h                             # 各命令处理器头文件聚合
│  ├─ piwbd.*                          # 命令处理器公共基类（继承 p_mpm）
│  ├─ COW / COP / CLW / CLP / CDP / CDW / CDPW / CDJS .h/.cpp
│  │                                   # OPEN_WORLD/OPEN_PLAYER/LIST_*/DEL_* 各命令执行器
│  ├─ p_mpm.*                          # 核心执行类：路径预处理、模式识别、列表加载、命令分发
│  ├─ func.*                           # 目录扫描、JSON 读写、回收站删除等底层函数
│  ├─ SharedMemory.*                   # 后台模式的共享内存初始化/握手/命令循环
│  ├─ Struct.h                         # 共享内存命令与数据序列化结构体定义
│  ├─ Enums.h                          # 命令 / 加载模式 / 执行状态等枚举
│  ├─ BackgroundRunning.* / CommandRunning.*
│  ├─ Logout/                          # 轻量日志库
│  ├─ nlohmann-json/                   # 内置单头 JSON 依赖
│  └─ mpm.rc                           # 版本资源（产品名/作者信息）
├─ mpm_GUI/                            # WPF 客户端（.NET 10，MVVM）
│  ├─ App.xaml(.cs)                    # 启动入口；`--smoke`、`--ui` 自检参数
│  ├─ MainWindow.xaml(.cs)             # 主窗口：概览 / 存档 / 玩家 / 设置 页签
│  ├─ Models/Dtos.cs                   # 存档/玩家/足迹等记录定义
│  ├─ Services/                        # MpmEngineService、MpmProtocol、MpmCodec、
│  │                                   # SettingsStore、SmokeRunner、UiServices、NativeMethods
│  ├─ ViewModels/                      # Shell / Overview / Worlds / Players / Settings
│  ├─ Views/                           # 各页视图与确认对话框
│  ├─ Themes/ · Converters/            # 样式资源与值转换器
│  └─ Properties/PublishProfiles/      # FolderProfile.pubxml（单文件发布）
└─ x64/                                # 构建产物目录（已在 .gitignore 中忽略）
```

## 构建

### 环境要求

- Windows 10/11（x64）
- Visual Studio（含 C++ 桌面开发工作负载），工程使用 **v145 平台工具集**、Windows 10 SDK、C++20
- .NET 10 SDK（`net10.0-windows`）

### 构建步骤

```powershell
# 1) 常规构建（引擎 + GUI）
#    Visual Studio 打开 MinecraftPlayerManagement.slnx，生成 x64/Release；
#    构建 mpm_GUI.csproj 后，mpm.exe 会被自动复制到 GUI 输出目录。

# 2) 单独发布 GUI（单文件、自包含）
dotnet publish mpm_GUI/mpm_GUI.csproj -p:PublishProfile=FolderProfile -c Release

# 3) All-in-One 单文件启动器
#    构建 MinecraftPlayerManagement.vcxproj（Release x64）：
#    生成目标会自动发布 GUI 单文件 → 将 mpm.exe 等生成 RCDATA 资源 → 链接产出单文件。
```

主要输出位置：

| 组件 | 输出 |
| --- | --- |
| 引擎 `mpm.exe` | `x64\Release\net10.0-windows\mpm.exe`（构建时复制进 GUI 输出目录） |
| GUI | `mpm_GUI\bin\Release\net10.0-windows\publish\win-x64\` 等 |
| All-in-One 启动器 | `x64\Release\MinecraftPlayerManagement.exe` |

## 使用说明

1. 启动 GUI（`mpm_GUI.exe`，或直接运行 All-in-One `MinecraftPlayerManagement.exe`）。
2. 首次运行若提示 mpm 未连接：在「设置」页指定 `mpm.exe` 路径并「启动mpm」；也可手动「浏览…」配置。
3. 在「概览」页选择 Minecraft 客户端（`.minecraft`）或服务端根目录，应用自动识别模式并加载数据。
4. 在「存档」页选择存档查看其中的玩家数据，按需删除玩家/清空存档；
   在「玩家」页搜索玩家并查看其跨存档足迹，按需清理缓存或彻底删除。

## 自检（冒烟测试）

```powershell
# 端到端自检：自动生成模拟客户端/服务端目录，
# 验证 mpm 握手、路径识别、存档/玩家列表与详情查询等全链路。
# 通过时退出码为 0，输出结果文件。
mpm_GUI.exe --smoke <工作目录> [结果文件]

# UI 冒烟：短暂打开主窗口以捕获 XAML/绑定运行期错误（约 1.5 秒后自动退出）。
mpm_GUI.exe --ui
```

## 目录

- 仅在 **Windows x64** 上测试与构建。
- 面向 **Minecraft Java 版**：单机客户端根目录（含 `saves`、`usercache.json` 等）或 Java 服务端根目录。
- 删除操作涉及目标目录内的文件与 `usercache.json` / `usernamecache.json` 缓存，请在使用前备份重要存档。

## 作者与版本

- 引擎版本资源：Product `Minecraft 用户数据管理工具`，内部名 `mpm.exe`，© 2025 Qing_Xiaoyu_stlr。
- 仓库远端：`https://gitea.devserver/QXY_stlr/MinecraftPlayerManagement.git`

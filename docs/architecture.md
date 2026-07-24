# 架构设计文档

## 项目概述

Mail-Box-Net-Disk 是一个命令行工具，利用 IMAP/SMTP 邮箱实现文件存储（网盘）功能。

## 技术栈

- **运行时**: .NET 10 (net10.0)
- **IMAP/SMTP**: MailKit (最新稳定版)
- **JSON**: System.Text.Json（内置）
- **CLI**: Spectre.Console
- **DI**: Microsoft.Extensions.DependencyInjection
- **日志**: Microsoft.Extensions.Logging

## 分层架构

```
┌─────────────────────────────────┐
│          Program.cs             │ 入口、DI 配置、命令分发
├─────────────────────────────────┤
│         Commands/               │ 每个 CLI 命令一个文件
│  Setup / List / Upload / ...    │ 解析参数 → 调用 Service
├─────────────────────────────────┤
│         Services/               │ 业务逻辑层
│  DiskService / ImapService      │ 核心操作、IMAP 封装
├─────────────────────────────────┤
│         MailKit                 │ 第三方：IMAP/SMTP 协议
└─────────────────────────────────┘
```

### 依赖方向
```
Commands → Services → MailKit
Commands → Services → Models
Program → Commands / Services (通过 DI)
SettingsManager → Models (读写配置)
```

## 项目结构

```
src/MailDisk/
├── Program.cs                    # Main: 构建 DI 容器，执行命令
├── Locale.cs                     # 国际化（根据系统语言中/英文切换）
├── MailDisk.csproj               # .NET 10 项目文件
├── Configuration/
│   └── SettingsManager.cs        # mail.json 配置的读写
├── Models/
│   └── DiskFileEntry.cs          # 文件条目模型（名称、大小、是否分卷）
├── Services/
│   ├── ImapService.cs            # IMAP 连接管理 & 邮件操作
│   ├── DiskService.cs            # 核心网盘逻辑（上传/下载/列表/清理）
│   └── ChecksumService.cs        # SHA256 计算与校验
└── Commands/
    ├── SetupCommand.cs           # -s 配置向导
    ├── ListFoldersCommand.cs     # -lf 列出邮件文件夹
    ├── CreateFolderCommand.cs    # -cf 创建文件夹
    ├── ListFilesCommand.cs       # -l 列出文件
    ├── CleanCommand.cs           # -c 清理不完整分卷
    ├── UploadCommand.cs          # -u 上传文件
    ├── DownloadCommand.cs        # -d 下载文件
    ├── UploadFolderCommand.cs    # -uf 上传文件夹
    └── DownloadFolderCommand.cs  # -df 下载文件夹
```

## 关键设计决策

### 1. 为什么用 Spectre.Console 而不是 System.CommandLine
- Spectre.Console 已经是项目依赖
- 提供丰富的终端渲染（表格、进度条、颜色）
- 命令路由更简洁

### 2. 为什么不用接口（IDiskService 等）
- 项目规模小（<2000 行），接口抽象收益低
- 不需要 mock 测试（无单元测试需求）
- 保持代码简洁，不过度工程化

### 3. 异步设计
- MailKit 提供完整 async API
- 所有 IMAP 操作使用 async/await
- Program.Main 为 `async Task Main`

### 4. 进度反馈
- `ImapService.FetchSummariesAsync` 接收 `Action<int, int>? onProgress` 回调
- `DiskService` 的列表、上传文件夹、下载文件夹方法透传进度回调
- 命令层使用 `AnsiConsole.Progress()` 渲染进度条
- 慢操作（获取大量邮件、上传/下载文件夹）显示百分比进度，操作名称随进度更新

### 5. 国际化 (i18n)
- `Locale.cs` 通过 `CultureInfo.CurrentUICulture` 检测系统语言
- `zh-*` 系列显示中文，其他默认英文
- 所有用户可见字符串集中在 `Locale` 类中，通过 `Locale.XXX` 属性访问
- 异常和技术日志保持英文

### 6. Spectre.Console 转义
- 所有用户提供的字符串（文件名、路径、文件夹名）在嵌入 Spectre.Console 输出前必须用 `Markup.Escape()` 转义
- 否则 `[` `]` 等字符会被误解析为 markup 标记，导致运行时崩溃

### 4. 配置向后兼容
- `mail.json` 文件位置不变（可执行文件同目录）
- 字段名不变：`imap`, `port`, `ssl`, `account`, `password`, `address`, `block`
- 新增字段使用默认值，读取旧配置不会报错

### 5. SSL 证书
- 默认验证 SSL 证书（安全优先）
- 通过配置选项允许禁用（`mail.json` 中的 `skipSslVerify` 字段）
- TODO: 未来可改为可配置

## 数据流

### 上传流程
```
Command → DiskService.UploadFile()
  ├→ File.OpenRead()
  ├→ ChecksumService.ComputeSha256()
  ├→ 文件 ≤ blockSize → ImapService.AppendMail() ×1
  └→ 文件 >  blockSize → ImapService.AppendMail() ×N (分卷)
      每封邮件带 X-MailDisk-* 自定义头
```

### 下载流程
```
Command → DiskService.DownloadFile()
  ├→ ImapService.SearchMails("[mailDisk]filename*")
  ├→ 解析主题，识别单文件/分卷
  ├→ ImapService.GetMessage() ×1 或 ×N
  ├→ 拼接分卷 → File.Write
  └→ ChecksumService.Verify(可选)
```

### 文件列表流程
```
Command → DiskService.ListFiles()
  ├→ ImapService.FetchSummaries(folder)
  ├→ 过滤 Subject 含 [mailDisk] 的邮件
  ├→ 识别分卷文件 → 检查完整性
  └→ 返回 DiskFileEntry[] (去重，合并分卷)
```

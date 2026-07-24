# Mail-Box-Net-Disk — AI Coding Agent Instructions

项目概述见 [README.md](README.md)，设计文档见 [docs/](docs/)。

## 构建与运行

```bash
dotnet build maildisk/maildisk.sln
dotnet run --project maildisk/src/MailDisk -h
```

- 目标框架：`net10.0`
- 依赖：`MailKit 4.*`、`Spectre.Console 0.*`
- `Microsoft.Extensions.*` 来自 .NET 共享框架，无需额外 NuGet 引用

## 架构

```
Program.cs (入口，DI 容器，命令路由)
  └─ Commands/             每个 CLI 命令一个文件
       └─ Services/         业务逻辑层
            ├─ DiskService   核心：上传/下载/列表/分卷/校验
            ├─ ImapService    IMAP 连接和邮件操作
            └─ ChecksumService SHA256 计算与校验
                 └─ Configuration/SettingsManager  mail.json 读写
```

- 分层架构，依赖方向：Commands → Services → MailKit
- `Microsoft.Extensions.DependencyInjection` 做 DI
- `Microsoft.Extensions.Logging` 做日志（仅 Warning 级别输出到控制台）
- Spectre.Console 做 CLI 渲染

## 关键约定

### 邮件主题格式
所有网盘文件通过邮件主题前缀 `[mailDisk]` 识别：
- 小文件：`[mailDisk]文件名`
- 大文件分卷：`[mailDisk]文件名<1/N>`、`[mailDisk]文件名<2/N>` …

详见 [docs/file-format.md](docs/file-format.md)。

### 附件命名
附件固定命名为 `attachment.netdiskfile`，MIME 类型 `application/octet-stream`，Base64 编码。

### 配置存储
`mail.json` 与可执行文件同目录（`AppContext.BaseDirectory`）。字段名保持旧版兼容：`imap`, `port`, `ssl`, `account`, `password`, `address`, `block`。新增可选字段 `skipSslVerify`。

### 自定义邮件头（v2）
新上传文件自动添加，旧文件无这些头则跳过校验。

| 头名 | 说明 |
|---|---|
| `X-MailDisk-File-Size` | 原始文件大小（字节） |
| `X-MailDisk-SHA256` | 原始文件 SHA256 |
| `X-MailDisk-Chunk-Index` | 分卷序号 |
| `X-MailDisk-Chunk-Total` | 总分卷数 |
| `X-MailDisk-Chunk-SHA256` | 分卷数据 SHA256 |

## 代码风格

- 中文注释和用户界面文本
- 缩进：Tab
- 类名 PascalCase，方法名 PascalCase
- 使用 file-scoped namespace (`namespace MailDisk.Services;`)
- 使用 `System.Text.Json`（非 Newtonsoft）
- 所有 I/O 使用 async/await

## 国际化

- [Locale.cs](maildisk/src/MailDisk/Locale.cs) 统一管理所有用户可见字符串
- 通过 `CultureInfo.CurrentUICulture` 自动检测中/英文
- 新增文本必须加入 `Locale` 类，不要硬编码
- 异常消息和技术日志保持英文

## 进度反馈

- 慢操作（获取邮件列表、清理、上传/下载文件夹）使用 `AnsiConsole.Progress()` 进度条
- `ImapService.FetchSummariesAsync` 和 `DiskService` 列表/文件夹方法接收 `Action<int, int>? onProgress` 回调
- 快速操作（连接、创建文件夹、单文件上传）使用 `AnsiConsole.Status()` 转圈即可

## Spectre.Console 安全规则

- **所有用户提供的字符串必须用 `Markup.Escape()` 转义后再嵌入 Spectre.Console 输出**
- 这包括：文件名、路径、文件夹名、命令参数
- 否则含 `[` `]` 或特定字符组合的文本会触发 markup 解析错误
- 示例：`$"[cyan]{Markup.Escape(fileName)}[/]"` 而非 `$"[cyan]{fileName}[/]"`

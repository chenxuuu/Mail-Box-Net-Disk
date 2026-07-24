# 邮箱网盘

[English](README.en.md)

将邮箱变成你的个人网盘。利用 IMAP/SMTP 协议，把文件存储为邮件附件，实现文件的上传、下载和管理。

## 特性

- 支持任意 IMAP/SMTP 协议的邮箱（QQ、163、Gmail 等）
- 大文件自动分卷存储，下载时自动合并
- SHA256 完整性校验
- 文件夹上传/下载（递归）
- 自动检测系统语言，显示中文或英文界面
- 跨平台（Windows / macOS / Linux）

## 快速开始

### 1. 安装 .NET 10 SDK

从 [dotnet.microsoft.com](https://dotnet.microsoft.com/download) 下载安装。

### 2. 构建

```bash
git clone https://github.com/chenxuuu/Mail-Box-Net-Disk.git
cd Mail-Box-Net-Disk/maildisk
dotnet build maildisk.sln
```

### 3. 配置

```bash
dotnet run --project src/MailDisk -- -s
```

按提示输入 IMAP 服务器信息。配置保存在可执行文件同目录的 `mail.json`。

> QQ 邮箱用户需在 [mail.qq.com](https://mail.qq.com) 设置中生成授权码，并用授权码作为密码。

### 4. 使用

```bash
dotnet run --project src/MailDisk -- -h
```

## 命令参考

| 命令 | 参数 | 说明 |
|---|---|---|
| `-h` | 无 | 查看帮助 |
| `-s` | 无 | 交互式配置邮箱参数 |
| `-lf` | 无 | 列出所有邮件文件夹 |
| `-cf` | `<文件夹名>` | 创建邮件文件夹 |
| `-l` | `<邮件文件夹>` | 列出云端文件 |
| `-c` | `<邮件文件夹>` | 清理不完整的分卷文件 |
| `-u` | `<邮件文件夹> <本地> <云端>` | 上传文件 |
| `-d` | `<邮件文件夹> <本地> <云端>` | 下载文件 |
| `-uf` | `<邮件文件夹> <本地> <云端>` | 上传文件夹 |
| `-df` | `<邮件文件夹> <本地> <云端>` | 下载文件夹 |

> 所有文件名和路径不能包含 `<` 字符。

## 配置文件

```json
{
  "imap": "imap.example.com",
  "port": 993,
  "ssl": true,
  "account": "user@example.com",
  "password": "your-password",
  "address": "user@example.com",
  "block": "25",
  "skipSslVerify": false
}
```

| 字段 | 必填 | 说明 |
|---|---|---|
| `imap` | 是 | IMAP 服务器地址 |
| `port` | 是 | IMAP 端口（通常 993） |
| `ssl` | 是 | 是否启用 SSL |
| `account` | 是 | 邮箱账号 |
| `password` | 是 | 密码或授权码 |
| `address` | 是 | 发件人邮箱地址 |
| `block` | 是 | 单个邮件最大附件大小（MiB），超过自动分卷 |
| `skipSslVerify` | 否 | 跳过 SSL 证书验证（不推荐） |

## 原理

文件以邮件附件形式存储：

- **小文件**（≤ block 大小）：一封邮件，主题 `[mailDisk]文件名`
- **大文件**（> block 大小）：多封邮件分卷，主题 `[mailDisk]文件名<1/N>` … `<N/N>`
- 每个附件固定命名为 `attachment.netdiskfile`，Base64 编码
- 新上传文件附带 `X-MailDisk-*` 自定义邮件头，记录大小和 SHA256 校验值

详见 [docs/file-format.md](docs/file-format.md)。

## 项目结构

```
maildisk/
├── maildisk.sln
├── src/MailDisk/
│   ├── Program.cs              入口 & 命令路由
│   ├── Locale.cs               中英文国际化
│   ├── Configuration/          配置管理
│   ├── Models/                 数据模型
│   ├── Services/               业务逻辑（IMAP、上传下载、校验）
│   └── Commands/               每个 CLI 命令一个文件
└── docs/                       设计文档
```

## 技术栈

- .NET 10
- MailKit（IMAP/SMTP）
- Spectre.Console（CLI 渲染）
- System.Text.Json

## 许可

代码可随意引用，请保留指向本项目的说明文字。


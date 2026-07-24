# CLI 命令参考

## 通用约定

- `<email folder>` 表示 IMAP 邮件文件夹名
- `<local file>` / `<local folder>` 是本地路径
- `<cloud name>` 是云端虚拟路径（用 `/` 分隔目录）
- 所有路径和文件名不能包含 `<`

## 命令列表

| 命令 | 参数 | 功能 |
|---|---|---|
| `-h` | 无 | 显示帮助信息 |
| `-s` | 无 | 交互式配置 IMAP/SMTP 参数 |
| `-lf` | 无 | 列出所有邮件文件夹 |
| `-cf` | `<folder>` | 创建邮件文件夹 |
| `-l` | `<folder>` | 列出云端的文件 |
| `-c` | `<folder>` | 清理不完整的分卷文件 |
| `-u` | `<folder> <local> <cloud>` | 上传单个文件 |
| `-d` | `<folder> <local> <cloud>` | 下载单个文件 |
| `-uf` | `<folder> <local> <cloud>` | 上传整个文件夹 |
| `-df` | `<folder> <local> <cloud>` | 下载整个文件夹 |

## 使用示例

```bash
# 首次配置
maildisk -s

# 查看所有邮件文件夹
maildisk -lf

# 创建文件夹
maildisk -cf mydisk

# 上传文件
maildisk -u mydisk ./readme.txt docs/readme.txt

# 上传整个目录
maildisk -uf mydisk ./project project-backup

# 列出云端文件
maildisk -l mydisk

# 下载文件
maildisk -d mydisk ./downloaded.txt docs/readme.txt

# 下载整个目录
maildisk -df mydisk ./restored project-backup

# 清理不完整分卷
maildisk -c mydisk
```

## 配置文件 mail.json

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

| 字段 | 类型 | 说明 |
|---|---|---|
| `imap` | string | IMAP 服务器地址 |
| `port` | int | IMAP 端口，默认 993 |
| `ssl` | bool | 是否使用 SSL |
| `account` | string | 登录账号 |
| `password` | string | 登录密码（明文，注意安全） |
| `address` | string | 发件邮箱地址 |
| `block` | string | 单个邮件最大附件大小（MiB） |
| `skipSslVerify` | bool | 是否跳过 SSL 证书验证（默认 false） |

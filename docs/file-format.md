# 文件存储格式规范

## 概述

Mail-Box-Net-Disk 将文件存储为 IMAP 邮件。每个文件对应一封或多封邮件（大文件分卷）。

## 邮件标识

所有网盘邮件通过主题前缀 `[mailDisk]` 识别。非此前缀的邮件会被忽略。

## 主题格式

### 小文件（单封邮件）
```
[mailDisk]<虚拟路径>
```
示例：`[mailDisk]subdir/readme.txt`

### 大文件（多封邮件分卷）
```
[mailDisk]<虚拟路径><分卷号/总分卷数>
```
示例：
- `[mailDisk]video.mp4<1/3>`
- `[mailDisk]video.mp4<2/3>`
- `[mailDisk]video.mp4<3/3>`

### 规则
- 虚拟路径使用 `/` 分隔，**不使用** `\`
- 文件名不能包含 `<`（Windows 文件名本身就不允许）
- 分卷号从 1 开始，连续递增

## 附件格式

| 属性 | 值 |
|---|---|
| 文件名 | `attachment.netdiskfile` |
| MIME 类型 | `application/octet-stream` |
| 传输编码 | Base64 |
| 数量 | 每封邮件 1 个附件 |

## 邮件正文

纯文本：
```
This mail was sent by Mail-Box-Net-Disk
please do not delete
```

## 自定义邮件头（v2 新增）

新上传的文件会添加以下自定义邮件头，旧文件不受影响。

| 头名 | 说明 | 示例 |
|---|---|---|
| `X-MailDisk-File-Size` | 原始文件总大小（字节） | `1048576` |
| `X-MailDisk-SHA256` | 原始文件 SHA256 哈希（HEX） | `a1b2c3...` |
| `X-MailDisk-Chunk-Index` | 当前分卷序号（仅分卷文件） | `2` |
| `X-MailDisk-Chunk-Total` | 总分卷数（仅分卷文件） | `5` |
| `X-MailDisk-Chunk-SHA256` | 当前分卷数据的 SHA256 | `d4e5f6...` |

### 校验逻辑

1. 下载完成后，如果邮件有 `X-MailDisk-SHA256` 头，计算下载文件的 SHA256 并比对
2. 如果邮件有 `X-MailDisk-Chunk-SHA256` 头，对每个分卷下载后立即校验
3. 旧文件无这些头，跳过校验

## 虚拟路径映射

IMAP 文件夹名作为顶层容器，文件在其中的虚拟路径解析规则：
- `folder/subdir/file.txt` → IMAP 文件夹内的一封主题为 `[mailDisk]subdir/file.txt` 的邮件
- `file.txt`（无子目录）→ 主题为 `[mailDisk]file.txt`

## 兼容性

- v1 文件（无自定义头）可以正常下载，仅跳过校验
- v2 文件向后兼容，旧版客户端忽略未知邮件头
- 分卷格式保持一致，`<N/M>` 标记不变

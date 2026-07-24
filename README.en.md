# Mail-Box-Net-Disk

[中文](README.md)

Turn your email inbox into a personal cloud drive. Store files as email attachments via IMAP/SMTP — upload, download, and manage files right from the terminal.

## Features

- Works with any IMAP/SMTP email provider (QQ, Gmail, Outlook, etc.)
- Large files auto-split into chunks; auto-merge on download
- SHA256 integrity verification
- Recursive folder upload & download
- Auto-detects system language (Chinese / English)
- Cross-platform (Windows / macOS / Linux)

## Quick Start

### 1. Install .NET 10 SDK

Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

### 2. Build

```bash
git clone https://github.com/chenxuuu/Mail-Box-Net-Disk.git
cd Mail-Box-Net-Disk/maildisk
dotnet build maildisk.sln
```

### 3. Configure

```bash
dotnet run --project src/MailDisk -- -s
```

Follow the prompts to enter your IMAP server details. Configuration is saved to `mail.json` next to the executable.

> QQ Mail users: generate an authorization code at [mail.qq.com](https://mail.qq.com) and use it as the password.

### 4. Run

```bash
dotnet run --project src/MailDisk -- -h
```

## Commands

| Command | Arguments | Description |
|---|---|---|
| `-h` | none | Show help |
| `-s` | none | Interactive IMAP setup |
| `-lf` | none | List all email folders |
| `-cf` | `<folder>` | Create an email folder |
| `-l` | `<folder>` | List files in folder |
| `-c` | `<folder>` | Clean incomplete chunks |
| `-u` | `<folder> <local> <cloud>` | Upload a file |
| `-d` | `<folder> <local> <cloud>` | Download a file |
| `-uf` | `<folder> <local> <cloud>` | Upload a folder |
| `-df` | `<folder> <local> <cloud>` | Download a folder |

> File and folder names must not contain `<`.

## Configuration

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

| Field | Required | Description |
|---|---|---|
| `imap` | Yes | IMAP server address |
| `port` | Yes | IMAP port (usually 993) |
| `ssl` | Yes | Enable SSL |
| `account` | Yes | Email account |
| `password` | Yes | Password or authorization code |
| `address` | Yes | Sender email address |
| `block` | Yes | Max attachment size per email (MiB); larger files are chunked |
| `skipSslVerify` | No | Skip SSL certificate verification (not recommended) |

## How It Works

Files are stored as email attachments:

- **Small files** (≤ block size): one email, subject `[mailDisk]filename`
- **Large files** (> block size): multiple chunked emails, subjects `[mailDisk]filename<1/N>` … `<N/N>`
- Each attachment is named `attachment.netdiskfile`, Base64-encoded
- New uploads include `X-MailDisk-*` custom headers for size and SHA256 verification

See [docs/file-format.md](docs/file-format.md) for details.

## Project Structure

```
maildisk/
├── maildisk.sln
├── src/MailDisk/
│   ├── Program.cs              Entry point & command routing
│   ├── Locale.cs               i18n (Chinese / English)
│   ├── Configuration/          Settings management
│   ├── Models/                 Data models
│   ├── Services/               Business logic (IMAP, upload/download, checksum)
│   └── Commands/               One file per CLI command
└── docs/                       Design documents
```

## Tech Stack

- .NET 10
- MailKit (IMAP/SMTP)
- Spectre.Console (CLI rendering)
- System.Text.Json

## License

Free to use and reference. Please retain attribution to this project.

using System.Globalization;

namespace MailDisk;

/// <summary>
/// 国际化字符串，根据系统语言自动切换中/英文
/// </summary>
public static class Locale
{
	private static readonly bool IsZh =
		CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

	// ─── Help ───
	public static string Help_Title => "Mail-Box-Net-Disk";
	public static string Help_Tagline => IsZh ? "让邮箱变成你的网盘！" : "Make your email become a netdisk!";
	public static string Help_Commands => IsZh ? "命令" : "Commands";
	public static string Help_ShowHelp => IsZh ? "显示帮助信息" : "Show this help";
	public static string Help_Setup => IsZh ? "配置 IMAP 设置" : "Configure IMAP settings";
	public static string Help_ListFolders => IsZh ? "列出所有邮件文件夹" : "List all email folders";
	public static string Help_CreateFolder => IsZh ? "创建新邮件文件夹" : "Create a new email folder";
	public static string Help_ListFiles => IsZh ? "列出文件夹中的文件" : "List files in folder";
	public static string Help_Clean => IsZh ? "清理不完整的分卷" : "Clean incomplete chunks";
	public static string Help_Upload => IsZh ? "上传文件" : "Upload a file";
	public static string Help_Download => IsZh ? "下载文件" : "Download a file";
	public static string Help_UploadFolder => IsZh ? "上传文件夹" : "Upload a folder";
	public static string Help_DownloadFolder => IsZh ? "下载文件夹" : "Download a folder";
	public static string Help_Note => IsZh ? "注意：文件/文件夹名不能包含 '<'" : "Note: file/folder names cannot contain '<'";

	// ─── Common ───
	public static string Common_NoCommand => IsZh ? "未指定命令。使用 -h 查看帮助。" : "No command specified. Use -h for help.";
	public static string Common_UnknownCommand => IsZh ? "未知命令" : "Unknown command";
	public static string Common_UseHelp => IsZh ? "使用 -h 查看可用命令。" : "Use -h to see available commands.";
	public static string Common_SettingsNotFound => IsZh ? "未找到配置。使用 -s 进行配置。" : "Settings not found. Use -s to configure.";
	public static string Common_MissingArg => IsZh ? "缺少参数，位置" : "Missing argument at position";
	public static string Common_UseHForHelp => IsZh ? "使用 -h 查看帮助。" : ". Use -h for help.";

	// ─── Setup ───
	public static string Setup_Notice => IsZh
		? "注意：如果使用 QQ 邮箱，请在 mail.qq.com 创建授权码"
		: "Notice: If you're using QQ Mail, please create a login code on mail.qq.com";
	public static string Setup_ImapPrompt => IsZh ? "IMAP 服务器地址：" : "IMAP server address:";
	public static string Setup_PortPrompt => IsZh ? "IMAP 端口：" : "IMAP server port:";
	public static string Setup_SslPrompt => IsZh ? "使用 SSL？" : "Use SSL?";
	public static string Setup_AccountPrompt => IsZh ? "账号：" : "Account:";
	public static string Setup_PasswordPrompt => IsZh ? "密码：" : "Password:";
	public static string Setup_AddressPrompt => IsZh ? "邮箱地址：" : "Email address:";
	public static string Setup_BlockPrompt => IsZh ? "每封邮件最大大小 (MiB)：" : "Max size per email (MiB):";
	public static string Setup_SkipSslPrompt => IsZh
		? "跳过 SSL 证书验证？（不推荐）"
		: "Skip SSL certificate verification? (not recommended)";
	public static string Setup_Done => IsZh ? "完成！配置已保存到 mail.json" : "Done! Settings saved to mail.json";

	// ─── List Folders ───
	public static string LF_Getting => IsZh ? "正在获取所有文件夹..." : "Getting all folders...";
	public static string LF_Title => IsZh ? "所有文件夹" : "All folders";
	public static string LF_Column => IsZh ? "文件夹名" : "Folder Name";

	// ─── Create Folder ───
	public static string CF_NoName => IsZh ? "请提供文件夹名" : "Please provide a folder name";
	public static string CF_Creating => IsZh ? "正在创建文件夹" : "Creating folder";
	public static string CF_Created => IsZh ? "已创建" : "created";

	// ─── List Files ───
	public static string LS_NoFolder => IsZh ? "请提供文件夹名" : "Please provide a folder name";
	public static string LS_Fetching => IsZh ? "正在获取文件列表" : "Fetching file list from";
	public static string LS_NoFiles => IsZh ? "没有找到文件。" : "No files found.";
	public static string LS_ColumnIndex => "#";
	public static string LS_ColumnName => IsZh ? "文件名" : "File Name";
	public static string LS_ColumnSize => IsZh ? "大小" : "Size";
	public static string LS_ColumnType => IsZh ? "类型" : "Type";
	public static string LS_TypeSingle => IsZh ? "单文件" : "single";
	public static string LS_TypeChunked => IsZh ? "分卷" : "parts";
	public static string LS_Total => IsZh ? "个文件" : "file(s) total";

	// ─── Clean ───
	public static string CL_NoFolder => IsZh ? "请提供文件夹名" : "Please provide a folder name";
	public static string CL_Cleaning => IsZh ? "正在清理不完整文件" : "Cleaning incomplete files in";
	public static string CL_Done => IsZh ? "清理完成" : "Cleanup completed for";

	// ─── Upload ───
	public static string UL_NoAngleBracket => IsZh ? "错误：文件名不能包含 '<'" : "Error: file name cannot contain '<'";
	public static string UL_Uploading => IsZh ? "正在上传" : "Uploading";
	public static string UL_Done => IsZh ? "上传完成" : "Upload complete";

	// ─── Download ───
	public static string DL_NoAngleBracket => IsZh ? "错误：文件名不能包含 '<'" : "Error: file name cannot contain '<'";
	public static string DL_Downloading => IsZh ? "正在下载" : "Downloading";
	public static string DL_Refreshing => IsZh ? "正在刷新文件索引" : "Refreshing file index";
	public static string DL_Done => IsZh ? "下载完成" : "Download complete";

	// ─── Upload Folder ───
	public static string UF_NoAngleBracket => IsZh ? "错误：文件夹名不能包含 '<'" : "Error: folder name cannot contain '<'";
	public static string UF_FolderNotFound => IsZh ? "本地文件夹不存在" : "Local folder not found";
	public static string UF_Uploading => IsZh ? "正在上传文件夹" : "Uploading folder";
	public static string UF_UploadingStatus => IsZh ? "正在上传..." : "Uploading...";
	public static string UF_Done => IsZh ? "上传完成！" : "Upload complete!";

	// ─── Download Folder ───
	public static string DF_NoAngleBracket => IsZh ? "错误：文件夹名不能包含 '<'" : "Error: folder name cannot contain '<'";
	public static string DF_Downloading => IsZh ? "正在下载文件夹" : "Downloading folder";
	public static string DF_DownloadingStatus => IsZh ? "正在下载..." : "Downloading...";
	public static string DF_Done => IsZh ? "下载完成！" : "Download complete!";

	// ─── Errors ───
	public static string Err_ErrorPrefix => IsZh ? "错误" : "Error";
}

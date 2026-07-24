using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailDisk.Configuration;

namespace MailDisk.Services;

/// <summary>
/// IMAP 邮件服务封装
/// </summary>
public class ImapService
{
	private readonly AppSettings _settings;
	private readonly ILogger<ImapService> _logger;

	public ImapService(AppSettings settings, ILogger<ImapService> logger)
	{
		_settings = settings;
		_logger = logger;
	}

	/// <summary>
	/// 创建已认证的 IMAP 客户端
	/// </summary>
	private async Task<ImapClient> ConnectAsync(CancellationToken ct = default)
	{
		var client = new ImapClient();

		if (_settings.SkipSslVerify)
		{
			client.ServerCertificateValidationCallback = (s, c, h, e) => true;
			_logger.LogWarning("SSL certificate verification is disabled");
		}

		_logger.LogDebug("Connecting to {Host}:{Port} (SSL={Ssl})", _settings.ImapServer, _settings.Port, _settings.UseSsl);
		await client.ConnectAsync(_settings.ImapServer, _settings.Port, _settings.UseSsl, ct);
		await client.AuthenticateAsync(_settings.Account, _settings.Password, ct);

		// 发送客户端标识
		if (client.Capabilities.HasFlag(ImapCapabilities.Id))
		{
			var impl = new ImapImplementation { Name = "Foxmail", Version = "9.156" };
			await client.IdentifyAsync(impl, ct);
		}

		return client;
	}

	/// <summary>
	/// 获取所有邮件文件夹（递归）
	/// </summary>
	public async Task<IReadOnlyList<IMailFolder>> GetFoldersAsync(CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var result = new List<IMailFolder>();
			var personal = client.GetFolder(client.PersonalNamespaces?.FirstOrDefault()?.Path ?? "");
			CollectFolders(personal, result);
			return result;
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	private void CollectFolders(IMailFolder folder, List<IMailFolder> result)
	{
		foreach (var sub in folder.GetSubfolders(false))
		{
			result.Add(sub);
			if (sub.GetSubfolders(false).Count > 0)
				CollectFolders(sub, result);
		}
	}

	/// <summary>
	/// 获取指定文件夹的邮件摘要列表
	/// </summary>
	public async Task<IReadOnlyList<IMessageSummary>> FetchSummariesAsync(
		string folderPath, Action<int, int>? onProgress = null, CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var folder = await client.GetFolderAsync(folderPath, ct);
			await folder.OpenAsync(FolderAccess.ReadWrite, ct);

			var total = folder.Count;
			_logger.LogDebug("Fetching {Count} messages from {Folder}", total, folderPath);

			var summaries = new List<IMessageSummary>();
			for (var i = 0; i < total; i += 100)
			{
				var max = Math.Min(i + 99, total - 1);
				if (max < i) break;
				var batch = await folder.FetchAsync(i, max,
					MessageSummaryItems.Full | MessageSummaryItems.UniqueId, ct);
				summaries.AddRange(batch);
				onProgress?.Invoke(Math.Min(i + 100, total), total);
			}

			return summaries;
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	/// <summary>
	/// 按 UniqueId 获取邮件消息
	/// </summary>
	public async Task<MimeMessage> GetMessageAsync(
		string folderPath, UniqueId uid, CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var folder = await client.GetFolderAsync(folderPath, ct);
			await folder.OpenAsync(FolderAccess.ReadOnly, ct);
			return await folder.GetMessageAsync(uid, ct);
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	/// <summary>
	/// 追加邮件到文件夹
	/// </summary>
	public async Task<UniqueId?> AppendAsync(
		string folderPath, MimeMessage message, CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var folder = await client.GetFolderAsync(folderPath, ct);
			await folder.OpenAsync(FolderAccess.ReadWrite, ct);
			return await folder.AppendAsync(message, MessageFlags.Seen, ct);
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	/// <summary>
	/// 创建邮件文件夹
	/// </summary>
	public async Task CreateFolderAsync(string path, CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var idx = path.LastIndexOf('/');
			if (idx < 0)
			{
				var root = client.GetFolder(client.PersonalNamespaces?.FirstOrDefault()?.Path ?? "");
				await root.CreateAsync(path, true, ct);
			}
			else
			{
				var parent = await client.GetFolderAsync(path[..idx], ct);
				await parent.CreateAsync(path[(idx + 1)..], true, ct);
			}
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	/// <summary>
	/// 删除指定 UniqueId 的邮件
	/// </summary>
	public async Task DeleteMessagesAsync(
		string folderPath, IList<UniqueId> uids, CancellationToken ct = default)
	{
		if (uids.Count == 0) return;
		var client = await ConnectAsync(ct);
		try
		{
			var folder = await client.GetFolderAsync(folderPath, ct);
			await folder.OpenAsync(FolderAccess.ReadWrite, ct);
			await folder.AddFlagsAsync(uids, MessageFlags.Deleted, true, ct);
			await folder.ExpungeAsync(ct);
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}

	/// <summary>
	/// 将文件夹中未读邮件全部标记为已读
	/// </summary>
	public async Task MarkAllAsSeenAsync(string folderPath, CancellationToken ct = default)
	{
		var client = await ConnectAsync(ct);
		try
		{
			var folder = await client.GetFolderAsync(folderPath, ct);
			await folder.OpenAsync(FolderAccess.ReadWrite, ct);
			var uids = await folder.SearchAsync(SearchQuery.NotSeen, ct);
			if (uids.Count > 0)
			{
				await folder.AddFlagsAsync(uids, MessageFlags.Seen, true, ct);
				_logger.LogDebug("Marked {Count} messages as seen in {Folder}", uids.Count, folderPath);
			}
		}
		finally
		{
			await client.DisconnectAsync(true, ct);
		}
	}
}

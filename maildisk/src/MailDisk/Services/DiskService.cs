using System.Text.RegularExpressions;
using MailKit;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailDisk.Configuration;
using MailDisk.Models;

namespace MailDisk.Services;

/// <summary>
/// 核心网盘服务：上传、下载、文件列表、清理
/// </summary>
public class DiskService
{
	public const string SubjectPrefix = "[mailDisk]";
	public const string AttachmentName = "attachment.netdiskfile";
	public const string MailBodyText = "This mail was sent by Mail-Box-Net-Disk\r\nplease do not delete";

	// 匹配分卷主题 [mailDisk]filename<1/5>
	private static readonly Regex ChunkFirstRegex = new(
		@"^(.+?)<1/(\d+?)>$", RegexOptions.Compiled);

	// 匹配任意分卷 [mailDisk]filename<3/5>
	private static readonly Regex ChunkAnyRegex = new(
		@"^(.+?)<(\d+?)/(\d+?)>$", RegexOptions.Compiled);

	private readonly AppSettings _settings;
	private readonly ImapService _imap;
	private readonly ChecksumService _checksum;
	private readonly ILogger<DiskService> _logger;

	public DiskService(AppSettings settings, ImapService imap, ChecksumService checksum, ILogger<DiskService> logger)
	{
		_settings = settings;
		_imap = imap;
		_checksum = checksum;
		_logger = logger;
	}

	// ==================== 主题解析 ====================

	/// <summary>
	/// 解析邮件主题，提取文件信息。
	/// 返回 (文件名, 分卷序号, 总分卷数, 是否分卷)
	/// </summary>
	public static (string Name, int ChunkIndex, int ChunkTotal, bool IsChunked) ParseSubject(string subject)
	{
		if (!subject.StartsWith(SubjectPrefix))
			return ("", 0, 0, false);

		var name = subject[SubjectPrefix.Length..];

		if (!name.Contains('<'))
			return (name, 1, 1, false);

		var match = ChunkAnyRegex.Match(name);
		if (match.Success && match.Groups.Count == 4)
		{
			return (
				match.Groups[1].Value,
				int.Parse(match.Groups[2].Value),
				int.Parse(match.Groups[3].Value),
				true
			);
		}

		return (name, 1, 1, false);
	}

	// ==================== 文件列表 ====================

	/// <summary>
	/// 列出云端文件夹中的所有文件
	/// </summary>
	public async Task<IReadOnlyList<DiskFileEntry>> ListFilesAsync(
		string folderPath, bool deleteMissing = false,
		Action<int, int>? onProgress = null, CancellationToken ct = default)
	{
		var summaries = await _imap.FetchSummariesAsync(folderPath, onProgress, ct);

		// 收集所有网盘邮件的主题名
		var subjects = new List<string>();
		foreach (var m in summaries)
		{
			var subject = m.Envelope?.Subject ?? "";
			if (subject.StartsWith(SubjectPrefix))
				subjects.Add(subject[SubjectPrefix.Length..]);
		}

		// 解析文件条目
		var files = new List<DiskFileEntry>();
		var seenNames = new HashSet<string>();

		foreach (var s in subjects)
		{
			if (!s.Contains('<'))
			{
				files.Add(new DiskFileEntry { Name = s });
				continue;
			}

			var match = ChunkFirstRegex.Match(s);
			if (match.Success && match.Groups.Count == 3)
			{
				var name = match.Groups[1].Value;
				if (seenNames.Contains(name)) continue;
				seenNames.Add(name);

				var total = int.Parse(match.Groups[2].Value);
				var complete = true;
				for (var i = 1; i <= total; i++)
				{
					if (!subjects.Contains($"{name}<{i}/{total}>"))
					{
						complete = false;
						break;
					}
				}

				if (complete)
				{
					files.Add(new DiskFileEntry
					{
						Name = name,
						IsChunked = true,
						ChunkCount = total
					});
				}
			}
		}

		// 清理不完整的分卷
		if (deleteMissing)
		{
			var fileNames = new HashSet<string>(files.Select(f => f.Name));
			var toDelete = new List<UniqueId>();

			foreach (var m in summaries)
			{
				var subject = m.Envelope?.Subject ?? "";
				if (!subject.StartsWith(SubjectPrefix)) continue;

				var (name, _, _, isChunked) = ParseSubject(subject);
				if (name == "") continue;

				if (!fileNames.Contains(name))
				{
					toDelete.Add(m.UniqueId);
					_logger.LogInformation("Marking incomplete file for deletion: {Subject}", subject);
				}
			}

			if (toDelete.Count > 0)
				await _imap.DeleteMessagesAsync(folderPath, toDelete, ct);
		}

		return files;
	}

	// ==================== 上传 ====================

	/// <summary>
	/// 上传单个文件（自动处理分卷）
	/// </summary>
	public async Task UploadFileAsync(
		string cloudName, string folderPath, string localPath,
		CancellationToken ct = default, Action<int, int>? onProgress = null)
	{
		if (!File.Exists(localPath))
			throw new FileNotFoundException($"File not found: {localPath}");

		cloudName = NormalizeCloudPath(cloudName);

		var fileInfo = new FileInfo(localPath);
		var fileSize = fileInfo.Length;
		var blockSize = _settings.BlockSizeBytes;

		if (fileSize <= blockSize)
		{
			// 小文件，单封邮件
			await using var stream = File.OpenRead(localPath);
			var sha256 = await _checksum.ComputeSha256Async(stream, ct);
			stream.Position = 0;

			var message = CreateMailMessage(cloudName, stream, fileSize, sha256, 1, 1);
			await _imap.AppendAsync(folderPath, message, ct);

			_logger.LogInformation("Uploaded: {Name} ({Size} bytes, SHA256={Hash})",
				cloudName, fileSize, sha256[..8]);
		}
		else
		{
			// 大文件，分卷上传
			var steps = (int)Math.Ceiling((double)fileSize / blockSize);

			// 先计算整个文件的 SHA256
			await using var fullStream = File.OpenRead(localPath);
			var fileSha256 = await _checksum.ComputeSha256Async(fullStream, ct);

			await using var fs = File.OpenRead(localPath);
			var buffer = new byte[blockSize];
			int bytesRead;

			for (var i = 1; i <= steps; i++)
			{
				bytesRead = await fs.ReadAsync(buffer, ct);
				var chunkData = buffer.AsMemory(0, bytesRead);
				var chunkSha256 = _checksum.ComputeSha256(chunkData.ToArray());
				var chunkStream = new MemoryStream(chunkData.ToArray());

				var subject = $"{SubjectPrefix}{cloudName}<{i}/{steps}>";
				var message = CreateMailMessageChunk(subject, chunkStream, fileSize, fileSha256, chunkSha256, i, steps);

				await _imap.AppendAsync(folderPath, message, ct);
				onProgress?.Invoke(i, steps);
				_logger.LogInformation("Uploaded chunk {I}/{Total}: {Name} (SHA256={Hash})",
					i, steps, cloudName, chunkSha256[..8]);
			}
		}
	}

	/// <summary>
	/// 上传整个文件夹
	/// </summary>
	public async Task UploadFolderAsync(
		string cloudPath, string folderPath, string localPath,
		Action<int, int, string>? onProgress = null, CancellationToken ct = default)
	{
		cloudPath = NormalizeCloudPath(cloudPath);
		if (!Directory.Exists(localPath))
			throw new DirectoryNotFoundException($"Directory not found: {localPath}");

		// 先获取已有文件列表，跳过已存在的
		var existingFiles = await ListFilesAsync(folderPath, deleteMissing: true, ct: ct);
		var existingNames = new HashSet<string>(existingFiles.Select(f => f.Name));

		// 统计总文件数以显示进度
		var allFiles = CountFiles(localPath);

		await UploadFolderRecursive(cloudPath, folderPath, localPath, existingNames, onProgress, 0, allFiles, ct);
	}

	private static int CountFiles(string localPath)
	{
		var count = 0;
		foreach (var dir in Directory.GetDirectories(localPath))
			count += CountFiles(dir);
		count += Directory.GetFiles(localPath).Length;
		return count;
	}

	private async Task<int> UploadFolderRecursive(
		string cloudPath, string folderPath, string localPath, HashSet<string> existingFiles,
		Action<int, int, string>? onProgress, int counter, int total, CancellationToken ct)
	{
		// 子目录
		foreach (var dir in Directory.GetDirectories(localPath))
		{
			var dirName = Path.GetFileName(dir);
			counter = await UploadFolderRecursive($"{cloudPath}/{dirName}", folderPath, dir, existingFiles, onProgress, counter, total, ct);
		}

		// 文件
		foreach (var file in Directory.GetFiles(localPath))
		{
			var fileName = Path.GetFileName(file);
			var cloudName = $"{cloudPath}/{fileName}".TrimStart('/');

			counter++;
			if (existingFiles.Contains(cloudName))
			{
				_logger.LogInformation("Skipping existing file: {Name}", cloudName);
				onProgress?.Invoke(counter, total, cloudName + " (skip)");
				continue;
			}

			onProgress?.Invoke(counter, total, cloudName);

			try
			{
				await UploadFileAsync(cloudName, folderPath, file, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to upload {File}", file);
			}
		}

		return counter;
	}

	// ==================== 下载 ====================

	/// <summary>
	/// 下载单个文件（自动拼接分卷）
	/// </summary>
	public async Task DownloadFileAsync(
		string cloudName, string folderPath, string localPath,
		CancellationToken ct = default, Action<int, int>? onProgress = null)
	{
		if (File.Exists(localPath))
			throw new IOException($"File already exists: {localPath}");

		var invalidChars = Path.GetInvalidFileNameChars();
		cloudName = invalidChars.Aggregate(cloudName, (s, c) => s.Replace(c.ToString(), ""));

		var summaries = await _imap.FetchSummariesAsync(folderPath, ct: ct);

		// 查找匹配的文件邮件
		var matches = summaries.Where(m =>
		{
			var subject = m.Envelope?.Subject ?? "";
			if (!subject.StartsWith(SubjectPrefix)) return false;
			var name = subject[SubjectPrefix.Length..];
			if (name == cloudName) return true;
			if (name.StartsWith($"{cloudName}<"))
			{
				var match = ChunkAnyRegex.Match(name);
				return match.Success && match.Groups[1].Value == cloudName;
			}
			return false;
		}).ToList();

		if (matches.Count == 0)
			throw new FileNotFoundException($"File not found on cloud: {cloudName}");

		// 判断是单文件还是分卷
		var firstSubject = matches[0].Envelope!.Subject![SubjectPrefix.Length..];
		bool isSingle;

		if (firstSubject == cloudName)
			isSingle = true;
		else
		{
			var cm = ChunkFirstRegex.Match(firstSubject);
			isSingle = !cm.Success;
		}

		if (isSingle)
		{
			await DownloadSingleFile(matches[0], folderPath, localPath, ct);
			onProgress?.Invoke(1, 1);
		}
		else
		{
			await DownloadChunkedFile(matches, folderPath, localPath, cloudName, ct, onProgress);
		}
	}

	private async Task DownloadSingleFile(
		IMessageSummary summary, string folderPath, string localPath, CancellationToken ct)
	{
		var message = await _imap.GetMessageAsync(folderPath, summary.UniqueId, ct);
		var attachment = message.Attachments.OfType<MimePart>().FirstOrDefault();
		if (attachment?.Content == null)
			throw new InvalidDataException("No attachment found in mail");

		await using var output = File.Create(localPath);
		await attachment.Content.DecodeToAsync(output, ct);

		// 校验 SHA256（从完整邮件头读取）
		var expectedSha = message.Headers["X-MailDisk-SHA256"];
		if (!string.IsNullOrEmpty(expectedSha))
		{
			output.Position = 0;
			var ok = await _checksum.VerifyAsync(output, expectedSha, ct);
			_logger.LogInformation(ok
				? "SHA256 verified: {File}"
				: "SHA256 MISMATCH: {File}", localPath);
		}
	}

	private async Task DownloadChunkedFile(
		List<IMessageSummary> matches, string folderPath, string localPath, string cloudName,
		CancellationToken ct, Action<int, int>? onProgress = null)
	{
		// 从第一分卷解析总分卷数
		var first = matches.FirstOrDefault(m =>
		{
			var s = m.Envelope!.Subject![SubjectPrefix.Length..];
			return ChunkFirstRegex.IsMatch(s);
		});

		if (first == null)
			throw new InvalidDataException("Cannot find first chunk of file");

		var firstMatch = ChunkFirstRegex.Match(first.Envelope!.Subject![SubjectPrefix.Length..]);
		var totalChunks = int.Parse(firstMatch.Groups[2].Value);

		// 检查完整性
		var subjects = new HashSet<string>(matches.Select(m => m.Envelope!.Subject![SubjectPrefix.Length..]));
		for (var i = 1; i <= totalChunks; i++)
		{
			if (!subjects.Contains($"{cloudName}<{i}/{totalChunks}>"))
				throw new InvalidDataException($"Missing chunk {i}/{totalChunks} for file: {cloudName}");
		}

		// 按分卷序号排序
		var orderedMatches = matches
			.Select(m =>
			{
				var match = ChunkAnyRegex.Match(m.Envelope!.Subject![SubjectPrefix.Length..]);
				var idx = match.Success ? int.Parse(match.Groups[2].Value) : 1;
				return (Index: idx, Summary: m);
			})
			.OrderBy(x => x.Index)
			.ToList();

		await using var output = File.Create(localPath);
		string? expectedFileSha = null;

		foreach (var (idx, summary) in orderedMatches)
		{
			_logger.LogInformation("Downloading chunk {I}/{Total}: {Name}", idx, totalChunks, cloudName);
			onProgress?.Invoke(idx, totalChunks);

			var message = await _imap.GetMessageAsync(folderPath, summary.UniqueId, ct);
			var attachment = message.Attachments.OfType<MimePart>().FirstOrDefault();
			if (attachment?.Content == null)
				throw new InvalidDataException($"No attachment in chunk {idx}");

			// 从第一分卷读取文件 SHA256
			if (idx == 1)
				expectedFileSha = message.Headers["X-MailDisk-SHA256"];

			// 先解码到内存流以验证分卷 SHA256
			using var ms = new MemoryStream();
			await attachment.Content.DecodeToAsync(ms, ct);
			ms.Position = 0;

			var expectedChunkSha = message.Headers["X-MailDisk-Chunk-SHA256"];
			if (!string.IsNullOrEmpty(expectedChunkSha))
			{
				var ok = await _checksum.VerifyAsync(ms, expectedChunkSha, ct);
				if (!ok)
					_logger.LogWarning("Chunk SHA256 mismatch: {Name}<{I}/{Total}>", cloudName, idx, totalChunks);
			}

			ms.Position = 0;
			await ms.CopyToAsync(output, ct);
		}

		// 校验完整文件 SHA256
		if (!string.IsNullOrEmpty(expectedFileSha))
		{
			output.Position = 0;
			var ok = await _checksum.VerifyAsync(output, expectedFileSha, ct);
			_logger.LogInformation(ok
				? "SHA256 verified: {File}"
				: "SHA256 MISMATCH: {File}", localPath);
		}
	}

	/// <summary>
	/// 下载整个文件夹
	/// </summary>
	public async Task DownloadFolderAsync(
		string cloudPath, string folderPath, string localPath,
		Action<int, int, string>? onProgress = null, CancellationToken ct = default)
	{
		cloudPath = NormalizeCloudPath(cloudPath) + "/";
		localPath = localPath.Replace('\\', '/').TrimEnd('/') + "/";

		var files = await ListFilesAsync(folderPath, deleteMissing: false, ct: ct);
		var matching = files.Where(f => f.Name.StartsWith(cloudPath)).ToList();
		var total = matching.Count;
		var current = 0;

		foreach (var file in matching)
		{
			var relativePath = file.Name[cloudPath.Length..];
			if (string.IsNullOrEmpty(relativePath)) continue;

			current++;
			onProgress?.Invoke(current, total, file.Name);

			var localFile = localPath + relativePath.Replace('/', Path.DirectorySeparatorChar);
			var localDir = Path.GetDirectoryName(localFile);
			if (localDir != null)
				Directory.CreateDirectory(localDir);

			try
			{
				await DownloadFileAsync(file.Name, folderPath, localFile, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to download {File}", file.Name);
			}
		}
	}

	// ==================== 辅助方法 ====================

	/// <summary>
	/// 创建邮件消息（单文件）
	/// </summary>
	private MimeMessage CreateMailMessage(
		string fileName, Stream content, long fileSize, string sha256,
		int chunkIndex, int chunkTotal)
	{
		var subject = chunkTotal > 1
			? $"{SubjectPrefix}{fileName}<{chunkIndex}/{chunkTotal}>"
			: $"{SubjectPrefix}{fileName}";

		return CreateMailMessageChunk(subject, content, fileSize, sha256, "", chunkIndex, chunkTotal);
	}

	/// <summary>
	/// 创建邮件消息（分卷）
	/// </summary>
	private MimeMessage CreateMailMessageChunk(
		string subject, Stream content, long fileSize, string fileSha256,
		string chunkSha256, int chunkIndex, int chunkTotal)
	{
		var message = new MimeMessage();
		message.From.Add(MailboxAddress.Parse(_settings.Address));
		message.To.Add(MailboxAddress.Parse(_settings.Address));
		message.Subject = subject;

		// 自定义头
		message.Headers.Add("X-MailDisk-File-Size", fileSize.ToString());
		if (!string.IsNullOrEmpty(fileSha256))
			message.Headers.Add("X-MailDisk-SHA256", fileSha256);
		if (chunkTotal > 1)
		{
			message.Headers.Add("X-MailDisk-Chunk-Index", chunkIndex.ToString());
			message.Headers.Add("X-MailDisk-Chunk-Total", chunkTotal.ToString());
		}
		if (!string.IsNullOrEmpty(chunkSha256))
			message.Headers.Add("X-MailDisk-Chunk-SHA256", chunkSha256);

		// 正文
		var body = new TextPart("plain") { Text = MailBodyText };

		// 附件
		var attachment = new MimePart("application", "octet-stream")
		{
			Content = new MimeContent(content, ContentEncoding.Default),
			ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
			ContentTransferEncoding = ContentEncoding.Base64,
			FileName = AttachmentName
		};

		var multipart = new Multipart("mixed");
		multipart.Add(body);
		multipart.Add(attachment);
		message.Body = multipart;

		return message;
	}

	private static string NormalizeCloudPath(string path)
	{
		path = path.Replace('\\', '/').TrimStart('/').TrimEnd('/');
		while (path.Length > 0 && path[0] == '/')
			path = path[1..];
		return path;
	}
}

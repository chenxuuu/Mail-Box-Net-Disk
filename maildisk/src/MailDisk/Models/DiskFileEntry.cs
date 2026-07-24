namespace MailDisk.Models;

/// <summary>
/// 文件条目信息
/// </summary>
public class DiskFileEntry
{
	public string Name { get; init; } = "";
	public bool IsChunked { get; init; }
	public int ChunkCount { get; init; }
	public long? FileSize { get; init; }
	public string? Sha256 { get; init; }

	public override string ToString() => IsChunked ? $"{Name} ({ChunkCount} parts)" : Name;
}

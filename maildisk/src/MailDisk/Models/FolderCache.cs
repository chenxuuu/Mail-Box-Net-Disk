using System.Text.Json.Serialization;

namespace MailDisk.Models;

public class FolderCache
{
	[JsonPropertyName("uidValidity")]
	public uint UidValidity { get; set; }

	[JsonPropertyName("uidNext")]
	public uint UidNext { get; set; }

	[JsonPropertyName("files")]
	public Dictionary<string, FileCacheEntry> Files { get; set; } = new();
}

public class FileCacheEntry
{
	[JsonPropertyName("chunks")]
	public List<ChunkCacheEntry> Chunks { get; set; } = new();
}

public class ChunkCacheEntry
{
	[JsonPropertyName("u")]
	public uint Uid { get; set; }

	[JsonPropertyName("i")]
	public int Index { get; set; }
}

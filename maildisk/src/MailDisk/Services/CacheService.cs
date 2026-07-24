using System.Text.Json;
using MailDisk.Models;
using Microsoft.Extensions.Logging;

namespace MailDisk.Services;

public class CacheService
{
	private static string CachePath =>
		Path.Combine(AppContext.BaseDirectory, "cache.json");

	private Dictionary<string, FolderCache>? _cache;
	private readonly ILogger<CacheService> _logger;

	public CacheService(ILogger<CacheService> logger)
	{
		_logger = logger;
	}

	public FolderCache? GetFolder(string folderPath)
	{
		LoadIfNeeded();
		return _cache!.TryGetValue(folderPath, out var v) ? v : null;
	}

	public void SetFolder(string folderPath, FolderCache cache)
	{
		LoadIfNeeded();
		_cache![folderPath] = cache;
		Save();
	}

	public void InvalidateFolder(string folderPath)
	{
		LoadIfNeeded();
		if (_cache!.Remove(folderPath))
		{
			Save();
			_logger.LogDebug("Cache invalidated for folder: {Folder}", folderPath);
		}
	}

	private void LoadIfNeeded()
	{
		if (_cache != null) return;

		if (File.Exists(CachePath))
		{
			try
			{
				var json = File.ReadAllText(CachePath);
				_cache = JsonSerializer.Deserialize<Dictionary<string, FolderCache>>(json)
					?? new Dictionary<string, FolderCache>();
				_logger.LogDebug("Cache loaded from {Path}", CachePath);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to load cache, resetting");
				_cache = new Dictionary<string, FolderCache>();
			}
		}
		else
		{
			_cache = new Dictionary<string, FolderCache>();
		}
	}

	private void Save()
	{
		try
		{
			var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(CachePath, json);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to save cache");
		}
	}
}

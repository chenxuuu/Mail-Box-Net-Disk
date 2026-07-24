using System.Text.Json;
using System.Text.Json.Serialization;

namespace MailDisk.Configuration;

/// <summary>
/// 应用配置模型，对应 mail.json
/// </summary>
public class AppSettings
{
	[JsonPropertyName("imap")]
	public string ImapServer { get; set; } = "";

	[JsonPropertyName("port")]
	public int Port { get; set; } = 993;

	[JsonPropertyName("ssl")]
	public bool UseSsl { get; set; } = true;

	[JsonPropertyName("account")]
	public string Account { get; set; } = "";

	[JsonPropertyName("password")]
	public string Password { get; set; } = "";

	[JsonPropertyName("address")]
	public string Address { get; set; } = "";

	[JsonPropertyName("block")]
	public long BlockSizeMb { get; set; } = 25;

	[JsonPropertyName("skipSslVerify")]
	public bool SkipSslVerify { get; set; }

	public long BlockSizeBytes => BlockSizeMb * 1024 * 1024;
}

/// <summary>
/// mail.json 配置管理
/// </summary>
public class SettingsManager
{
	private static string ConfigPath =>
		Path.Combine(AppContext.BaseDirectory, "mail.json");

	public bool Exists => File.Exists(ConfigPath);

	public AppSettings? Load()
	{
		if (!Exists) return null;
		var json = File.ReadAllText(ConfigPath);
		return JsonSerializer.Deserialize<AppSettings>(json);
	}

	public void Save(AppSettings settings)
	{
		var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(ConfigPath, json);
	}
}

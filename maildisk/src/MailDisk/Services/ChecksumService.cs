using System.Security.Cryptography;

namespace MailDisk.Services;

/// <summary>
/// SHA256 校验服务
/// </summary>
public class ChecksumService
{
	/// <summary>
	/// 计算流的 SHA256 哈希
	/// </summary>
	public async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
	{
		stream.Position = 0;
		using var sha = SHA256.Create();
		var hash = await sha.ComputeHashAsync(stream, ct);
		stream.Position = 0;
		return Convert.ToHexStringLower(hash);
	}

	/// <summary>
	/// 计算字节数组的 SHA256 哈希
	/// </summary>
	public string ComputeSha256(byte[] data)
	{
		var hash = SHA256.HashData(data);
		return Convert.ToHexStringLower(hash);
	}

	/// <summary>
	/// 验证流的内容是否匹配给定的 SHA256
	/// </summary>
	public async Task<bool> VerifyAsync(Stream stream, string expectedSha256, CancellationToken ct = default)
	{
		if (string.IsNullOrEmpty(expectedSha256))
			return true; // 无校验值，跳过验证
		var actual = await ComputeSha256Async(stream, ct);
		return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
	}
}

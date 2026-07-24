using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -u: 上传单个文件
/// </summary>
internal static class UploadCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath, string localFile, string cloudName)
	{
		if (cloudName.Contains('<'))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.UL_NoAngleBracket}[/]");
			return 1;
		}

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync($"{Locale.UL_Uploading} [cyan]{Markup.Escape(localFile)}[/] → [cyan]{Markup.Escape(folderPath)}/{Markup.Escape(cloudName)}[/]...", async ctx =>
			{
				await disk.UploadFileAsync(cloudName, folderPath, localFile);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.UL_Done}: [cyan]{Markup.Escape(cloudName)}[/][/]");

		return 0;
	}
}

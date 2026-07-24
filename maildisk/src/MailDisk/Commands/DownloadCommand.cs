using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -d: 下载单个文件
/// </summary>
internal static class DownloadCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath, string localFile, string cloudName)
	{
		if (cloudName.Contains('<'))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.DL_NoAngleBracket}[/]");
			return 1;
		}

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync($"{Locale.DL_Downloading} [cyan]{Markup.Escape(folderPath)}/{Markup.Escape(cloudName)}[/] → [cyan]{Markup.Escape(localFile)}[/]...", async ctx =>
			{
				await disk.DownloadFileAsync(cloudName, folderPath, localFile);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.DL_Done}: [cyan]{Markup.Escape(localFile)}[/][/]");

		return 0;
	}
}

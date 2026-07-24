using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -df: 下载整个文件夹
/// </summary>
internal static class DownloadFolderCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath, string localFolder, string cloudPath)
	{
		if (cloudPath.Contains('<'))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.DF_NoAngleBracket}[/]");
			return 1;
		}

		AnsiConsole.MarkupLine($"{Locale.DF_Downloading} [cyan]{Markup.Escape(folderPath)}/{Markup.Escape(cloudPath)}[/] → [cyan]{Markup.Escape(localFolder)}[/]...");

		await AnsiConsole.Progress()
			.AutoClear(false)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask(Locale.DF_DownloadingStatus, autoStart: true);
				task.IsIndeterminate();
				var lastTotal = 0;

				await disk.DownloadFolderAsync(cloudPath, folderPath, localFolder,
					onProgress: (current, total, name) =>
					{
						if (total != lastTotal)
						{
							task.MaxValue(total);
							task.IsIndeterminate(false);
							lastTotal = total;
						}
						task.Value(current);
						if (string.IsNullOrEmpty(name))
							task.Description = $"{Locale.DL_Refreshing} [cyan]{Markup.Escape(folderPath)}[/] ({current}/{total})";
						else
							task.Description = $"{Locale.DF_DownloadingStatus} [dim]{Markup.Escape(name)}[/] ({current}/{total})";
					});
				task.Description = Locale.DF_Done;
				task.Value(task.MaxValue);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.DF_Done}[/]");

		return 0;
	}
}

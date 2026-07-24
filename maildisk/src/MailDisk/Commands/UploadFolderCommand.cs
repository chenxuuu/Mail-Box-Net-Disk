using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -uf: 上传整个文件夹
/// </summary>
internal static class UploadFolderCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath, string localFolder, string cloudPath)
	{
		if (cloudPath.Contains('<'))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.UF_NoAngleBracket}[/]");
			return 1;
		}

		if (!Directory.Exists(localFolder))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.UF_FolderNotFound}: {Markup.Escape(localFolder)}[/]");
			return 1;
		}

		AnsiConsole.MarkupLine($"{Locale.UF_Uploading} [cyan]{Markup.Escape(localFolder)}[/] → [cyan]{Markup.Escape(folderPath)}/{Markup.Escape(cloudPath)}[/]...");

		await AnsiConsole.Progress()
			.AutoClear(false)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask(Locale.UF_UploadingStatus, autoStart: true);
				task.IsIndeterminate();

				await disk.UploadFolderAsync(cloudPath, folderPath, localFolder,
					onProgress: (current, total, name) =>
					{
						task.MaxValue(total);
						task.Value(current);
						task.Description = $"{Locale.UF_UploadingStatus} [dim]{Markup.Escape(name)}[/]";
					});
				task.Description = Locale.UF_Done;
				task.Value(task.MaxValue);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.UF_Done}[/]");

		return 0;
	}
}

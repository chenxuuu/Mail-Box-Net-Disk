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

		await AnsiConsole.Progress()
			.AutoClear(false)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask($"{Locale.UL_Uploading} [cyan]{Markup.Escape(cloudName)}[/]", autoStart: true);
				task.IsIndeterminate();
				var lastTotal = 0;

				await disk.UploadFileAsync(cloudName, folderPath, localFile,
					onProgress: (current, total) =>
					{
						if (total != lastTotal)
						{
							task.MaxValue(total);
							task.IsIndeterminate(false);
							lastTotal = total;
						}
						task.Value(current);
						if (total > 1)
							task.Description = $"{Locale.UL_Uploading} [cyan]{Markup.Escape(cloudName)}[/] ({current}/{total})";
					});
				task.Description = $"{Locale.UL_Done}: [cyan]{Markup.Escape(cloudName)}[/]";
				task.Value(task.MaxValue);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.UL_Done}: [cyan]{Markup.Escape(cloudName)}[/][/]");

		return 0;
	}
}

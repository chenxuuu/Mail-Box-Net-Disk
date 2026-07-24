using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -c: 清理不完整分卷
/// </summary>
internal static class CleanCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath)
	{
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.CL_NoFolder}[/]");
			return 1;
		}

		await AnsiConsole.Progress()
			.AutoClear(false)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask($"{Locale.CL_Cleaning} [cyan]{Markup.Escape(folderPath)}[/]", autoStart: true);
				task.IsIndeterminate();
				var lastTotal = 0;

				await disk.ListFilesAsync(folderPath, deleteMissing: true, onProgress: (current, total) =>
				{
					if (total != lastTotal)
					{
						task.MaxValue(total);
						lastTotal = total;
					}
					task.Value(current);
				});
				task.Value(task.MaxValue);
			});

		AnsiConsole.MarkupLine($"[green]{Locale.CL_Done} [cyan]{Markup.Escape(folderPath)}[/].[/]");

		return 0;
	}
}

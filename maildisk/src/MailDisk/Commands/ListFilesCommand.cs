using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -l: 列出云端文件
/// </summary>
internal static class ListFilesCommand
{
	public static async Task<int> RunAsync(DiskService disk, string folderPath)
	{
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.LS_NoFolder}[/]");
			return 1;
		}

		var files = new List<Models.DiskFileEntry>();
		await AnsiConsole.Progress()
			.AutoClear(false)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask($"{Locale.LS_Fetching} [cyan]{Markup.Escape(folderPath)}[/]", autoStart: true);
				task.IsIndeterminate();
				var lastTotal = 0;

				files.AddRange(await disk.ListFilesAsync(folderPath, onProgress: (current, total) =>
				{
					if (total != lastTotal)
					{
						task.MaxValue(total);
						task.IsIndeterminate(false);
						lastTotal = total;
					}
					task.Value(current);
					task.Description = $"{Locale.LS_Fetching} [cyan]{Markup.Escape(folderPath)}[/] ({current}/{total})";
				}));
				task.Value(task.MaxValue);
			});

		AnsiConsole.WriteLine();

		if (files.Count == 0)
		{
			AnsiConsole.MarkupLine($"[yellow]{Locale.LS_NoFiles}[/]");
			return 0;
		}

		var table = new Table();
		table.Border(TableBorder.Rounded);
		table.AddColumn(new TableColumn($"[bold]{Locale.LS_ColumnIndex}[/]").RightAligned());
		table.AddColumn($"[bold]{Locale.LS_ColumnName}[/]");
		table.AddColumn(new TableColumn($"[bold]{Locale.LS_ColumnSize}[/]").RightAligned());
		table.AddColumn($"[bold]{Locale.LS_ColumnType}[/]");

		var index = 1;
		foreach (var f in files)
		{
			var sizeStr = f.FileSize.HasValue
				? FormatSize(f.FileSize.Value)
				: "-";
			var typeStr = f.IsChunked ? $"{f.ChunkCount} {Locale.LS_TypeChunked}" : Locale.LS_TypeSingle;

			table.AddRow(
				(index++).ToString(),
				Markup.Escape(f.Name),
				sizeStr,
				typeStr
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine($"[green]{files.Count} {Locale.LS_Total}[/]");

		return 0;
	}

	private static string FormatSize(long bytes)
	{
		return bytes switch
		{
			< 1024 => $"{bytes} B",
			< 1024 * 1024 => $"{bytes / 1024.0:F1} KiB",
			< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MiB",
			_ => $"{bytes / (1024.0 * 1024 * 1024):F2} GiB"
		};
	}
}

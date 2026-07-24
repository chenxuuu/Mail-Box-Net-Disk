using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -lf: 列出所有邮件文件夹
/// </summary>
internal static class ListFoldersCommand
{
	public static async Task<int> RunAsync(ImapService imap)
	{
		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync(Locale.LF_Getting, async ctx =>
			{
				var folders = await imap.GetFoldersAsync();
				ctx.Spinner(Spinner.Known.Dots);

				AnsiConsole.MarkupLine($"[bold]{Locale.LF_Title}:[/]");
				var table = new Table();
				table.AddColumn(Locale.LF_Column);
				table.Border(TableBorder.Rounded);

				foreach (var f in folders)
					table.AddRow(Markup.Escape(f.FullName));

				AnsiConsole.Write(table);
			});

		return 0;
	}
}

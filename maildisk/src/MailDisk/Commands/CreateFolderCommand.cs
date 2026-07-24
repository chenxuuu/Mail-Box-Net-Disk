using MailDisk.Services;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -cf: 创建邮件文件夹
/// </summary>
internal static class CreateFolderCommand
{
	public static async Task<int> RunAsync(ImapService imap, string folderName)
	{
		if (string.IsNullOrWhiteSpace(folderName))
		{
			AnsiConsole.MarkupLine($"[red]{Locale.CF_NoName}[/]");
			return 1;
		}

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync($"{Locale.CF_Creating} [cyan]{Markup.Escape(folderName)}[/]...", async ctx =>
			{
				await imap.CreateFolderAsync(folderName);
				AnsiConsole.MarkupLine($"[green]{Locale.CF_Created}: [cyan]{Markup.Escape(folderName)}[/].[/]");
			});

		return 0;
	}
}

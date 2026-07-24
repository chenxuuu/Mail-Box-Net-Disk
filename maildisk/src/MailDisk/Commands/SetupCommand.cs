using MailDisk.Configuration;
using Spectre.Console;

namespace MailDisk.Commands;

/// <summary>
/// -s: 交互式设置 IMAP 参数
/// </summary>
internal static class SetupCommand
{
	public static async Task<int> RunAsync(SettingsManager settingsManager)
	{
		AnsiConsole.MarkupLine($"[yellow]{Locale.Setup_Notice}[/]");
		AnsiConsole.WriteLine();

		var settings = new AppSettings();

		settings.ImapServer = AnsiConsole.Ask<string>(Locale.Setup_ImapPrompt);
		settings.Port = AnsiConsole.Ask(Locale.Setup_PortPrompt, 993);
		settings.UseSsl = AnsiConsole.Confirm(Locale.Setup_SslPrompt, true);
		settings.Account = AnsiConsole.Ask<string>(Locale.Setup_AccountPrompt);
		settings.Password = AnsiConsole.Prompt(
			new TextPrompt<string>(Locale.Setup_PasswordPrompt).Secret());
		settings.Address = AnsiConsole.Ask<string>(Locale.Setup_AddressPrompt);
		settings.BlockSizeMb = AnsiConsole.Ask(Locale.Setup_BlockPrompt, 25L);
		settings.SkipSslVerify = AnsiConsole.Confirm(Locale.Setup_SkipSslPrompt, false);

		settingsManager.Save(settings);
		AnsiConsole.MarkupLine($"[green]{Locale.Setup_Done}[/]");

		return 0;
	}
}

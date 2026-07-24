using MailDisk.Commands;
using MailDisk.Configuration;
using MailDisk.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MailDisk;

class Program
{
	static async Task<int> Main(string[] args)
	{
		if (args.Length == 0)
		{
			AnsiConsole.MarkupLine($"[red]{Locale.Common_NoCommand}[/]");
			return 1;
		}

		try
		{
			return args[0] switch
			{
				"-h" => ShowHelp(),
				"-s" => await RunSetupAsync(),
				"-lf" => await RunWithImap((imap, _) =>
					ListFoldersCommand.RunAsync(imap)),
				"-cf" => await RunWithImap((imap, _) =>
					CreateFolderCommand.RunAsync(imap, Arg(args, 1))),
				"-l" => await RunWithDisk((disk, _) =>
					ListFilesCommand.RunAsync(disk, Arg(args, 1))),
				"-c" => await RunWithDisk((disk, _) =>
					CleanCommand.RunAsync(disk, Arg(args, 1))),
				"-u" => await RunWithDisk((disk, _) =>
					UploadCommand.RunAsync(disk, Arg(args, 1), Arg(args, 2), Arg(args, 3))),
				"-d" => await RunWithDisk((disk, _) =>
					DownloadCommand.RunAsync(disk, Arg(args, 1), Arg(args, 2), Arg(args, 3))),
				"-uf" => await RunWithDisk((disk, _) =>
					UploadFolderCommand.RunAsync(disk, Arg(args, 1), Arg(args, 2), Arg(args, 3))),
				"-df" => await RunWithDisk((disk, _) =>
					DownloadFolderCommand.RunAsync(disk, Arg(args, 1), Arg(args, 2), Arg(args, 3))),
				_ => UnknownCommand(args[0])
			};
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]{Locale.Err_ErrorPrefix}: {Markup.Escape(ex.Message)}[/]");
			return 1;
		}
	}

	// ==================== 服务提供 ====================

	private static ServiceProvider BuildServices()
	{
		var services = new ServiceCollection();

		// 配置
		var settingsManager = new SettingsManager();
		var settings = settingsManager.Load();
		if (settings == null)
		{
			AnsiConsole.MarkupLine($"[red]{Locale.Common_SettingsNotFound}[/]");
			return null!;
		}
		services.AddSingleton(settings);
		services.AddSingleton(settingsManager);

		// 日志
		services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

		// 服务
		services.AddSingleton<ImapService>();
		services.AddSingleton<ChecksumService>();
		services.AddSingleton<DiskService>();

		return services.BuildServiceProvider();
	}

	private delegate Task<int> ImapCommand(ImapService imap, AppSettings settings);
	private delegate Task<int> DiskCommand(DiskService disk, AppSettings settings);

	private static async Task<int> RunWithImap(ImapCommand cmd)
	{
		using var sp = BuildServices();
		if (sp == null) return 1;
		var imap = sp.GetRequiredService<ImapService>();
		var settings = sp.GetRequiredService<AppSettings>();
		return await cmd(imap, settings);
	}

	private static async Task<int> RunWithDisk(DiskCommand cmd)
	{
		using var sp = BuildServices();
		if (sp == null) return 1;
		var disk = sp.GetRequiredService<DiskService>();
		var settings = sp.GetRequiredService<AppSettings>();
		return await cmd(disk, settings);
	}

	private static async Task<int> RunSetupAsync()
	{
		var settingsManager = new SettingsManager();
		return await SetupCommand.RunAsync(settingsManager);
	}

	// ==================== 帮助 ====================

	private static int ShowHelp()
	{
		var panel = new Panel(
			$"[bold]{Locale.Help_Title}[/]\n\n" +
			$"{Locale.Help_Tagline}\n\n" +
			$"[bold]{Locale.Help_Commands}:[/]\n" +
			$"  [cyan]-h[/]                      {Locale.Help_ShowHelp}\n" +
			$"  [cyan]-s[/]                      {Locale.Help_Setup}\n" +
			$"  [cyan]-lf[/]                     {Locale.Help_ListFolders}\n" +
			$"  [cyan]-cf[/] [grey]<folder>[/]             {Locale.Help_CreateFolder}\n" +
			$"  [cyan]-l[/]  [grey]<folder>[/]             {Locale.Help_ListFiles}\n" +
			$"  [cyan]-c[/]  [grey]<folder>[/]             {Locale.Help_Clean}\n" +
			$"  [cyan]-u[/]  [grey]<folder> <local> <cloud>[/]   {Locale.Help_Upload}\n" +
			$"  [cyan]-d[/]  [grey]<folder> <local> <cloud>[/]   {Locale.Help_Download}\n" +
			$"  [cyan]-uf[/] [grey]<folder> <local> <cloud>[/]   {Locale.Help_UploadFolder}\n" +
			$"  [cyan]-df[/] [grey]<folder> <local> <cloud>[/]   {Locale.Help_DownloadFolder}\n\n" +
			$"[dim]{Locale.Help_Note}[/]")
		{
			Border = BoxBorder.Rounded,
			Header = new PanelHeader(" Mail Disk "),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
		return 0;
	}

	private static int UnknownCommand(string cmd)
	{
		AnsiConsole.MarkupLine($"[red]{Locale.Common_UnknownCommand}: {Markup.Escape(cmd)}[/]");
		AnsiConsole.MarkupLine(Locale.Common_UseHelp);
		return 1;
	}

	private static string Arg(string[] args, int index)
	{
		if (index >= args.Length)
		{
			throw new ArgumentException(
				$"{Locale.Common_MissingArg} {index}{Locale.Common_UseHForHelp}");
		}
		return args[index];
	}
}

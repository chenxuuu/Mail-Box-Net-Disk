using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;
using maildisk.apis;
using System.IO;

namespace maildisk
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            switch(args[0])
            {
                case "-h":
                        Console.WriteLine(@"
***********
*Mail Disk*
***********
You can use these commands:

-h: 
show commands we support.

-s:
set your imap settings.

-lf:
list all folders on mail server.

-cf <folder name>:
create new folder on mail server.

-l <email folder>:
show files in this folder.

-c <email folder>:
clear all wrong files in this folder.

-u <email folder> <local file> <file name on cloud>:
upload a file to net disk.

-d <email folder> <local file> <file name on cloud>:
download a file from net disk.

-uf <email folder> <local folder> <folder name on cloud>:
upload a folder to net disk and clear all wrong files in this email folder.
Notice: if file is exist on cloud, it will not be uploaded.

-df <email folder> <local folder> <folder name on cloud>:
download a folder from net disk.
Notice: local file must be not exist.

All file and path should not contain '<'".Replace("\r\n","\r\n\t"));
                        return;

                    case "-s":
                        Settings.Set();
                        return;

                    case "-lf":
                        var lfdisk = Settings.GetDisk();
                        if (lfdisk == null) return;
                        Console.WriteLine("getting all folders ...");
                        var all = lfdisk.GetFolders();
                        Console.WriteLine("here's all folders:");
                        foreach(var f in all)
                        {
                            Console.WriteLine(f.FullName);
                        }
                        return;

                    case "-cf":
                        var cfdisk = Settings.GetDisk();
                        if (cfdisk == null) return;
                        if(args.Length < 2) { Console.WriteLine("please enter a folder name");return; }
                        Console.WriteLine($"creating folder {args[1]} ...");
                        cfdisk.CreatFolder(args[1]);
                        return;

                    case "-l":
                        var ldisk = Settings.GetDisk();
                        if (ldisk == null) return;
                        if (args.Length < 2) { Console.WriteLine("please enter a folder name"); return; }
                        Console.WriteLine($"fetching file list with folder {args[1]} ...");
                        var lfiles = ldisk.GetFileList(args[1]);
                        Console.WriteLine($"\r\n\r\ndone! list of files:");
                        foreach (var s in lfiles)
                        {
                            Console.WriteLine(s);
                        }
                        return;

                    case "-c":
                        var cdisk = Settings.GetDisk();
                        if (cdisk == null) return;
                        if (args.Length < 2) { Console.WriteLine("please enter a folder name"); return; }
                        Console.WriteLine($"fetching file list with folder {args[1]} ...");
                        cdisk.GetFileList(args[1], true);
                        Console.WriteLine($"\r\n\r\ndone!");
                        return;

                    case "-u":
                        var udisk = Settings.GetDisk();
                        if (udisk == null) return;
                        if (args.Length < 3) { Console.WriteLine("wrong args count"); return; }
                        Console.WriteLine($"uploading file {args[2]} to {args[1]} as {args[3]} ...");
                        if(args[3].IndexOf("<")>=0)
                        {
                            Console.WriteLine($"error! file name do not contain '<'");
                            return;
                        }
                        udisk.UploadBigFile(args[3], args[1], args[2], (int)Settings.maxBlock * 1024 * 1024);
                        return;

                    case "-d":
                        var ddisk = Settings.GetDisk();
                        if (ddisk == null) return;
                        if (args.Length < 3) { Console.WriteLine("wrong args count"); return; }
                        Console.WriteLine($"Download file {args[3]} from {args[1]} as {args[2]} ...");
                        if (args[3].IndexOf("<") >= 0)
                        {
                            Console.WriteLine($"error! file name do not contain '<'");
                            return;
                        }
                        ddisk.DownloadFile(args[1], args[3], args[2]);
                        return;

                    case "-uf":
                        var ufdisk = Settings.GetDisk();
                        if (ufdisk == null) return;
                        if (args.Length < 3) { Console.WriteLine("wrong args count"); return; }
                        Console.WriteLine($"upload folder {args[3]} to {args[1]} as {args[2]} ...");
                        if (args[3].IndexOf("<") >= 0)
                        {
                            Console.WriteLine($"error! folder name do not contain '<'");
                            return;
                        }
                        ufdisk.UploadFolder(args[3], args[1], args[2], (int)Settings.maxBlock * 1024 * 1024);
                        Console.WriteLine("done! all files uploaded!");
                        return;

                    case "-df":
                        var dfdisk = Settings.GetDisk();
                        if (dfdisk == null) return;
                        if (args.Length < 3) { Console.WriteLine("wrong args count"); return; }
                        Console.WriteLine($"Download folder {args[3]} from {args[1]} as {args[2]} ...");
                        if (args[3].IndexOf("<") >= 0)
                        {
                            Console.WriteLine($"error! folder name do not contain '<'");
                            return;
                        }
                        dfdisk.DownloadFolder(args[3], args[1], args[2]);
                        Console.WriteLine("done! all files downloaded!");
                        return;

                    default:
                        break;
            }
            Console.WriteLine(@"no commond matched
use -h to show commands we support");
            return;
        }
    }
}

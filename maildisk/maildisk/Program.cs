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

-u <email folder> <local file> <file name on cloud>:
upload a file to net disk.

-d <email folder> <local file> <file name on cloud>:
download a file from net disk.".Replace("\r\n","\r\n\t"));
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
                        foreach(var s in ldisk.GetFileList(args[1]))
                        {
                            Console.WriteLine(s);
                        }
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


                    default:
                        break;
            }
            Console.WriteLine(@"no commond matched
use -h to show commands we support");
            return;
        }
    }
}

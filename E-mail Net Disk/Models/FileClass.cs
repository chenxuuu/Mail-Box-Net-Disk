using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_mail_Net_Disk.Models
{
    class NetFileItem
    {
        public string FileName { get; set; }
        public String FileHash { get; set; }
        public string FileSize { get; set; }
        public string FileDateCreated { get; set; }
        public uint FileSum { get; set; }
    }

    class FileClass
    {
        

        //public static ObservableCollection<NetFileItem> GetFileList()
        //{
        //    var netfiles = new ObservableCollection<NetFileItem>();
        //    netfiles.Add(new NetFileItem { FileName = "name1name1name1name1name1", FileHash = "hashhashhashhashhashhashhash1", FileSize = 12345, FileDateCreated = "2017/3/19" });
        //    netfiles.Add(new NetFileItem { FileName = "name2name1name1name1name1name1name1", FileHash = "hashhashhashhashhashhashhash2", FileSize = 12345, FileDateCreated = "2017/3/19" });
        //    netfiles.Add(new NetFileItem { FileName = "name3", FileHash = "hashhashhashhashhashhashhash3", FileSize = 12345, FileDateCreated = "2017/3/19" });
        //    netfiles.Add(new NetFileItem { FileName = "name4", FileHash = "hashhashhashhashhashhashhash4", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name5", FileHash = "hashhashhashhashhashhashhash5", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name6", FileHash = "hashhashhashhashhashhashhash6", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name7", FileHash = "hashhashhashhashhashhashhash7", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name8name1name1name1name1name1name1", FileHash = "hashhashhashhashhashhashhash8", FileSize = 12345566548456546, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name9", FileHash = "hashhashhashhashhashhashhash9", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name10", FileHash = "hashhashhashhashhashhashhash10", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    netfiles.Add(new NetFileItem { FileName = "name11", FileHash = "hashhashhashhashhashhashhash11", FileSize = 12345, FileDateCreated = "data/00/00" });
        //    return netfiles;
        //}

    }
}

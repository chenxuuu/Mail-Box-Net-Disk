using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using E_mail_Net_Disk.Models;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Popups;
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace E_mail_Net_Disk
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Files : Page
    {
        private ObservableCollection<NetFileItem> NetFiles;

        public Files()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            NetFiles = new ObservableCollection<NetFileItem>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await FileManager.GetFilesLocal(NetFiles);
            await settings.GetSettings();
        }

        private async void ShowMessageDialog(string s, string title)
        {
            var msgDialog = new Windows.UI.Popups.MessageDialog(s) { Title = title };
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("确定"));
            await msgDialog.ShowAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await FileManager.GetFilesLocal(NetFiles);
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openFile = new FileOpenPicker();
            openFile.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openFile.ViewMode = PickerViewMode.List;
            openFile.FileTypeFilter.Add("*");

            // 选取单个文件
            StorageFile file = await openFile.PickSingleFileAsync();

            if (file != null)
            {
                Windows.Storage.FileProperties.BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                string filename = file.Name;
                string filehash = file.GetHashCode().ToString();
                string filesize = FileManager.FileSizeConvert(basicProperties.Size);
                string filedata = System.DateTime.Now.ToString();

                NetFiles.Add(new NetFileItem
                {
                    FileName = filename,
                    FileHash = filehash,
                    FileSize = filesize,
                    FileDateCreated = filedata
                });
                try
                {
                    UploadProgressRing.Visibility = Visibility.Visible;
                    UploadTextBlock.Visibility = Visibility.Visible;
                    await FileManager.UploadFiles(file);
                }
                catch
                {
                    ShowMessageDialog("上传过程中出现错误！请检查邮箱账号设置！", "提示");
                }

                await FileManager.SaveSettings(NetFiles);
                UploadProgressRing.Visibility = Visibility.Collapsed;
                UploadTextBlock.Visibility = Visibility.Collapsed;
            }
        }


        private async Task<int> ShowMSG(string filename)
        {
            int resultsum = 2;
            var dialog = new MessageDialog(string.Format("{0}\r\n请选择你要进行的动作：",filename), "消息提示");

            dialog.Commands.Add(new UICommand("下载（没开发）", cmd => { resultsum = 0; }, commandId: 0));
            dialog.Commands.Add(new UICommand("删除", cmd => { resultsum = 1; }, commandId: 1));
            dialog.Commands.Add(new UICommand("取消", cmd => { resultsum = 2; }, commandId: 2));

            //设置默认按钮，不设置的话默认的确认按钮是第一个按钮
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 2;

            //获取返回值
            /*var result = */await dialog.ShowAsync();
            return resultsum;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var file = (NetFileItem)e.ClickedItem;
            int result = await ShowMSG(file.FileName);
            if (result == 0)
            {

            }
            else if (result == 1)
            {
                for (int i = 0; i < NetFiles.Count; i++)
                {
                    if (NetFiles[i].FileHash == file.FileHash && 
                        NetFiles[i].FileDateCreated == file.FileDateCreated && 
                        NetFiles[i].FileSize == file.FileSize &&
                        NetFiles[i].FileName == file.FileName)
                        NetFiles.Remove(NetFiles[i]);
                }
                await FileManager.SaveSettings(NetFiles);
            }
        }
    }
}

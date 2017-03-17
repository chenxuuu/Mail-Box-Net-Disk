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

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace E_mail_Net_Disk
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void IconListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesButton.IsSelected)
            {
                MyFrame.Navigate(typeof(Files));
                RefreshButton.Visibility = Visibility.Visible;
                Title.Text = "文件";
            }
            else if (ProgressButton.IsSelected)
            {
                MyFrame.Navigate(typeof(ProgressPage));
                RefreshButton.Visibility = Visibility.Collapsed;
                Title.Text = "传输进度";
            }
            else if (AccountButton.IsSelected)
            {
                MyFrame.Navigate(typeof(Account));
                RefreshButton.Visibility = Visibility.Collapsed;
                Title.Text = "账号设置";
            }
            else if (NoticeButton.IsSelected)
            {
                MyFrame.Navigate(typeof(Notice));
                RefreshButton.Visibility = Visibility.Collapsed;
                Title.Text = "用法";
            }
            else if (AboutButton.IsSelected)
            {
                MyFrame.Navigate(typeof(About));
                RefreshButton.Visibility = Visibility.Collapsed;
                Title.Text = "关于";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

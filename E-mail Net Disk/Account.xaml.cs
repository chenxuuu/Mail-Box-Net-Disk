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
using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace E_mail_Net_Disk
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Account : Page
    {
        public Account()
        {
            this.InitializeComponent();
        }

        private async void SaveSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.Parse(PortTextBox.Text);
                StorageFolder folder;
                folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹

                var file_demonstration = await folder.CreateFileAsync("settings", CreationCollisionOption.ReplaceExisting);
                //创建文件

                using (Stream file = await file_demonstration.OpenStreamForWriteAsync())
                {
                    using (StreamWriter write = new StreamWriter(file))
                    {
                        write.Write(string.Format("{0};{1};{2};{3};{4};{5}",
                                                    ImapTextBox.Text,
                                                    SmtpTextBox.Text,
                                                    PortTextBox.Text,
                                                    UserTextBox.Text,
                                                    PasswordTextBox.Password,
                                                    SSLCheckBox.IsChecked.ToString()
                                                   ));
                    }
                }
                ShowMessageDialog("设置成功！", "提示");
            }
            catch
            {
                ShowMessageDialog("参数错误！", "提示");
            }

            //DebugTextBlock.Text = string.Format("{0};{1};{2};{3};{4};{5}",
            //                                    ImapTextBox.Text,
            //                                    SmtpTextBox.Text,
            //                                    PortTextBox.Text,
            //                                    UserTextBox.Text,
            //                                    PasswordTextBox.Password,
            //                                    SSLCheckBox.IsChecked.ToString()
            //                                   );
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            StorageFolder folder;
            folder = ApplicationData.Current.RoamingFolder; //获取应用目录的文件夹

            var file_demonstration = await folder.CreateFileAsync("settings", CreationCollisionOption.OpenIfExists);
            //创建文件

            string s;
            using (Stream file = await file_demonstration.OpenStreamForReadAsync())
            {
                using (StreamReader read = new StreamReader(file))
                {
                    s = read.ReadToEnd();
                }
            }

            //DebugTextBlock.Text = s;

            if (s.IndexOf(";") >= 1 && s.IndexOf(";") != s.Length - 1)
            {
                string[] str2;
                int count_temp = 0;
                str2 = s.Split(';');
                foreach (string i in str2)
                {
                    if (count_temp == 0)
                    {
                        ImapTextBox.Text = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 1)
                    {
                        SmtpTextBox.Text = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 2)
                    {
                        PortTextBox.Text = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 3)
                    {
                        UserTextBox.Text = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 4)
                    {
                        PasswordTextBox.Password = i.ToString();
                        count_temp++;
                    }
                    else if (count_temp == 5)
                    {
                        if (i.ToString() == "True")
                        {
                            SSLCheckBox.IsChecked = true;
                        }
                        else
                        {
                            SSLCheckBox.IsChecked = false;
                        }
                        count_temp++;
                    }
                }
            }
        }

        private async void ShowMessageDialog(string s,string title)
        {
            var msgDialog = new Windows.UI.Popups.MessageDialog(s) { Title = title };
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("确定"));
            await msgDialog.ShowAsync();
        }
    }
}

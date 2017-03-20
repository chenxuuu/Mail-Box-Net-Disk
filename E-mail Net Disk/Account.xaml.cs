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
                settings.ImapURL = ImapTextBox.Text;
                settings.SmtpURL = SmtpTextBox.Text;
                settings.PortNumber = int.Parse(PortTextBox.Text);
                settings.UserName = UserTextBox.Text;
                settings.Password = PasswordTextBox.Password;
                settings.IsSSL = (bool)SSLCheckBox.IsChecked;
                await settings.SaveSettings();
                ShowMessageDialog("设置成功！", "提示");
            }
            catch
            {
                ShowMessageDialog("参数错误！", "提示");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                await settings.GetSettings();
                ImapTextBox.Text = settings.ImapURL;
                SmtpTextBox.Text = settings.SmtpURL;
                PortTextBox.Text = settings.PortNumber.ToString();
                UserTextBox.Text = settings.UserName;
                PasswordTextBox.Password = settings.Password;
                SSLCheckBox.IsChecked = settings.IsSSL;
            }catch { }
        }

        private async void ShowMessageDialog(string s,string title)
        {
            var msgDialog = new Windows.UI.Popups.MessageDialog(s) { Title = title };
            msgDialog.Commands.Add(new Windows.UI.Popups.UICommand("确定"));
            await msgDialog.ShowAsync();
        }
    }
}

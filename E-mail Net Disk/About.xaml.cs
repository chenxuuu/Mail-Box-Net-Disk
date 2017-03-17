using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace E_mail_Net_Disk
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class About : Page
    {
        public About()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var mailto = new Uri("mailto:lolicon@papapoi.com?subject=关于邮箱网盘的反馈&body=反馈的时候这几点要注意哦：<br/>1.如果是报告bug，请写明bug发生时是如何操作的<br/>2.提交建议的话尽量说的详细一点哦~<br/>暂时就这两点啦");
            await Launcher.LaunchUriAsync(mailto);
        }
    }
}

# 邮箱网盘

![build](https://github.com/chenxuuu/Mail-Box-Net-Disk/workflows/build/badge.svg)

利用邮箱实现网盘功能的工具。

本项目有两个仓库，不过建议在GitHub进行star/pr操作：

GitHub：[https://github.com/chenxuuu/Mail-Box-Net-Disk](https://github.com/chenxuuu/Mail-Box-Net-Disk)

git.osc：[https://gitee.com/chenxuuu/Mail-Box-Net-Disk](https://gitee.com/chenxuuu/Mail-Box-Net-Disk)

## 下载

每次发布版本，会自动在GitHub的release页更新：[点击前往](https://github.com/chenxuuu/Mail-Box-Net-Disk/releases/latest)

## 原理

利用邮箱附件作为文件存储空间，实现文件的上传下载功能

## 功能

- [x] 支持smtp/imap协议的邮箱
- [x] 读取文件夹列表/新建文件夹
- [x] 上传小于限制大小的文件
- [x] 读取已存在的文件
- [x] 上传大文件自动分卷
- [x] 识别分卷上传的文件，下载自动合并
- [x] 优化文件搜索速度
- [x] 完成命令行工具成品
- [x] 支持文件夹上传
- [x] 支持文件夹下载
- [ ] win系统下的gui管理工具
- [ ] 其他系统下的gui管理工具

## 命令列表

```cmd
maildisk -h 查看命令帮助

         -s
         更改邮箱参数设置

         -lf
         列出邮箱的所有邮件文件夹

         -cf <邮件文件夹名>
         新建一个邮件文件夹

         -l <邮件文件夹>
         列出所有该邮件文件夹下的文件

         -c <邮件文件夹>
         清除所有该邮件文件夹下不完整的分卷文件

         -u <邮件文件夹> <本地文件> <云端文件>
         上传文件

         -d <邮件文件夹> <本地文件> <云端文件>
         下载文件

         -uf <邮件文件夹> <本地文件夹> <云端虚拟文件夹路径>
         上传文件夹，并清理不完整分卷的邮件
         注意：如果云端存在该文件，则不会上传

         -df <邮件文件夹> <本地文件夹> <云端虚拟文件夹路径>
         下载文件夹

         所有文件和路径都不能包含'<'符号
```

## 加入该项目

如果你愿意忍受我写的智障代码，那么欢迎pr

## 其他

该项目的代码可以随意引用，但请保留指向该项目的说明文字

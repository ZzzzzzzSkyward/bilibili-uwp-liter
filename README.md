# BiliLite Fix

由逍遥橙子开发的PC端B站软件我非常喜欢，但是可惜二维码登录出了点问题，所以尝试修复一下。

## 更新说明

使用VS2019，Win10 x64 1809 17763版本升级了所有依赖，但是由于很多包都要求18362版本所以我没法升到最新的。

签名换了一个，到2024年过期。

包名换了一个，版本号改了。

## bug修复

`appkey`统一改成TV版，以应付B站的`appkey`与`access_key`检验

修复评论相关API缺少`csrf`字段，方案来自@ywmoyue

修复评论里面出现了超过`int`范围的数字

## bug增加

调试时动态页面发生`Not Implemented`崩溃，但是我不会修，因为我找不到到底是哪个函数

无法使用密码、手机登录，因为验证码显示不出来。所以只能二维码登录

日志文件的路径显示错误

## 下载

看release

## 其他fork

https://github.com/ywmoyue/biliuwp-lite

# 原介绍

第三方哔哩哔哩UWP客户端

下载及常见问题见：[https://www.showdoc.com.cn/biliuwpv4](https://www.showdoc.com.cn/biliuwpv4)

## 截图

[QQ截图20200730165337.png](https://vip1.loli.net/2020/08/02/rGLMwtVSYmaKgxi.png)

![](./screenshot/ui.png)

## 说明

运行项目需要添加FFmpeg引用。

- Nuget添加FFmpegInteropX.FFmpegUWP包。此包不支持HTTPS，可能无法正常观看直播。

- [下载](https://xiaoyaocz.lanzoui.com/i6aLtpn0kcf)并引用已编译的包，此包支持HTTPS。

	修改BiliLite.csproj里的FFmpeg路径
		
	```
	<Content Include="FFmpeg路径\$(PlatformTarget)\bin\*.dll" />
	```
	
- 自定义编译。详见[FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX)

## 参考及引用

[SYEngine](https://github.com/ShanYe/SYEngine)

[FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX)

[bilibili-API-collect](https://github.com/SocialSisterYi/bilibili-API-collect)

[bilibili-grpc-api](https://github.com/SeeFlowerX/bilibili-grpc-api)

[FontAwesome5](https://github.com/MartinTopfstedt/FontAwesome5)

[waslibs](https://github.com/wasteam/waslibs)
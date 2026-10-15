# Zakun's Advanced Computer System

ProjectMER 示意图里的方块会变成屏幕。方块上写的程序画在其中一个面上。第一个点击的玩家占用这块屏幕。

[English](README.md) · [Türkçe](README.tr.md) · [Русский](README.ru.md) · [한국어](README.ko.md)

作者：seksirakun48

## 需要

- SCP: Secret Laboratory 专用服务器
- LabAPI 1.1.7 或更高
- ProjectMER
- 你用来编译示意图的 Unity 工程

## 安装

把[发布页](https://github.com/seksirakun/Zakun-s-Advanced-Computer-System/releases)里的 `ZACS.dll` 放进 `LabAPI/plugins/global`。重启服务器。ZACS 在 ProjectMER 之后加载。

## 玩家

服务器会加一个按键。建议键是 E，名称是 `ZACS Interact`。两项都在插件配置里。

点击即占用屏幕。视线离开 4 秒、死亡或退出都会放开。其他人看到 `This terminal is in use.`

文本框会在屏幕上打开键盘。

## 地图

把 `unity` 文件夹里的内容复制到示意图工程的 `Assets`。

在 Schematic 下的立方体上：

1. 添加 **ZACS / Monitor Object**。
2. 填写 **Monitor Name**。自带名称是 `zacs.desktop` 和 `zacs.sample`。
3. 选择 **Face**。Forward 是方块本地 +Z 面。
4. 可选：把一个 Interactable 指定为 **Power Button**。
5. 按 **Compile Monitor**。

这一步会编译示意图，并在旁边写出 `ZACS.json`。用 ProjectMER 在游戏里生成该示意图。

找不到同名物体时，服务器日志是 `ZACS screen not found`。

之后如果重新编译示意图，要再按一次 Compile Monitor。只编译示意图不会更新显示器文件。

没有电源按钮且 **Starts On** 勾选：屏幕保持开启。Starts On 关闭且没有按钮：屏幕保持关闭。有按钮时，Starts On 只是生成时的状态。按钮负责开关。

## 自己的程序

在另一个 LabAPI 插件里引用 `ZACS.dll`。不要再加载一份。

```csharp
ZacsApi.Register("door.panel", session => new DoorPanel());
```

方块上的名称要和它一致。尺寸是 1080 高的虚拟像素。方块越大，面板越大。

[API](docs/api.zh.md)

编译本仓库需要专用服务器的 `Managed` 目录，以及 global plugins 里的 `ProjectMER.dll`。路径不同时用 `/p:ScpslManaged=` 和 `/p:ProjectMerDll=`。

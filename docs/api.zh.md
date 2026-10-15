# API

[English](api.md) · [Türkçe](api.tr.md) · [Русский](api.ru.md) · [한국어](api.ko.md)

引用 `ZACS.dll`。类型在 `Zacs` 命名空间。在你自己插件的 `Enable` 里注册。名称与方块上的 Monitor Name 相同，不区分大小写。

```csharp
public sealed class DoorPanel : MonitorProgram
{
    public override void Open()
    {
        Session.Root = Zml.Column(
            Zml.Text("DOOR").Font(42f).Paint(Palette.Brass),
            Zml.Button("Open", OnOpen).Tall(64f))
            .Pad(24f)
            .Gap(12f)
            .Back(Palette.Background);
    }

    void OnOpen(MonitorSession session, Button button)
    {
        button.Text = "Opened";
        session.Touch();
    }
}
```

`Open` 在示意图生成时调用，不是在玩家占用屏幕时。示意图被移除或程序重新加载时调用 `Close`。

## 注册

- `ZacsApi.Register(name, session => new YourProgram())`
- `ZacsApi.Unregister(name)`
- `ZacsApi.IsRegistered(name)`
- `ZacsApi.Sessions` — 当前存在的屏幕
- `ZacsApi.Find(schematicName, blockPath)`

如果这个名字已经在一块已生成的屏幕上，再次注册会重载那块屏幕。

## MonitorProgram

- `Open()`
- `Close()`
- `Tick(float delta)` — 屏幕开着时每帧调用
- `OnPower(bool on)`

`Session` 在 `Open` 之前赋值。

## MonitorSession

- `Root` — 在 `Open` 里放上控件树
- `Owner`、`HasOwner`
- `IsPowered`
- `MonitorName`、`SchematicName`、`BlockPath`
- `Claimed`、`Released`、`PowerChanged`
- `Touch()` — 文字、颜色或数值变了
- `Invalidate()` — 增删了控件
- `SetPower(bool)`
- `Release()` — 放开占用者
- `CloseField()` — 关掉键盘
- `Find(name)` — 用 `.Named("id")` 标过的控件

## 布局

像素相对配置里的 `VirtualHeight`。默认 1080。宽度跟随方块比例。

- `.Wide(280f)` 固定宽度
- `.Tall(84f)` 固定高度
- `.Flex(1f)` 分掉剩下的空间
- `.Font(36f)` 字号
- `.Pad(20f)` 内边距
- `.Gap(16f)` 子项间距
- `.Back(color)` 底板颜色
- `.Paint(color)` 文字颜色
- `.Named("id")` 查找名

`WidthUnit = LengthUnit.Percent` 且 `Width = 40` 时，占父级的 40%。高度同样。

`Zml.Column` 和 `Zml.Row` 排列子项。`Zml.Window` 加一条标题栏。`Zml.Separator` 是分隔线。`Zml.Spacer` 吃掉行或列里剩下的空间。

## 控件

- `Zml.Text`
- `Zml.Button(label, OnClick)` — `(MonitorSession, Button)`
- `Zml.Toggle(label, value, OnChanged)`
- `Zml.Slider(value, OnChanged)` — `Min`、`Max`、`Value`、`Normalized`
- `Zml.Progress(value)` — 0 到 1
- `Zml.Field(placeholder, OnSubmit)` — 单行，打开键盘

`Palette` 有 `Background`、`Plate`、`Raised`、`Line`、`Text`、`Muted`、`Brass`、`BrassDim`、`Field`、`Ok`、`Danger`、`Clear`。

## 占用

第一次点击占用屏幕，并按下指针下的控件。在占用者把视线移开 `ReleaseDelay` 秒（默认 4）、死亡或离开之前，其他人会被拒绝。这时触发 `Claimed` 和 `Released`。

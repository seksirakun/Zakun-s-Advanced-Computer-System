# API

[Türkçe](api.tr.md) · [Русский](api.ru.md) · [中文](api.zh.md) · [한국어](api.ko.md)

Reference `ZACS.dll`. The types are in the `Zacs` namespace. Register from your plugin's `Enable`. The name matches Monitor Name on the cube, and case does not matter.

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

`Open` runs when the schematic spawns, not when a player takes the screen. `Close` runs when the schematic is removed or the program is loaded again.

## Register

- `ZacsApi.Register(name, session => new YourProgram())`
- `ZacsApi.Unregister(name)`
- `ZacsApi.IsRegistered(name)`
- `ZacsApi.Sessions` — screens that exist right now
- `ZacsApi.Find(schematicName, blockPath)`

If that name is already on a spawned screen, registering it reloads the screen.

## MonitorProgram

- `Open()`
- `Close()`
- `Tick(float delta)` — each frame while the screen is on
- `OnPower(bool on)`

`Session` is set before `Open`.

## MonitorSession

- `Root` — assign the widget tree in `Open`
- `Owner`, `HasOwner`
- `IsPowered`
- `MonitorName`, `SchematicName`, `BlockPath`
- `Claimed`, `Released`, `PowerChanged`
- `Touch()` — a label, color, or value changed
- `Invalidate()` — widgets were added or removed
- `SetPower(bool)`
- `Release()` — drop the owner
- `CloseField()` — close the keyboard
- `Find(name)` — a widget marked with `.Named("id")`

## Layout

Pixels follow `VirtualHeight` in the config. The default is 1080. Width follows the cube.

- `.Wide(280f)` fixed width
- `.Tall(84f)` fixed height
- `.Flex(1f)` share the space that is left
- `.Font(36f)` text size
- `.Pad(20f)` padding inside
- `.Gap(16f)` space between children
- `.Back(color)` plate color
- `.Paint(color)` text color
- `.Named("id")` lookup name

`WidthUnit = LengthUnit.Percent` and `Width = 40` take 40% of the parent. Height works the same way.

`Zml.Column` and `Zml.Row` stack children. `Zml.Window` adds a title bar. `Zml.Separator` is a line. `Zml.Spacer` takes leftover space in a row or column.

## Controls

- `Zml.Text`
- `Zml.Button(label, OnClick)` — `(MonitorSession, Button)`
- `Zml.Toggle(label, value, OnChanged)`
- `Zml.Slider(value, OnChanged)` — `Min`, `Max`, `Value`, `Normalized`
- `Zml.Progress(value)` — from 0 to 1
- `Zml.Field(placeholder, OnSubmit)` — one line, opens the keyboard

`Palette` holds `Background`, `Plate`, `Raised`, `Line`, `Text`, `Muted`, `Brass`, `BrassDim`, `Field`, `Ok`, `Danger`, and `Clear`.

## Ownership

The first click claims the screen and presses the widget under the pointer. Other players are refused until the owner looks away for `ReleaseDelay` seconds (default 4), dies, or leaves. `Claimed` and `Released` fire for that.

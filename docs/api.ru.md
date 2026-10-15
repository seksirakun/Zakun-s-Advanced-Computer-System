# API

[English](api.md) · [Türkçe](api.tr.md) · [中文](api.zh.md) · [한국어](api.ko.md)

Подключите `ZACS.dll`. Типы лежат в пространстве имён `Zacs`. Регистрируйте программу в `Enable` своего плагина. Имя совпадает с Monitor Name на кубе, регистр не важен.

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

`Open` вызывается при спавне схематика, а не когда игрок забирает экран. `Close` вызывается, когда схематик удаляют или программу загружают заново.

## Регистрация

- `ZacsApi.Register(name, session => new YourProgram())`
- `ZacsApi.Unregister(name)`
- `ZacsApi.IsRegistered(name)`
- `ZacsApi.Sessions` — экраны, которые сейчас существуют
- `ZacsApi.Find(schematicName, blockPath)`

Если это имя уже стоит на заспавненном экране, регистрация перезагружает экран.

## MonitorProgram

- `Open()`
- `Close()`
- `Tick(float delta)` — каждый кадр, пока экран включён
- `OnPower(bool on)`

`Session` задаётся до `Open`.

## MonitorSession

- `Root` — дерево виджетов, задаётся в `Open`
- `Owner`, `HasOwner`
- `IsPowered`
- `MonitorName`, `SchematicName`, `BlockPath`
- `Claimed`, `Released`, `PowerChanged`
- `Touch()` — изменились текст, цвет или значение
- `Invalidate()` — виджеты добавили или убрали
- `SetPower(bool)`
- `Release()` — отпустить владельца
- `CloseField()` — закрыть клавиатуру
- `Find(name)` — виджет с `.Named("id")`

## Раскладка

Пиксели считаются от `VirtualHeight` в конфиге. По умолчанию 1080. Ширина берётся из пропорций куба.

- `.Wide(280f)` фиксированная ширина
- `.Tall(84f)` фиксированная высота
- `.Flex(1f)` делит оставшееся место
- `.Font(36f)` размер текста
- `.Pad(20f)` внутренний отступ
- `.Gap(16f)` промежуток между детьми
- `.Back(color)` цвет пластины
- `.Paint(color)` цвет текста
- `.Named("id")` имя для поиска

`WidthUnit = LengthUnit.Percent` и `Width = 40` занимают 40% родителя. С высотой так же.

`Zml.Column` и `Zml.Row` выстраивают детей. `Zml.Window` добавляет полосу заголовка. `Zml.Separator` — линия. `Zml.Spacer` забирает свободное место в ряду или колонке.

## Элементы

- `Zml.Text`
- `Zml.Button(label, OnClick)` — `(MonitorSession, Button)`
- `Zml.Toggle(label, value, OnChanged)`
- `Zml.Slider(value, OnChanged)` — `Min`, `Max`, `Value`, `Normalized`
- `Zml.Progress(value)` — от 0 до 1
- `Zml.Field(placeholder, OnSubmit)` — одна строка, открывает клавиатуру

В `Palette`: `Background`, `Plate`, `Raised`, `Line`, `Text`, `Muted`, `Brass`, `BrassDim`, `Field`, `Ok`, `Danger`, `Clear`.

## Владелец

Первое нажатие забирает экран и нажимает виджет под указателем. Остальные получают отказ, пока владелец не отведёт взгляд на `ReleaseDelay` секунд (по умолчанию 4), не умрёт или не выйдет. На это срабатывают `Claimed` и `Released`.

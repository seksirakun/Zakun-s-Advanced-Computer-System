# API

[English](api.md) · [Русский](api.ru.md) · [中文](api.zh.md) · [한국어](api.ko.md)

`ZACS.dll`'e referans ver. Tipler `Zacs` namespace'indedir. Kayıt işlemini plugin'inin `Enable` içinde yap. İsim, küpteki Monitor Name ile eşleşir. Büyük küçük harf bakılmaz.

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

`Open`, oyuncu ekranı alınca değil, schematic spawn olunca çalışır. `Close`, schematic silinince veya program yeniden yüklenince çalışır.

## Kayıt

- `ZacsApi.Register(name, session => new YourProgram())`
- `ZacsApi.Unregister(name)`
- `ZacsApi.IsRegistered(name)`
- `ZacsApi.Sessions` — o an duran ekranlar
- `ZacsApi.Find(schematicName, blockPath)`

Bu isim spawn olmuş bir ekrandaysa, kayıt o ekranı yeniden yükler.

## MonitorProgram

- `Open()`
- `Close()`
- `Tick(float delta)` — ekran açıkken her kare
- `OnPower(bool on)`

`Session`, `Open`'dan önce atanır.

## MonitorSession

- `Root` — ağacı `Open` içinde ver
- `Owner`, `HasOwner`
- `IsPowered`
- `MonitorName`, `SchematicName`, `BlockPath`
- `Claimed`, `Released`, `PowerChanged`
- `Touch()` — yazı, renk veya değer değişti
- `Invalidate()` — widget eklendi veya silindi
- `SetPower(bool)`
- `Release()` — sahibi bırakır
- `CloseField()` — klavyeyi kapatır
- `Find(name)` — `.Named("id")` ile işaretlenmiş widget

## Yerleşim

Pikseller config'teki `VirtualHeight` değerine göredir. Varsayılan 1080'dir. Genişlik küpün oranından gelir.

- `.Wide(280f)` sabit genişlik
- `.Tall(84f)` sabit yükseklik
- `.Flex(1f)` kalan alanı paylaşır
- `.Font(36f)` yazı boyutu
- `.Pad(20f)` iç boşluk
- `.Gap(16f)` çocuklar arası boşluk
- `.Back(color)` plaka rengi
- `.Paint(color)` yazı rengi
- `.Named("id")` arama adı

`WidthUnit = LengthUnit.Percent` ve `Width = 40`, ebeveynin yüzde 40'ıdır. Yükseklik de aynıdır.

`Zml.Column` ve `Zml.Row` çocukları dizer. `Zml.Window` başlık çubuğu ekler. `Zml.Separator` çizgidir. `Zml.Spacer` satırda veya sütunda kalan yeri alır.

## Kontroller

- `Zml.Text`
- `Zml.Button(label, OnClick)` — `(MonitorSession, Button)`
- `Zml.Toggle(label, value, OnChanged)`
- `Zml.Slider(value, OnChanged)` — `Min`, `Max`, `Value`, `Normalized`
- `Zml.Progress(value)` — 0 ile 1 arası
- `Zml.Field(placeholder, OnSubmit)` — tek satır, klavyeyi açar

`Palette`: `Background`, `Plate`, `Raised`, `Line`, `Text`, `Muted`, `Brass`, `BrassDim`, `Field`, `Ok`, `Danger`, `Clear`.

## Sahiplik

İlk tıklama ekranı alır ve imlecin altındaki widget'a basar. Sahibi `ReleaseDelay` saniye (varsayılan 4) bakmayana, ölene veya çıkana kadar başkası giremez. `Claimed` ve `Released` bu değişimde çalışır.

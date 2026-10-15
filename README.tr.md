# Zakun's Advanced Computer System

ProjectMER schematic'indeki bir küp oyunda ekran olur. Küpe yazdığın program o yüze çizilir. İlk tıklayan oyuncu ekranı alır.

[English](README.md) · [Русский](README.ru.md) · [中文](README.zh.md) · [한국어](README.ko.md)

Yazar: seksirakun48

## Gerekenler

- SCP: Secret Laboratory dedicated server
- LabAPI 1.1.7 veya üstü
- ProjectMER
- Schematic derlediğin Unity projesi

## Kurulum

[Release](https://github.com/seksirakun/Zakun-s-Advanced-Computer-System/releases) içindeki `ZACS.dll` dosyasını `LabAPI/plugins/global` klasörüne koy. Sunucuyu yeniden başlat. ZACS, ProjectMER'den sonra yüklenir.

## Oyuncu

Sunucu bir tuş ekler. Önerilen tuş E, yazısı `ZACS Interact`. İkisi de plugin config'indedir.

Tıklamak ekranı alır. 4 saniye bakmamak, ölmek veya çıkmak bırakır. Başkasına `This terminal is in use.` gider.

Yazı alanına basınca ekranda klavye açılır.

## Harita

`unity` klasörünün içindekileri schematic projesinin `Assets` klasörüne kopyala.

Schematic altındaki primitive küpte:

1. **ZACS / Monitor Object** ekle.
2. **Monitor Name** yaz. Hazır isimler: `zacs.desktop`, `zacs.sample`.
3. **Face** yüzü seçer. Forward, küpün yerel +Z yüzüdür.
4. İstersen bir Interactable'ı **Power Button** yap.
5. **Compile Monitor**'a bas.

Bu, schematic'i derler ve yanına `ZACS.json` yazar. Schematic'i oyunda ProjectMER ile spawn et.

İsimli obje yoksa konsola `ZACS screen not found` düşer.

Schematic'i sonra tekrar derlersen Compile Monitor'a yine bas. Schematic derlemesi monitör dosyasını kendi başına yenilemez.

Güç düğmesi yok ve **Starts On** açık: ekran açık kalır. Starts On kapalı ve düğme yok: kapalı kalır. Düğme varsa Starts On sadece spawn anıdır. Düğme açıp kapatır.

## Kendi programın

Başka bir LabAPI plugininden `ZACS.dll`'e referans ver. İkinci bir kopyasını yükleme.

```csharp
ZacsApi.Register("door.panel", session => new DoorPanel());
```

Küpteki isim bununla aynı olsun. Boyutlar 1080 yüksekliğindeki sanal pikseldir. Küp büyüdükçe panel de büyür.

[API](docs/api.tr.md)

Bu depoyu derlemek için dedicated server `Managed` klasörü ve global plugins içindeki `ProjectMER.dll` gerekir. Başka yerdelerse `/p:ScpslManaged=` ve `/p:ProjectMerDll=` ver.

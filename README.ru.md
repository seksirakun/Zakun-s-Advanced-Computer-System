# Zakun's Advanced Computer System

Куб в схематике ProjectMER становится экраном. Программа, указанная на кубе, рисуется на одной грани. Экран берёт тот, кто нажал первым.

[English](README.md) · [Türkçe](README.tr.md) · [中文](README.zh.md) · [한국어](README.ko.md)

Автор: seksirakun48

## Что нужно

- Выделенный сервер SCP: Secret Laboratory
- LabAPI 1.1.7 или новее
- ProjectMER
- Unity-проект, в котором вы уже собираете схематики

## Установка

Положите `ZACS.dll` из [релиза](https://github.com/seksirakun/Zakun-s-Advanced-Computer-System/releases) в `LabAPI/plugins/global`. Перезапустите сервер. ZACS загружается после ProjectMER.

## Игрок

Сервер добавляет клавишу. Предложенная клавиша — E, подпись — `ZACS Interact`. Обе задаются в конфиге плагина.

Нажатие забирает экран. Если отвести взгляд на 4 секунды, умереть или выйти, экран освобождается. Остальным показывается `This terminal is in use.`

Текстовое поле открывает клавиатуру на экране.

## Карта

Скопируйте содержимое папки `unity` в `Assets` проекта схематиков.

На примитив-куб внутри Schematic:

1. Добавьте **ZACS / Monitor Object**.
2. Укажите **Monitor Name**. Встроенные имена: `zacs.desktop` и `zacs.sample`.
3. Выберите **Face**. Forward — локальная грань +Z.
4. По желанию назначьте Interactable в **Power Button**.
5. Нажмите **Compile Monitor**.

Кнопка собирает схематик и пишет рядом `ZACS.json`. Заспавньте схематик через ProjectMER.

Если объекта с таким именем нет, сервер пишет `ZACS screen not found`.

После повторной сборки схематика снова нажмите Compile Monitor. Обычная сборка файл монитора сама не обновляет.

Нет кнопки питания и **Starts On** включён: экран остаётся включённым. Starts On выключен и кнопки нет: экран остаётся выключенным. С кнопкой Starts On — только состояние при спавне. Кнопка переключает питание.

## Своя программа

Подключите `ZACS.dll` из другого плагина LabAPI. Вторую копию не загружайте.

```csharp
ZacsApi.Register("door.panel", session => new DoorPanel());
```

Это же имя поставьте на куб. Размеры — пиксели экрана высотой 1080. Больший куб даёт большую панель.

[API](docs/api.ru.md)

Сборка этого репозитория требует папку `Managed` выделенного сервера и `ProjectMER.dll` в global plugins. Другие пути: `/p:ScpslManaged=` и `/p:ProjectMerDll=`.

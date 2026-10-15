# Zakun's Advanced Computer System

A cube in a ProjectMER schematic becomes a screen. The program named on that cube is drawn on one face. The first player who clicks owns it.

[Türkçe](README.tr.md) · [Русский](README.ru.md) · [中文](README.zh.md) · [한국어](README.ko.md)

Author: seksirakun48

## Requirements

- SCP: Secret Laboratory dedicated server
- LabAPI 1.1.7 or newer
- ProjectMER
- The Unity project you already use to compile schematics

## Install

Put `ZACS.dll` from the [release](https://github.com/seksirakun/Zakun-s-Advanced-Computer-System/releases) into `LabAPI/plugins/global`. Restart the server. ZACS loads after ProjectMER.

## Players

The server adds a key bind. Suggested key is E. The label is `ZACS Interact`. Both are in the plugin config.

Click takes the screen. Looking away for 4 seconds, dying, or leaving drops it. Anyone else sees `This terminal is in use.`

A text field opens a keyboard on the screen.

## Map

Copy the contents of `unity` into the `Assets` folder of your schematic project.

On a primitive cube under a Schematic:

1. Add **ZACS / Monitor Object**.
2. Set **Monitor Name**. Built-in names are `zacs.desktop` and `zacs.sample`.
3. Set **Face**. Forward is the cube's local +Z face.
4. Optional: assign an Interactable as **Power Button**.
5. Press **Compile Monitor**.

That compiles the schematic and writes `ZACS.json` beside it. Spawn the schematic with ProjectMER.

If the named object is missing, the server logs `ZACS screen not found`.

Press Compile Monitor again after you compile the schematic later. A schematic compile on its own does not refresh the monitor file.

No power button and **Starts On** enabled: the screen stays on. Starts On disabled and no button: it stays off. With a button, Starts On is only the state at spawn. The button toggles it.

## Your own program

Reference `ZACS.dll` from another LabAPI plugin. Do not load a second copy of it.

```csharp
ZacsApi.Register("door.panel", session => new DoorPanel());
```

Use that name on the cube. Sizes are pixels on a 1080-tall screen. A larger cube makes a larger panel.

[API](docs/api.md)

Building this repository needs the dedicated server `Managed` folder and `ProjectMER.dll` in the global plugins folder. Override the paths with `/p:ScpslManaged=` and `/p:ProjectMerDll=` if they sit somewhere else.

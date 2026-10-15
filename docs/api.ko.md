# API

[English](api.md) · [Türkçe](api.tr.md) · [Русский](api.ru.md) · [中文](api.zh.md)

`ZACS.dll`을 참조한다. 타입은 `Zacs` 네임스페이스에 있다. 자기 플러그인의 `Enable`에서 등록한다. 이름은 큐브의 Monitor Name과 같고, 대소문자는 보지 않는다.

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

`Open`은 플레이어가 화면을 차지할 때가 아니라 스키매틱이 스폰될 때 호출된다. 스키매틱이 사라지거나 프로그램이 다시 로드되면 `Close`가 호출된다.

## 등록

- `ZacsApi.Register(name, session => new YourProgram())`
- `ZacsApi.Unregister(name)`
- `ZacsApi.IsRegistered(name)`
- `ZacsApi.Sessions` — 지금 있는 화면
- `ZacsApi.Find(schematicName, blockPath)`

그 이름이 이미 스폰된 화면에 있으면, 등록이 그 화면을 다시 불러온다.

## MonitorProgram

- `Open()`
- `Close()`
- `Tick(float delta)` — 화면이 켜져 있는 동안 매 프레임
- `OnPower(bool on)`

`Session`은 `Open` 전에 들어간다.

## MonitorSession

- `Root` — 트리는 `Open`에서 넣는다
- `Owner`, `HasOwner`
- `IsPowered`
- `MonitorName`, `SchematicName`, `BlockPath`
- `Claimed`, `Released`, `PowerChanged`
- `Touch()` — 글, 색, 값이 바뀌었다
- `Invalidate()` — 위젯을 넣거나 뺐다
- `SetPower(bool)`
- `Release()` — 점유한 플레이어를 놓는다
- `CloseField()` — 키보드를 닫는다
- `Find(name)` — `.Named("id")`로 표시한 위젯

## 배치

픽셀은 설정의 `VirtualHeight`를 따른다. 기본값은 1080이다. 너비는 큐브 비율을 따른다.

- `.Wide(280f)` 고정 너비
- `.Tall(84f)` 고정 높이
- `.Flex(1f)` 남은 공간을 나눈다
- `.Font(36f)` 글자 크기
- `.Pad(20f)` 안쪽 여백
- `.Gap(16f)` 자식 사이 간격
- `.Back(color)` 판 색
- `.Paint(color)` 글자 색
- `.Named("id")` 찾는 이름

`WidthUnit = LengthUnit.Percent`이고 `Width = 40`이면 부모의 40%다. 높이도 같다.

`Zml.Column`과 `Zml.Row`는 자식을 쌓는다. `Zml.Window`는 제목 줄을 붙인다. `Zml.Separator`는 줄이다. `Zml.Spacer`는 행이나 열에 남은 자리를 차지한다.

## 컨트롤

- `Zml.Text`
- `Zml.Button(label, OnClick)` — `(MonitorSession, Button)`
- `Zml.Toggle(label, value, OnChanged)`
- `Zml.Slider(value, OnChanged)` — `Min`, `Max`, `Value`, `Normalized`
- `Zml.Progress(value)` — 0에서 1
- `Zml.Field(placeholder, OnSubmit)` — 한 줄, 키보드를 연다

`Palette`에는 `Background`, `Plate`, `Raised`, `Line`, `Text`, `Muted`, `Brass`, `BrassDim`, `Field`, `Ok`, `Danger`, `Clear`가 있다.

## 점유

첫 클릭이 화면을 차지하고, 포인터 아래 위젯을 누른다. 점유한 사람이 `ReleaseDelay`초(기본 4) 동안 시선을 떼거나, 죽거나, 나가기 전에는 다른 사람이 거절된다. 이때 `Claimed`와 `Released`가 호출된다.

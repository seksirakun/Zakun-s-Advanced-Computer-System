# Zakun's Advanced Computer System

ProjectMER 스키매틱의 큐브가 화면이 된다. 큐브에 적은 프로그램이 한 면에 그려진다. 처음 누른 플레이어가 화면을 차지한다.

[English](README.md) · [Türkçe](README.tr.md) · [Русский](README.ru.md) · [中文](README.zh.md)

작성: seksirakun48

## 필요한 것

- SCP: Secret Laboratory 데디케이티드 서버
- LabAPI 1.1.7 이상
- ProjectMER
- 스키매틱을 컴파일하는 Unity 프로젝트

## 설치

[릴리스](https://github.com/seksirakun/Zakun-s-Advanced-Computer-System/releases)의 `ZACS.dll`을 `LabAPI/plugins/global`에 넣는다. 서버를 다시 시작한다. ZACS는 ProjectMER 다음에 로드된다.

## 플레이어

서버가 키를 하나 추가한다. 제안 키는 E, 이름은 `ZACS Interact`다. 둘 다 플러그인 설정에 있다.

클릭하면 화면을 차지한다. 4초 동안 시선을 떼거나, 죽거나, 나가면 놓친다. 다른 플레이어에게는 `This terminal is in use.`가 보인다.

글 칸을 누르면 화면 위에 키보드가 열린다.

## 맵

`unity` 폴더 안의 내용을 스키매틱 프로젝트의 `Assets`에 복사한다.

Schematic 아래의 큐브에서:

1. **ZACS / Monitor Object**를 붙인다.
2. **Monitor Name**을 적는다. 기본 이름은 `zacs.desktop`, `zacs.sample`이다.
3. **Face**로 면을 고른다. Forward는 큐브의 로컬 +Z 면이다.
4. 원하면 Interactable을 **Power Button**에 넣는다.
5. **Compile Monitor**를 누른다.

스키매틱을 컴파일하고 옆에 `ZACS.json`을 쓴다. 게임에서는 ProjectMER로 스폰한다.

이름이 같은 오브젝트가 없으면 서버 로그에 `ZACS screen not found`가 나온다.

나중에 스키매틱을 다시 컴파일하면 Compile Monitor를 한 번 더 누른다. 스키매틱 컴파일만으로는 모니터 파일이 갱신되지 않는다.

전원 버튼이 없고 **Starts On**이 켜져 있으면 화면은 계속 켜져 있다. Starts On이 꺼져 있고 버튼이 없으면 계속 꺼져 있다. 버튼이 있으면 Starts On은 스폰 때의 상태일 뿐이다. 버튼이 켜고 끈다.

## 자기 프로그램

다른 LabAPI 플러그인에서 `ZACS.dll`을 참조한다. 복사본을 또 로드하지 않는다.

```csharp
ZacsApi.Register("door.panel", session => new DoorPanel());
```

큐브의 이름과 같아야 한다. 크기는 높이 1080인 가상 픽셀이다. 큐브가 커지면 패널도 커진다.

[API](docs/api.ko.md)

이 저장소를 빌드하려면 데디케이티드 서버의 `Managed` 폴더와 global plugins의 `ProjectMER.dll`이 필요하다. 경로가 다르면 `/p:ScpslManaged=`와 `/p:ProjectMerDll=`를 넘긴다.

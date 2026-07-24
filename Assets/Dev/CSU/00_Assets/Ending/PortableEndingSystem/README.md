# Portable Ending System

Unity 프로젝트에 복사해 사용하는 독립형 롤링 크레딧 패키지입니다.

다른 프로젝트의 Codex에게 설치와 통합까지 맡기려면
`TRANSFER_PROMPT_KO.md`의 프롬프트를 함께 전달하세요.

## 포함 기능

- `EndingCreditsData` ScriptableObject
- 최종 승리 이벤트와 크레딧 버튼에서 호출할 수 있는 `EndingSceneLoader`
- TMP preferred-height 기반 자동 크레딧 높이 계산
- `Time.unscaledDeltaTime` 기반 자동 스크롤
- `{GAME_TITLE}`, `{DEVELOPER_NAME}` 토큰 치환
- 개발자 이름 하이라이트와 영문 대문자 제목 스타일
- 텍스트 문구를 기준으로 한 사진 좌우 교차 배치
- 화면 크기 변경 시 진행률을 보존한 반응형 재배치
- BGM 재생, 페이드인, 페이드아웃
- 씬 진입/퇴장 화면 페이드
- 종료 버튼과 ESC 복귀
- Input System과 구 Input Manager 조건부 지원
- 엔딩 씬 및 데이터 SO 자동 생성 도구
- 열린 엔딩 씬 정적 검증 도구

## 요구 사항

- Unity 2022 LTS 이상 권장
- uGUI
- TextMesh Pro
- Input System은 선택 사항

외부 이미지, 폰트, 음원은 패키지에 포함하지 않았습니다.

## 설치

1. 이 패키지의 `Assets/PortableEndingSystem` 폴더를 대상 Unity 프로젝트의 `Assets` 아래로 복사합니다.
2. Unity 컴파일이 끝날 때까지 기다립니다.
3. 메뉴에서 `Tools > Portable Ending > Create Ending Scene`을 실행합니다.
4. 저장할 `Ending.unity` 위치를 선택합니다.
5. Builder가 같은 폴더에 만든 `Ending Credits Data.asset`을 선택합니다.
6. Inspector에서 게임명, 개발자명, 크레딧 본문, 사진, BGM, 복귀 씬을 설정합니다.
7. 생성된 씬의 `CreditsText`, 버튼 `Label`, `EscapeHint`에 한글을 포함하는 TMP Font Asset을 지정합니다.
8. `Tools > Portable Ending > Validate Open Ending Scene`을 실행합니다.
9. Play Mode에서 스크롤, 사진, BGM, ESC, 버튼 복귀를 확인합니다.

Builder는 생성한 Ending 씬을 Build Settings의 마지막에 추가합니다. `Ending Credits Data`의 `Exit Scene Name`에 지정한 씬도 Build Settings에 있어야 합니다.

## 엔딩 진입 연결

대상 프로젝트에 자체 씬 전환 서비스가 없다면 최종 승리 흐름을 유지하는 GameObject에
`EndingSceneLoader`를 추가한 뒤 다음과 같이 연결합니다.

1. `Ending Scene Name`을 생성한 Ending 씬 이름으로 설정합니다.
2. 화면 페이드가 필요하면 전체 화면 검정 UI의 `CanvasGroup`을 `Fade Overlay`에 연결합니다.
3. 승리 UnityEvent 또는 크레딧 버튼의 On Click에 `EndingSceneLoader.LoadEnding`을 연결합니다.

코드에서 호출해야 한다면 해당 컴포넌트 참조의 `LoadEnding()`을 호출하면 됩니다.
대상 프로젝트에 이미 씬 전환 서비스가 있다면 이 컴포넌트를 추가하지 않고 그 서비스로
Ending 씬을 로드하는 편이 좋습니다.

## 사진 데이터

각 사진에는 다음 값을 지정합니다.

- `Sprite`: 표시할 이미지
- `Anchor Text`: 사진을 붙일 크레딧 문구
- `Anchor Occurrence`: 같은 문구가 여러 번 나오면 몇 번째 문구인지 지정
- `Display Size`: 기준 표시 크기
- `Vertical Offset`: 문구 기준 세로 보정

사진은 목록 순서대로 왼쪽, 오른쪽을 번갈아 사용합니다. `Anchor Text`가 렌더링된 크레딧에서 발견되지 않으면 해당 사진은 숨겨지고 경고가 출력됩니다.

## 대상 프로젝트에 기존 오디오/전환 시스템이 있는 경우

기본 구현은 `AudioSource`와 `SceneManager.LoadSceneAsync`를 직접 사용합니다.

대상 프로젝트에 AudioManager, AudioMixer, 페이드 서비스 또는 Addressables 씬 전환이 이미 있다면:

- `EndingCreditsPlayer.PrepareBgm`
- `EndingCreditsPlayer.FadeBgmIn`
- `EndingCreditsPlayer.ExitRoutine`

세 지점을 기존 서비스 호출로 교체합니다. 중복 AudioManager나 전환 Manager를 만들 필요는 없습니다.

## SO를 다른 프로젝트로 옮길 때

`.asset` 파일만 옮기면 안 됩니다.

- `EndingCreditsData.cs`
- `Ending Credits Data.asset`
- SO가 참조하는 Sprite
- SO가 참조하는 AudioClip

을 함께 옮겨야 합니다.

기존 `.asset` 연결을 그대로 유지하려면 각 파일의 `.meta`도 함께 복사합니다. 대상 프로젝트에서 SO를 새로 만들 경우 `.meta`를 가져올 필요가 없습니다.

## 현재 프로젝트의 엔딩과 차이

이 패키지는 `GameAudioService`, `SoundId`, `SceneTransitionService`, `GameCursorController`에 의존하지 않습니다. 따라서 다른 프로젝트로 옮기기 쉽지만, 대상 프로젝트에 해당 서비스가 이미 있다면 README의 어댑트 지점을 통해 연결하는 것이 좋습니다.

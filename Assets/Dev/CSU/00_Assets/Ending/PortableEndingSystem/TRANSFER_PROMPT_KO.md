# 다른 Unity 프로젝트 적용용 프롬프트

아래 프롬프트를 대상 Unity 프로젝트를 연 Codex에 그대로 전달하세요.

---

현재 Unity 프로젝트에 `PortableEndingSystem`을 실제로 적용해 줘.

입력 패키지:

- 내가 제공한 `Assets/PortableEndingSystem` 폴더
- 패키지 안의 `README.md`

목표:

1. 먼저 대상 프로젝트의 Unity 버전, 렌더 파이프라인, 입력 시스템, 씬 전환 서비스, 오디오 서비스, 최종 승리 흐름, 메인 메뉴 씬 이름을 조사한다.
2. `Assets/PortableEndingSystem`을 대상 프로젝트에 복사하고 컴파일 오류가 없는지 확인한다.
3. `Tools > Portable Ending > Create Ending Scene`으로 `Ending.unity`와 `Ending Credits Data.asset`을 생성한다.
4. 프로젝트에 이미 있는 게임명, 개발자명, 엔딩 크레딧 문구, 사진 Sprite, 엔딩 BGM을 데이터 SO에 연결한다. 추측해서 잘못된 에셋을 연결하지 말고, 후보가 불명확하면 목록을 보고한다.
5. 한글을 모두 표시할 수 있는 프로젝트의 TMP Font Asset을 `CreditsText`, 버튼 Label, ESC 안내 텍스트에 지정한다.
6. 최종 스테이지 승리 후 Ending 씬으로 진입하게 연결한다. 기존 씬 전환 서비스가 없다면 `EndingSceneLoader.LoadEnding()`을 승리 이벤트에 연결한다. 기존 프로젝트에 씬 전환/페이드 서비스가 있으면 `EndingSceneLoader`를 쓰지 말고 기존 서비스로 진입시키며, `EndingCreditsPlayer`의 복귀 전환과 자체 화면 페이드도 중복되지 않도록 프로젝트 방식에 맞게 어댑터 또는 최소 수정으로 통합한다.
7. 기존 전역 AudioManager나 AudioMixer가 있으면 중복 BGM이 생기지 않도록 `AudioSource` 직접 재생 부분을 프로젝트 오디오 방식에 연결한다. 전역 서비스가 없다면 패키지 기본 구현을 유지한다.
8. `Exit Scene Name`을 실제 메인 메뉴 씬 이름으로 설정하고 Ending 씬과 복귀 씬을 Build Settings에 포함한다.
9. `Tools > Portable Ending > Validate Open Ending Scene`을 실행한다.
10. Play Mode에서 다음을 검증한다.
    - 화면 페이드인
    - 크레딧 자동 스크롤
    - 해상도 변경 후 스크롤 진행률 유지
    - 앵커 문구 옆 사진 좌우 교차 배치
    - BGM 재생과 페이드
    - ESC 및 종료 버튼 복귀
    - 최종 승리에서 Ending 씬 진입
    - Console 컴파일 오류와 Missing Script 없음
11. 기존 사용자 변경은 보존하고, 관계없는 파일은 수정하지 않는다.
12. 완료 후 변경 파일, 통합한 진입 지점, 검증 결과, 남은 수동 에셋 지정 항목을 짧게 보고한다.

작업 중 패키지의 namespace `PortableEndingSystem`은 유지하고, 대상 프로젝트 전용 의존성은 데이터 SO에 넣지 말고 진입/오디오/씬 전환 경계에서만 연결해 재사용성을 보존해 줘.

---

## 예상 작업 시간

- 패키지 복사 및 기본 씬 생성: 10~20분
- 기존 씬 전환·승리 흐름·오디오 서비스 연결: 30~90분
- 사진/BGM/TMP 폰트 지정 및 화면 조정: 30~60분
- Play Mode 검증과 수정: 30~60분

기존 프로젝트 구조가 일반적이면 총 1.5~3시간 정도입니다. Addressables, 커스텀 씬 로더, 전역 오디오 상태 머신, 여러 엔딩 분기까지 연결하면 3~6시간 정도를 잡는 편이 안전합니다.

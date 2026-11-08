# POC 검증

- 환경: 기존 Unity 6000.6.8f1 Editor(PID 25044, Pipeline port 7802), URP 17.6.0, Input System 1.20.1, uGUI 2.6.0. 버전·패키지 교체나 별도 Unity 인스턴스 실행 없음.
- 컴파일: 현재 오류 0 / 경고 0. 폐기 예정 객체 정렬 검색과 DEVELOPMENT_BUILD 전처리 경고는 공용 대체/DEBUG 변형으로 정리했다. ProjectValidation.Validate() 통과: 능력 정의 20, 패턴 7, 효과 슬롯 160, 빌드 씬/테이블 4개 일치, 새 Input System 설정 확인.
- Play의 프레임 진행을 확인했다. 최종 통합 검사의 frameStart/frameEnd는 Artifacts/POC/integration-results.json 원본을 따른다. 화면 캡처만을 실행 증거로 삼지 않았다.
- 실행 중 오류 0. LogKit 서버 키가 비어 있다는 경고 1건은 남아 있으며 서버 업로드는 검증하지 않았다. 로컬 JSONL 기록에는 영향이 없다.

| 검사 | 결과·범위 |
|---|---|
| 덱/테이블 스모크 | SmokeCheck.VerifyArenaData 호출 통과. 100장, 20종 각 5회, 각 20장 세트 중복 없음, 7패턴 |
| 20종 훅 | A01~A20 전부 발동. 공격형 18종 각각 실제 보스 피해 >0; A11 Magic 방어=true/Physical=false, A20 통과 이동. 패시브 스킬 입력은 무시 |
| 사망 100회 | 사망 이벤트 정확히 100회, 패배 이벤트 1회, 남은 목숨 0, 효과 0. 종료 후 추가 부활/중복 전이 없음 |
| 동시 사망 | A16 폭발로 보스 HP 0과 자기 사망이 같은 해결 프레임에 발생해 승리 1회 |
| 전생 정리 | A14 거대화/A20 유체화/A02 고속 대시 도중 실제 사망→부활 후 스케일 1, 일반 콜라이더, 대시/무적 없음, 예약 효과 0. 보스 HP·2페이즈 유지 |
| 7패턴 | B01~B07 각각 강제 시작 후 능력을 비운 플레이어가 기본 점프/이동/대시로 한 번씩 회피 |
| 2페이즈 연계 | B01→B02, B02→B04, B05→B03 관찰. 1페이즈에서는 연계 안 함 |
| 실제 입력 경로 | 가상 Keyboard/Mouse의 Input System 상태 이벤트를 큐에 넣어 D 이동, Space 점프, Shift 대시, J/LMB 평타, K/RMB 액티브, 패시브 K 무시 확인. 행동 함수를 직접 호출한 검사와 구분됨 |
| UI/진입 | 결과 패널 열림·UI Submit(Enter) 재시작, Preload→Title 자동 준비·Title Enter→Arena 100목숨 확인 |
| 별도 결함 확인 | A01 안전 스윙 후 궤적 재진입 사망; A15 경고 중 이탈 생존/정지 시 Magic 사망; A19 높은 착지에서 보스 피해 후 Fall 사망; A07 배후 유도 자기 관통; A14 Ceiling, A02 Wall, A20 Fall 사인 |
| 분석 파일 | TelemetryVerifier 통과. 최종 세션에서 session/sequence/time/revision 경계 확인, 필수 목숨 분석 필드 존재 |

- 자동 검사는 새 전투/고정 위치/강제 능력 지급/가속 시간 등 개발용 조작을 사용한다. 100목숨 검사는 8배 시간에서 수행했고 정상 게임의 부활 지연 테이블은 변경하지 않았다. 게임 플레이 난이도·완주 시간 측정으로 해석하면 안 된다.
- 검사 중 발견해 수정한 문제: 효과 bounds 깊이, 리지드바디 보간 좌표와 평타 위치 불일치, 위치 초기화 때 접촉 잔존, 능력 해제 후 HUD의 사망 능력 ID 누락, 즉시 A01 자해, A15 이탈 시간, A19 Fall/Explosion 사인 혼동, 전체 돌진 경로의 동시 판정, 도달하기 어려운 발판 배치.
- 전체 -projectSmoke 종료/풀/해금 시나리오 및 Test Runner는 실행하지 않았다. 테스트 asmdef는 추가하지 않았으며 POC 전용 스모크와 실제 Play 검사를 사용했다. 독립 실행 파일 빌드·Unity 6.3 에디터 실행도 미수행이다.
- 사람 플레이 미수행: 자연스러운 5~8분 완주, 모든 위치에서의 패턴/연계 회피, 능력별 선호·자살 리롤·웃음/사인 이해, 두 명 이상 블라인드 평가는 남아 있다.

산출물은 Artifacts/POC/의 integration-results/input-results/entry-results/boss-evasion-results/boss-links-results/ability-flaws-results JSON과 arena-final/victory/defeat/B01~B07-warning PNG다. 이 폴더는 로컬 검증 산출물이며 게임 자산은 Assets에 저장했다.

# 100 LIVES VS 1 BOSS POC

- Unity 6000.6.8f1 / URP 17.6.0, `poc/100-lives-arena`. ArenaPOC에서 Play 가능. 빌드는 Preload→Title→ArenaPOC로 진입하고 기존 Game 씬도 보존했다.
- 확정 규칙: 1HP·한 목숨에 능력 하나, 20종×5세트=100목숨, 보스 HP/페이즈 영속, 동시 사망은 보스 처치 승리 우선. 원본 기획은 Docs/GAME_DESIGN.md.
- 구현: Runtime/POC의 씬 로컬 세션·20능력·7패턴/2페이즈/3연계·160효과 슬롯·HUD/킬로그5줄/승패/재시작. runtime View/지형 생성, namespace/asmdef 없음.
- 수치·문구는 ArenaTableData.json. DTO(GameTables/GameTableRecords)→TableManager.Arena→GameTableRules 4계층, AbilityDefinition에는 ID만 저장. 표기는 Presentation이 담당한다.
- 입력은 InputSystem_Actions 자산 복제 + FindAction. A/D, Space, Shift, J/LMB, K/RMB; 패시브는 별도 스킬 없음.
- 도구: Tools/POC/Build Arena Scene, Debug Arena, Run Integration Checks (Play Mode). 검증: 컴파일 오류/경고0, 20종 발동·18공격 피해, 100사망/동시승리/정리, 입력/진입, 7패턴 기본 회피, 3연계, 분석 파일 계약 통과. 범위는 Docs/POC_TEST_REPORT.md.
- 분석은 GameEvents→TelemetryRecorder→TelemetryLog.GameTime. life_end와 arena_result summary를 기록하며 서버 전송은 없다. Docs/PLAY_LOG.md·TELEMETRY.md가 계약이다.
- 다음 확인: 사람의 5~8분 완주·블라인드 평가·조작감/효율 튜닝, 장시간 누수, 독립 실행 빌드. 오디오·세이브 미구현. LogKit 서버 키 없음 경고는 남아 있다.
- Unity 6.3/6.6 공용 API 정책/EntityId/정적 초기화 유지. 이번 실제 실행 검증은 6.6만 수행했다. 자세한 상태와 인계는 Docs/POC_STATUS.md·POC_HANDOFF.md.

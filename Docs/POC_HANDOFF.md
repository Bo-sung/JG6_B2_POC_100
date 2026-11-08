# POC 인수인계

- 프로젝트 C:/Gits/JG6_B2_POC_100, 브랜치 poc/100-lives-arena, Unity 6000.6.8f1/URP 17.6.0. AGENTS와 Docs의 기획 원칙 유지. 사용자 기존 tracked 변경을 보존했으며 통합 작업은 커밋하지 않았다. 별도 능력 워커의 기존 dd7bf93 커밋은 되돌리지 않았다.
- 구현 완료: ArenaPOC, A01~A20 런타임/정의/JSON, B01~B07/2페이즈/3연계, 100목숨·빠른 부활·HP 영속·승리 우선, HUD/킬로그 5슬롯/재시작, life_end 분석 경로.
- 실행: ArenaPOC를 열고 Play. Editor Play 시작 씬은 ArenaPOC로 설정했으며 빌드는 Preload→Title→ArenaPOC로 진입한다. 기존 Game 씬도 보존했다.
- 입력: A/D 이동, W/S 방향·공중 S 강하, Space 점프, Shift 대시, J/LMB 평타, K/RMB 액티브. 패시브는 K에 반응하지 않는다.
- 원본 데이터: Assets/Resources/Tables/ArenaTableData.json. DTO는 Runtime/Data/GameTables.cs·GameTableRecords.cs, TableManager.Arena, GameTableRules 순서다. AbilityDefinition은 ID만 보유한다. 능력의 추가 self*/spawnOffset/recoil*/spacing/emissionCount/impactDuration 값도 JSON 원본이다.
- 씬·프리팹·뷰 제작은 Tools/POC/Build Arena Scene. 능력/HP/패턴/사망 강제는 Debug Arena. 자동 통합은 Run Integration Checks (Play Mode); 종료 후 결과를 Artifacts/POC/integration-results.json에 쓴다.
- 검증 완료: 컴파일 오류/경고 0, ProjectValidation, 20종 발동/18공격 피해, 입력 바인딩과 Enter 재시작, 7패턴 기본 회피 1회씩, 3연계, 100회 사망 정확도, A16 동시 승리, A14/A20/A02 사망 정리, 핵심 자해 사인, JSONL 계약. 상세 한계는 POC_TEST_REPORT.md.
- 중요한 수정: 평타 중심은 motor.Position 사용. ResetState는 물리 접촉/Transform도 재설정. A19 fatal 파동은 보스 정산 뒤 SuicideKind=Fall. A01 첫 스윙은 자기 hurtbox 밖부터 시작. A15는 근거리 명중 때 경고 후 역튀김. A19 고도 문턱은 실제 발판 점프로 도달 가능하도록 조정했다.
- 미검증: 사람 완주/재미/능력별 효율, 모든 공간·연계의 회피 가능성, 장시간 누수, 독립 실행 빌드, Unity 6.3 실행. 런타임 LogKit 서버 키 없음 경고 1건; JSONL 서버 전송기는 없다.
- Unity CLI는 항상 --project-path C:/Gits/JG6_B2_POC_100 --caller plugin --skill unity-cli를 사용한다. AssetDatabase.ImportAsset 호출 금지. 저장 후 실제 창 포커스 전환으로 컴파일했으며 실행 후 Play 모드는 종료했다.

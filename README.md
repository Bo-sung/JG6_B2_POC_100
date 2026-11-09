# 100 LIVES VS 1 BOSS POC

- Unity 6000.6.8f1 / URP 17.6.0, `feature/mouse-aim`, 기준 `ba7b72a`. ArenaPOC를 열어 시작하며 빌드는 Preload→Title→ArenaPOC, 기존 Game 씬 보존.
- 기본 체력 1/A12 체력 6, 한 생명 한 능력 런타임, 20종×5=100목숨. 보스 HP/페이즈 영속, 동시 사망은 보스 처치 승리 우선.
- A/D·Space·공중 S·마우스 조준·Shift 대시·J/LMB 평타·Q 유지 전방 가드·K/RMB 스킬. 패리 없음. active는 K 제공 여부이며 패시브 훅과 병용 가능.
- 가드/능력 개정과 Player 프리팹·ArenaPOC·빌더·상태 HUD 연결 반영. A18은 기존 유지/재설계 보류. 상세 20종은 Docs/POC_STATUS.md.
- 수치/문구는 ArenaTableData JSON→DTO→TableManager→GameTableRules. 효과 160개는 씬 저작 슬롯, 런타임 View/지형 생성 없음. GameEvents→TelemetryRecorder 분석 경로 유지.
- 현재 버전은 Unity 컴파일과 Edit-mode 참조만 확인하며 사용자 플레이 미실시. 기존 성공/사용자 확인은 ba7b72a에만 해당. 인계·수동 확인 항목은 Docs/POC_HANDOFF.md.
- Tools/POC/Build Arena Scene·Author Guard Revision Views·Debug Arena. 구형 자동 통합은 오래된 규칙으로 오판하지 않게 차단했다. 커밋/푸시 없음.

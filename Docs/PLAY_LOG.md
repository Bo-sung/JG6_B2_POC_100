# 분석 이벤트 필드

- 원본: `TelemetryEvents` 이벤트 이름, `TelemetryPayload` 필드 대응. 파일 작성은 TelemetryRecorder 하나가 담당한다.
- `life_end`: lifeIndex, lifeId, abilityId, survivalSeconds, bossDamage, deathCause, source, bossPatternAtDeath, deckSeed, position{x,y,z}, time. 사망 확정마다 한 줄이며 테스트용 사망도 source=debug로 구분한다.
- `summary` / summaryId=`arena_result`: victory, livesUsed, total, bossHpRemaining, deckSeed, time. 같은 summaryId는 revision이 가장 큰 줄을 최종값으로 읽는다.
- 기존 `run_start`, `run_end`: scene, time, reason. `scene_change`: scene, loaded. `upgrade_granted`: id, displayName. `panel_visibility`: panel, open.
- 최상위 공통 필드: sessionId, sequence, time, timestampUtc, eventName, summaryId, revision, dataJson. dataJson은 payload JSON 문자열이다.
- 외부 분석 서버 연동은 없다. 서버를 추가하면 이벤트 이름·필드를 이 문서와 코드 양쪽에서 맞춘다.

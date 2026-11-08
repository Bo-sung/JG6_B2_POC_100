# 분석 기록 계약

- TelemetryLog는 `Resources/LogData.json`을 직접 읽고 `Application.persistentDataPath/Telemetry`에 JSONL을 기록한다. 도메인 밸런스 테이블과 분리한다.
- sessionId, sequence, FixedUpdate 기준 GameTime의 단일 소유자는 TelemetryLog다. 능력·보스·목숨 시스템은 별도 실행 ID/Time.time 시계를 만들지 않는다.
- 사건은 GameEvents에서 발행하고 TelemetryRecorder에서만 파일로 쓴다. 세부 이름·필드는 `PLAY_LOG.md`와 TelemetryEvents/TelemetryPayload가 원본이다.
- 목숨별 피해·생존 시간을 집계해 `life_end` 한 줄로 남긴다. 전투 최종 결과는 `WriteSummary("arena_result", payload)`이며 revision 최댓값이 정답이다.
- 콘솔·예외는 LogKit 경로를 사용한다. JSONL은 LogKit 업로드에 자동 포함되지 않으며 서버 전송기는 없다. 서버 주소·키는 Inspector/Configure/빌드 환경변수에서만 설정한다.
- 검증: `Tools/MyJG/Verify Telemetry File`, 개발 실행 인자 `-projectSmoke`, POC Play 통합 검사의 `Artifacts/POC/integration-results.json`. 실제 수행 여부와 결과는 `POC_TEST_REPORT.md`에 기록한다.

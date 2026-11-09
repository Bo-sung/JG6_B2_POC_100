# POC 실행 구조

- 대상은 Unity 6000.6.8f1 / URP 17.6.0. 버전·패키지 유지, 전역 namespace/Assembly-CSharp, Editor 폴더 규약, 테스트 asmdef 없음.
- ArenaSession이 단일 플레이어·LifeManager·AbilityController·BossController·DamageResolver·EffectSystem을 씬에서 소유한다. 런타임 View/지형 생성 없이 씬/프리팹 저작과 160개 비활성 효과 슬롯을 사용한다.
- 수치 원본: ArenaTableData.json → GameTables/GameTableRecords DTO → TableManager.Arena → GameTableRules 검증. AbilityDefinition에는 ID만 둔다. RequireValid로 오류 데이터 실행 차단, 표기/색은 Presentation 하나다.

## 입력과 능력

- PlayerActionController가 InputSystem_Actions 자산을 복제해 FindAction으로 읽는다. Update는 이동·조준과 입력 시점 방향, Guard held/pressed/released를 수집한다. FixedUpdate에서 동작·가드 상태를 처리한다.
- Player/Aim의 포인터 위치를 직렬화된 Camera로 플레이어 평면에 투영한다. 무효·근접 입력은 마지막 조준을 보존한다. 본체는 upright/양수 스케일, Eyes/Aim Pivot만 방향 표시한다.
- Player/Guard는 Q 유지다. 방향은 현재 마우스를 따라간다. 가드 시점 고정 반격 방향은 성공 때 캡처한다. 이동/점프/대시/평타/스킬 방향은 각 요청 계약을 따른다.
- 하나의 AbilityRuntime이 Modify/After/Landed/ModifyGuard/GuardStarted/GuardBlocked/Damaged/Tick/Cleanup을 제공한다. base player controller는 능력 ID 분기를 갖지 않는다.
- `active`는 K/RMB 스킬 하나를 제공한다는 뜻이며 기본 행동·스탯·가드 훅을 배제하지 않는다. K 제공 ID는 A03/A04/A06/A07/A12/A14/A15/A16/A17/A18/A19. 나머지는 K 동작 없음.
- A05는 성공한 물리 Q의 반격, A09는 가짜 Q, A10은 Q 시작 때 액체 귀환, A11은 약한 평타와 마법 전용 Q. A12는 강화 평타/체력과 K 충전 찌르기를 병용한다. 패리는 보류한다.

## 피해·체력·가드

- HitContext: lifeId/source/kind/damage/bypassSafety/fatal/guardable/hasOrigin/sourcePosition/incomingDirection/attackId. 출처는 안정 ID, 보이는 효과 위치·방향과 공통 연속 SAT 판정을 사용한다.
- 모든 보스/소유자/환경/자폭 피해는 DamageResolver 하나를 지난다. 보스 피해 먼저 해결하며, 생명 밖 지연 효과는 lifeId로 거부한다. attackId는 한 스윙의 여러 궤적 구간도 대상당 한 번만 피해를 주도록 한다.
- 기본 HP 1, A12 maxHealth는 JSON의 6. 일반 피격은 HP 차감하며 HP가 남으면 생명을 소모하거나 Death를 발행하지 않는다. fatal 환경/자폭은 HP와 무관하게 치명적이다.
- 가드는 전방 각도 정책과 공격 원점/입사 방향으로 판단한다. 기본 물리만 허용, A11은 마법만 허용, A09는 모두 실패. 보스 경량 B01은 guardable, B02~B04 중량은 불가, B05~B07 마법은 A11에서 가능하다. 소유자 효과·환경·fatal은 가드하지 않는다.
- 성공한 가드는 실제 피해가 아니다. A12에서 GuardStarted는 해당 생명의 GuardVulnerable을 고정하며 다음 실제 미방어 피해는 치명적이다. 체력·Q 상태·취약성은 PlayerStateChanged를 통해 전달한다.
- LifeManager는 사망을 pending으로 모아 LateUpdate에서 한 번 확정한다. ArenaSession은 해당 프레임 전체 해결 후 보스 HP 0을 우선해 승리를 결정한다. A16와 치명 A19 파동도 보스 피해 뒤 소유자 사망이다.

## 효과와 실제 운동

- EffectSlot은 씬 저작 사각형 슬롯을 재사용한다. 경고/활성 시각과 회전 판정을 일치시키며 bossHit/playerHit 및 attackId로 중복을 막는다. 효과 이동·몸 추종·유도와 previousPosition 갱신은 슬롯 내부에서 한 번 실행한다.
- A01: 저작된 Weapon Pivot/Swing Blade를 테이블 시간·각도로 휘두르고 같은 칼끝 궤도의 연결 구간을 월드에 4초 남긴다. 16구간/공유 attackId, 풀 예산 검증. 정면 정지 사각형으로 대체하지 않는다.
- A03: 몸에서 시작하는 광선이 캡처 방향을 유지하며 몸 위치를 추종. 발동 지점 위험 지역은 생성하지 않는다.
- A04: grounded 전용. 각 X에서 실제 Ground/Obstacle의 윗면 법선을 조회해 바닥·발판 칼날을 배치한다. 자기 위치 포함 홀수 13개, 풀 예산 제한.
- A07: 발사 축에 대한 owner AABB 투영 반경 + 회전 투사체 반길이 + 테이블 간격으로 발사점을 분리한다. 기존 고정 1.4는 대각 방향에서 합산 반경보다 작을 수 있었다. 슬롯 내부 유도로 외부 transform 이동/이전 위치 경합을 제거하며 실제 자기 교차는 유지한다. 이는 정적 코드 분석이며 플레이 재현 주장이 아니다.
- A10: 평타의 기본 근접 판정을 대체하는 이동 액체. Q 시작 시 현재 살아 있는 액체를 현재 소유자 위치로 돌린다. 액체가 없으면 별도 피해 없음.
- A06/A12/A15/A17: Immobilize가 기존 대시·관성 속도를 취소하고 실제 위치·속도를 고정한다. 필요한 분사/충전은 조준도 고정하며 피격 무적을 주지 않는다. A18의 기존 Lock과 데이터는 유지한다.
- A02: 실제 collider sweep/충돌 법선에 대한 접근 속도로 벽·바닥·천장 원인을 분리한다. 접선 속도는 치명 충돌에 포함하지 않는다. 위험 대시에는 무적을 추가하지 않는다.
- A13: 실제 반동 이동 거리·공중 최고점·착지 속도 기록. 공중 재공격으로 누적 경로를 리셋하지 않는다.
- A14: 시각과 실제 collider를 함께 3배로 확대, 지상 변신/해제는 발바닥 보존. HP 수정 없음.
- A15: 독 분사 중 이동/조준 고정, 이후 시전자 근처에 독. 독 활성 지연은 분사 종료보다 길어 이탈 창을 둔다.
- A19: K로 어디서나 3초 중량화. 대시 취소·상태 중 재대시 금지·공중 즉시 하강·강화 중력, 지상 점프는 허용. 일반 공중 최고점 기록을 공유하여 늦은 발동이 낙하 위험을 지우지 않는다. 착지/만료/사망/장착 때 복구한다.
- A20: phase 대시 동안 지상에서도 실제 하향 가속. 종료 후 즉시 실제 중첩/낙사 처리하며 안전 지점으로 이동시키지 않는다. phase linger 없음.

## 생명·보스·UI·분석

- 매 생명 lifeId 증가, 20종을 한 번씩 셔플한 5세트의 총 100장. 한 생명 하나의 런타임만 존재한다. 사망 후 보스 HP/페이즈는 보존하고 진행 중 보스 효과는 정리한다.
- 사망/재시작/디버그 재장착은 효과·예약·가드·가짜 가드 정책·취약성·체력 수정·고정·대시·거대화·중량화·phase를 정리한다. 정적 이벤트/팩토리/테이블은 SubsystemRegistration 초기화를 유지한다.
- Player.prefab: Eyes/Aim Pivot/Aim Marker, Guard Shield, Weapon Pivot/Swing Blade, Charge Pose. ArenaPOC: Main Camera 참조와 Player Status HUD. ArenaSceneBuilder는 동일 구조를 Editor에서 저작한다.
- PanelBase/View/Presenter로 HUD/킬로그/승패를 표시한다. 플레이어 체력/취약성/현재 자세는 Presentation이 포맷한다. 시각 변화 자체는 분석 로그로 매 프레임 남기지 않는다.
- GameEvents→TelemetryRecorder→TelemetryPayload/TelemetryEvents→TelemetryLog 경로. player_health는 실제 피해/최초 취약성 활성만, life_end는 사망만, 최종 결과는 arena_result summary. GameTime/sessionId/sequence는 TelemetryLog가 소유한다.

## 검증 범위

- 이번 개정은 Unity 컴파일과 Edit-mode 저작/직렬화 참조 확인만 수행한다. Play·테스트·주입 입력·캡처·진단 플레이 실험은 사용자 요청 없이 실행하지 않는다.
- 구형 ArenaVerification 진입은 폐기 규칙에 의한 오판을 막기 위해 차단했다. Debug 강제 사망은 높은 체력에도 확정 사망이며 ForceAbility는 모든 새 상태를 정리한다.
- `ba7b72a`의 기존 성공/사용자 확인과 마우스 조준의 컴파일 증거를 이번 능력 개정의 플레이 성공으로 해석하지 않는다. 수동 확인 항목은 POC_HANDOFF.md, 현재 20종은 POC_STATUS.md.

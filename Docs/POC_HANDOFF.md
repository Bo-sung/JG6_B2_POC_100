# POC 인수인계

- 작업 경로 `C:/Gits/JG6_B2_POC_100`, `feature/mouse-aim`, Unity 6000.6.8f1/URP 17.6.0. HEAD/master/origin/master는 ba7b72a 유지, 마우스 조준과 가드/능력 개정은 미커밋이다.
- ArenaPOC를 열고 Play하면 된다. Editor 시작 씬은 ArenaPOC이며 빌드는 Preload→Title→ArenaPOC. 사용자만 플레이 검증한다.
- A/D 이동, Space 점프, 공중 S 기본 강하, 마우스 조준, Shift 조준 대시, J/LMB 평타, Q 유지 전방 가드, K/RMB 단일 스킬. A05/A09/A10/A11은 공통 Q 훅, A12는 높은 체력/강한 평타와 K를 병용, A19는 공중 S가 아니라 어디서나 K.
- 기본 가드는 전방 경량 물리(B01), 중량(B02~B04)/마법(B05~B07)은 실패. A09는 가짜 방어, A11은 마법만 방어. 패리는 이번에 없다.
- Player.prefab: Eyes/Aim Pivot/Aim Marker 보존, Guard Shield/Weapon Pivot/Swing Blade/Charge Pose 추가. ArenaPOC의 aimCamera와 새 Player Status 텍스트 연결. 빌더에 동일 저작 경로가 있다. 효과는 기존 160개 슬롯 재사용.
- 실제 피해는 HitContext의 출처·입사 방향·피해 유형·가드 가능 여부·확정 사망과 공통 DamageResolver를 거친다. A12 비치명 피해는 HP만 감소하며 다음 생명/Death를 만들지 않는다. Q 사용 후 취약성은 남은 생명 동안 유지한다.
- lifeId·효과 정리·보스 우선 해결 유지. 사망/디버그 재장착에서 가드·체력 수정·취약성·거대화·고정·중량화·유체화와 효과를 비운다. A16 자폭은 체력과 관계없이 보스 정산 뒤 사망한다.
- 데이터 원본은 ArenaTableData.json, 클래스는 GameTables/GameTableRecords, 조회 TableManager, 검증 GameTableRules. 숫자를 프리팹/씬 기본값으로 복제하지 않는다.
- Tools/POC/Build Arena Scene은 전체 재저작, Author Guard Revision Views는 저장된 ArenaPOC의 해당 뷰만 갱신한다. Debug Arena의 강제 사망은 확정 사망이다. 이전 ArenaVerification은 1HP/K 가드/공중 S 규칙이므로 진입을 차단했다. 새 자동 플레이 시나리오는 사용자 요청 때 별도로 작성한다.
- 이번 버전은 컴파일과 Edit-mode 저작/참조만 확인. 이전 성공/사용자 수용은 ba7b72a 기준, 마우스 조준 자체도 컴파일 후 미플레이였으며 이번 개정은 아직 사용자 수용 전이다.

사용자 플레이 확인 항목:

- Q 전방/배후, B01과 중량/마법 구분, A09 가짜 방패, A05 반격 반동, A10 남은 액체의 실접촉 귀환, A11 물리 실패/전방 마법 성공.
- A12 여러 보통 피격 생존, Q 성공 방어는 피해 없음, Q 사용 이후 첫 미방어 피해 즉사와 다음 생명 초기화, K 충전 찌르기.
- A01 실제 검과 4초 월드 궤적 일치/재접촉 자해, A02 상하좌우·대각 충돌, A03 몸에서 광선, A04 지상/실제 발판 칼날.
- A06 긴 제자리 충전과 피격 노출, A07 정상 보스 방향 발사와 실제 자기 교차 위험, A08 점프 낙뢰 유지, A13 누적 반동/높은 낙하.
- A14 발바닥 유지/천장 위험/체력 1, A15 분사 고정/독 이탈 시간, A16 동시 승리, A17 3초 고정, A18 기존 유지, A19 지상·공중·대시 발동과 착지/만료 복구, A20 지상 침강/상향 보완/실제 매몰.
- 사망·재시작·Debug 장착의 상태 정리와 보스 HP/페이즈 영속, 긴 전투·완주·조작감·독립 빌드. Unity 6.3 실행은 미검증이다.

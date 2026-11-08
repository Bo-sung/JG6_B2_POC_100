using System;

// 순수 직렬화 항목. 수치는 JSON에 두고 이 파일에는 필드만 둔다.
// 검증 규칙이나 조회 로직을 여기 넣지 않는다(GameTableRules / TableManager).

[Serializable] public sealed class PlayerData
{
    public float maxHealth;
    public float moveSpeed;
    public int dashCharges;
    public float dashCooldown;
    public float interactRange;
}

// 해금(확장) 효과 한 항목. id로 찾고 nextUpgrade로 다음 해금을 잇는다.
// category는 Presentation이 색·제목의 기준으로 읽는 키이며 EffectCategory 이름과 일치해야 한다.
[Serializable] public sealed class UpgradeData
{
    public string id, nextUpgrade, displayName, description, category;
}

// 해금 사슬의 시작. 배열 순서에 의존하지 않기 위한 명시적 시작 키.
[Serializable] public sealed class UpgradeSequenceData
{
    public string firstUpgrade;
}

// 씬 진행 경로 한 행. 씬 이름이 키이며 배열 순서는 의미하지 않는다.
// nextScene이 비어 있으면 최종 씬, restartScene은 사망·재시작 때 로드하는 씬.
[Serializable] public sealed class SceneLinkData
{
    public string scene, nextScene, restartScene;
}

[Serializable]
public sealed class ArenaPlayerData
{
    public float speed, acceleration, jumpSpeed, gravity, dashSpeed, dashDuration, dashCooldown;
    public float attackDamage, attackRange, attackHeight, attackDuration, attackCooldown;
    public float coyoteTime, jumpBuffer, fallSpeed, wallDeathSpeed, groundProbe;
}

[Serializable]
public sealed class ArenaRulesData
{
    public int sets, seed, feedRows;
    public float bossHealth, phaseThreshold, respawnDelay, spawnSafety, feedDuration;
    public float floorY, deathY, leftWall, rightWall, bossRestX, bossRestY;
    public float bossPause, effectWarningAlpha, effectActiveAlpha, hitFlash;
    public string bossName, title, controls, victory, defeat;
}

[Serializable]
public sealed class AbilityData
{
    public string id, displayName, description, input;
    public bool active;
    public float damage, range, height, delay, duration, cooldown, speed, force, extra;
    public float selfDelay, selfDuration, selfWidth, selfHeight, spawnOffset;
    public float recoilDuration, recoilLift, spacing, impactDuration;
    public int emissionCount;
}

[Serializable]
public sealed class BossPatternData
{
    public string id, displayName, hint, followup;
    public int phase;
    public float warning, duration, recovery, width, height, speed, interval;
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 검증과 파생 값 계산의 공통 구현. DTO와 로더에는 게임 규칙을 넣지 않는다.
// 규칙을 추가할 때: Check로 항목별 오류를 모은다(첫 오류에서 멈추지 않는다) → IsValid로 보고 → 사용처는 RequireValid로 차단.
public sealed class GameTableRules
{
    public bool IsValid { get; private set; } = true;

    void Check(bool _condition, string _path, string _rule)
    {
        if (_condition)
        {
            return;
        }
        IsValid = false;
        Debug.LogError($"테이블 오류 [{_path}]: {_rule}");
    }

    // 키 인덱스 구성. 빈 키·중복 키·null 항목을 항목별로 보고한다.
    public Dictionary<string, T> Index<T>(T[] _entries, Func<T, string> _key, string _path) where T : class
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        Check(_entries != null, _path, "목록이 필요합니다.");
        foreach (var entry in _entries ?? Array.Empty<T>())
        {
            string id = entry == null ? null : _key(entry);
            Check(!string.IsNullOrEmpty(id), _path, "항목과 비어 있지 않은 키가 필요합니다.");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }
            Check(result.TryAdd(id, entry), _path + "." + id, "키가 중복되었습니다.");
        }
        return result;
    }

    // 모든 숫자의 유한성과 누락된 중첩 설정을 재귀로 검사한다. DTO에 새 클래스를 추가하면 자동으로 덮인다.
    void Inspect(object _value, string _path)
    {
        if (_value is string)
        {
            return;
        }
        if (_value == null)
        {
            Check(false, _path, "값이 누락되었습니다.");
            return;
        }
        if (_value is float number)
        {
            Check(!float.IsNaN(number) && !float.IsInfinity(number), _path, "유한한 숫자여야 합니다.");
            return;
        }
        var type = _value.GetType();
        if (type.IsPrimitive || type.IsEnum)
        {
            return;
        }
        if (_value is IEnumerable array)
        {
            int i = 0;
            foreach (var item in array)
            {
                Inspect(item, _path + "[" + i++ + "]");
            }
            return;
        }
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            // 문자열은 선택 필드일 수 있다. 필수 키와 참조는 Reference에서 별도 검사한다.
            if (field.FieldType == typeof(string))
            {
                continue;
            }
            Inspect(field.GetValue(_value), _path + "." + field.Name);
        }
    }

    void Positive(float _value, string _path)
    {
        Check(_value > 0, _path, "0보다 커야 합니다.");
    }
    void Nonnegative(float _value, string _path)
    {
        Check(_value >= 0, _path, "0 이상이어야 합니다.");
    }
    void Fraction(float _value, string _path)
    {
        Check(_value >= 0 && _value <= 1, _path, "0~1이어야 합니다.");
    }
    void IntRange(int _value, int _min, int _max, string _path)
    {
        Check(_value >= _min && _value <= _max, _path, $"{_min}~{_max} 범위여야 합니다.");
    }

    // 다른 테이블·같은 테이블의 키 참조. 빈 값이 허용되면 allowEmpty로 표현한다.
    void Reference(string _id, string _path, bool _resolved, string _label)
    {
        Reference(_id, _path, _resolved, _label, false);
    }

    void Reference(string _id, string _path, bool _resolved, string _label, bool _allowEmpty)
    {
        if (string.IsNullOrEmpty(_id))
        {
            Check(_allowEmpty, _path, $"{_label} 키가 필요합니다.");
            return;
        }
        Check(_resolved, _path, $"{_label} '{_id}'가 테이블에 없습니다.");
    }

    // 강화 단계처럼 1~maxLevel 항목이 하나씩 있어야 하는 목록 검사.
    public void ValidateLevelSet<T>(T[] _entries, Func<T, int> _getLevel, int _maxLevel, string _path) where T : class
    {
        Check(_entries != null && _entries.Length == _maxLevel, _path, $"단계 1~{_maxLevel} 항목이 모두 필요합니다.");
        if (_entries == null || _maxLevel <= 0)
        {
            return;
        }
        var seen = new HashSet<int>();
        foreach (var entry in _entries)
        {
            if (entry == null)
            {
                continue;
            }
            int level = _getLevel(entry);
            IntRange(level, 1, _maxLevel, $"{_path}.level({level})");
            Check(seen.Add(level), $"{_path}.level({level})", "단계가 중복되었습니다.");
        }
    }

    public void Validate(TableManager _tables)
    {
        Inspect(_tables.Gameplay, nameof(GameplayTableData));
        Inspect(_tables.Flow, nameof(FlowTableData));
        Inspect(_tables.Arena, nameof(ArenaTableData));

        ValidatePlayer(_tables.Gameplay.player);
        ValidateUpgrades(_tables);
        ValidateScenes(_tables);
        ValidateArena(_tables.Arena);
    }

    void ValidateArena(ArenaTableData _arena)
    {
        const string PATH = "ArenaTableData";
        if (_arena == null || _arena.player == null || _arena.rules == null)
        {
            Check(false, PATH, "플레이어와 전투 설정이 필요합니다.");
            return;
        }
        var abilities = Index(_arena.abilities == null ? null : _arena.abilities.ToArray(), _entry => _entry.id, PATH + ".abilities");
        var patterns = Index(_arena.patterns == null ? null : _arena.patterns.ToArray(), _entry => _entry.id, PATH + ".patterns");
        Check(abilities.Count == 20, PATH + ".abilities", "능력 20종이 필요합니다.");
        Check(patterns.Count == 7, PATH + ".patterns", "보스 패턴 7종이 필요합니다.");
        for (int i = 1; i <= 20; i++)
        {
            string id = "A" + i.ToString("00");
            Check(abilities.ContainsKey(id), PATH + ".abilities", id + "가 필요합니다.");
        }
        foreach (var ability in abilities.Values)
        {
            string path = PATH + ".abilities." + ability.id;
            Check(!string.IsNullOrWhiteSpace(ability.displayName), path, "표시 이름이 필요합니다.");
            Check(!string.IsNullOrWhiteSpace(ability.description), path, "결함 설명이 필요합니다.");
            Check(!string.IsNullOrWhiteSpace(ability.input), path, "입력 안내가 필요합니다.");
            Nonnegative(ability.damage, path + ".damage");
            Nonnegative(ability.range, path + ".range");
            Nonnegative(ability.height, path + ".height");
            Nonnegative(ability.delay, path + ".delay");
            Nonnegative(ability.duration, path + ".duration");
            Nonnegative(ability.cooldown, path + ".cooldown");
            Nonnegative(ability.selfDamage, path + ".selfDamage");
            Nonnegative(ability.selfDelay, path + ".selfDelay");
            Nonnegative(ability.selfDuration, path + ".selfDuration");
            Nonnegative(ability.selfWidth, path + ".selfWidth");
            Nonnegative(ability.selfHeight, path + ".selfHeight");
            Nonnegative(ability.spawnOffset, path + ".spawnOffset");
            Nonnegative(ability.recoilDuration, path + ".recoilDuration");
            Nonnegative(ability.recoilLift, path + ".recoilLift");
            Nonnegative(ability.spacing, path + ".spacing");
            Nonnegative(ability.impactDuration, path + ".impactDuration");
            IntRange(ability.emissionCount, 0, 160, path + ".emissionCount");
            if (ability.id != "A11" && ability.id != "A14" && ability.id != "A20")
            {
                Positive(ability.range, path + ".range");
                Positive(ability.height, path + ".height");
            }
            if (ability.id != "A05" && ability.id != "A11" && ability.id != "A12" && ability.id != "A20")
            {
                Positive(ability.duration, path + ".duration");
            }
            if (ability.id == "A01" || ability.id == "A07")
            {
                Positive(ability.spawnOffset, path + ".spawnOffset");
            }
            if (ability.id == "A15")
            {
                Positive(ability.selfDelay, path + ".selfDelay");
                Positive(ability.selfDuration, path + ".selfDuration");
                Positive(ability.selfWidth, path + ".selfWidth");
                Positive(ability.selfHeight, path + ".selfHeight");
            }
            if (ability.id == "A04")
            {
                Positive(ability.spacing, path + ".spacing");
                Check(ability.emissionCount > 0 && ability.emissionCount % 2 == 1, path + ".emissionCount", "자기 위치를 포함하는 홀수 개 칼날이 필요합니다.");
            }
            if (ability.id == "A05")
            {
                Positive(ability.recoilDuration, path + ".recoilDuration");
            }
            if (ability.id == "A05" || ability.id == "A19")
            {
                Positive(ability.impactDuration, path + ".impactDuration");
            }
            if (ability.id == "A19")
            {
                Positive(ability.extra, path + ".extra");
            }
            if (ability.id == "A17")
            {
                Positive(ability.force, path + ".force");
            }
            if (ability.id == "A02")
            {
                Check(ability.speed > _arena.player.wallDeathSpeed, path + ".speed", "벽 충돌 위험이 생기는 속도여야 합니다.");
            }
            foreach (string fieldName in new[] { "maxHealth", "skillDamage", "skillRange", "skillHeight", "skillCharge", "skillDuration",
                "swingDuration", "swingArc", "traceWidth", "surfaceReach", "travelLimit", "fatalFallHeight", "fatalImpactSpeed", "gravityMultiplier", "sinkAcceleration" })
            {
                Nonnegative((float)typeof(AbilityData).GetField(fieldName).GetValue(ability), path + "." + fieldName);
            }
            if (ability.id == "A01")
            {
                Check(ability.duration == 4, path + ".duration", "검 궤적은 4초 남습니다.");
                Positive(ability.swingDuration, path + ".swingDuration");
                Check(ability.swingArc > 0 && ability.swingArc <= 180, path + ".swingArc", "전방 휘두르기 각도가 필요합니다.");
                Positive(ability.traceWidth, path + ".traceWidth");
                Check(ability.emissionCount >= 8 && ability.emissionCount <= 24, path + ".emissionCount", "연속 궤적과 풀 예산을 위한 8~24개 구간이 필요합니다.");
                Check(ability.cooldown > ability.swingDuration, path + ".cooldown", "휘두르기 중 재시전할 수 없습니다.");
                Check(ability.cooldown > 0 && (Mathf.Ceil(ability.duration / ability.cooldown) + 1) * ability.emissionCount <= 100,
                    path, "지속 궤적은 160개 공용 풀 중 최대 100개를 사용해야 합니다.");
            }
            if (ability.id == "A04")
            {
                Positive(ability.surfaceReach, path + ".surfaceReach");
                Check(ability.emissionCount <= 24, path + ".emissionCount", "칼날 풀 예산은 최대 24개입니다.");
            }
            if (ability.id == "A06")
            {
                Check(ability.delay >= 3 && ability.damage < _arena.player.attackDamage * 2, path, "긴 충전과 작은 보상을 유지해야 합니다.");
            }
            if (ability.id == "A10")
            {
                Positive(ability.speed, path + ".speed");
                Positive(ability.spawnOffset, path + ".spawnOffset");
            }
            if (ability.id == "A12")
            {
                Check(ability.maxHealth >= 4, path + ".maxHealth", "여러 일반 피격을 견딜 체력이 필요합니다.");
                Positive(ability.skillDamage, path + ".skillDamage");
                Positive(ability.skillRange, path + ".skillRange");
                Positive(ability.skillHeight, path + ".skillHeight");
                Positive(ability.skillCharge, path + ".skillCharge");
                Positive(ability.skillDuration, path + ".skillDuration");
            }
            if (ability.id == "A13")
            {
                Positive(ability.travelLimit, path + ".travelLimit");
                Positive(ability.fatalFallHeight, path + ".fatalFallHeight");
                Positive(ability.fatalImpactSpeed, path + ".fatalImpactSpeed");
            }
            if (ability.id == "A15")
            {
                Check(ability.selfDelay > ability.duration, path + ".selfDelay", "독 분사 고정이 끝난 뒤 이탈 시간이 필요합니다.");
            }
            if (ability.id == "A19")
            {
                Positive(ability.gravityMultiplier, path + ".gravityMultiplier");
                Positive(ability.speed, path + ".speed");
                Positive(ability.fatalImpactSpeed, path + ".fatalImpactSpeed");
            }
            if (ability.id == "A20")
            {
                Positive(ability.sinkAcceleration, path + ".sinkAcceleration");
            }
            bool passive = ability.id == "A01" || ability.id == "A02" || ability.id == "A08" || ability.id == "A09" ||
                ability.id == "A05" || ability.id == "A10" || ability.id == "A11" || ability.id == "A13" || ability.id == "A20";
            Check(ability.active != passive, path + ".active", "active는 K 스킬 제공 여부와 일치해야 합니다. 패시브 훅과 병용 가능합니다.");
        }
        for (int i = 1; i <= 7; i++)
        {
            string id = "B" + i.ToString("00");
            if (!patterns.TryGetValue(id, out var pattern))
            {
                Check(false, PATH + ".patterns", id + "가 필요합니다.");
                continue;
            }
            string path = PATH + ".patterns." + id;
            Check(pattern.phase == (i <= 5 ? 1 : 2), path, "B01~B05는 1페이즈부터, B06~B07는 2페이즈부터 사용합니다.");
            Check(!string.IsNullOrWhiteSpace(pattern.displayName) && !string.IsNullOrWhiteSpace(pattern.hint), path, "패턴 이름과 회피 안내가 필요합니다.");
            Check(pattern.damageType == "Physical" || pattern.damageType == "Magic", path + ".damageType", "물리/마법 피해 유형이 필요합니다.");
            Positive(pattern.damage, path + ".damage");
            Check(!float.IsNaN(pattern.incomingX) && !float.IsInfinity(pattern.incomingX) && !float.IsNaN(pattern.incomingY)
                && !float.IsInfinity(pattern.incomingY), path, "유효한 입사 방향이 필요합니다.");
            Check(pattern.guardable == (id == "B01" || id == "B05" || id == "B06" || id == "B07"), path + ".guardable", "경량 물리와 마법만 가드 대상입니다.");
            Check(pattern.damageType == (i >= 5 ? "Magic" : "Physical"), path + ".damageType", "패턴의 실제 타격 유형과 일치해야 합니다.");
            Positive(pattern.warning, path + ".warning");
            Positive(pattern.duration, path + ".duration");
            Positive(pattern.recovery, path + ".recovery");
            Positive(pattern.width, path + ".width");
            Positive(pattern.height, path + ".height");
            Positive(pattern.interval, path + ".interval");
            Nonnegative(pattern.speed, path + ".speed");
            Reference(pattern.followup, path + ".followup", !string.IsNullOrEmpty(pattern.followup) && patterns.ContainsKey(pattern.followup), "연계 패턴", true);
            Check(pattern.followup != pattern.id, path + ".followup", "자기 자신을 연계할 수 없습니다.");
        }
        foreach (var field in typeof(ArenaPlayerData).GetFields())
        {
            Positive((float)field.GetValue(_arena.player), PATH + ".player." + field.Name);
        }
        Check(_arena.player.maxHealth == 1, PATH + ".player.maxHealth", "기본 체력은 1이며 A12만 명시적으로 높입니다.");
        Check(_arena.player.guardHalfAngle < 90, PATH + ".player.guardHalfAngle", "가드는 전방으로 제한됩니다.");
        var rules = _arena.rules;
        Check(rules.sets == 5 && rules.feedRows == 5, PATH + ".rules", "5세트 100목숨과 킬로그 5줄이 필요합니다.");
        Positive(rules.bossHealth, PATH + ".rules.bossHealth");
        Check(rules.phaseThreshold > 0 && rules.phaseThreshold < 1, PATH + ".rules.phaseThreshold", "0~1 사이여야 합니다.");
        Check(rules.respawnDelay >= 0.5f && rules.respawnDelay <= 1f, PATH + ".rules.respawnDelay", "0.5~1초여야 합니다.");
        Positive(rules.spawnSafety, PATH + ".rules.spawnSafety");
        Positive(rules.feedDuration, PATH + ".rules.feedDuration");
        Positive(rules.bossPause, PATH + ".rules.bossPause");
        Positive(rules.hitFlash, PATH + ".rules.hitFlash");
        Fraction(rules.effectWarningAlpha, PATH + ".rules.effectWarningAlpha");
        Fraction(rules.effectActiveAlpha, PATH + ".rules.effectActiveAlpha");
        Check(rules.leftWall < rules.rightWall && rules.deathY < rules.floorY, PATH + ".rules", "아레나 경계가 올바르지 않습니다.");
        Check(!string.IsNullOrWhiteSpace(rules.bossName) && !string.IsNullOrWhiteSpace(rules.controls) &&
            !string.IsNullOrWhiteSpace(rules.title) && !string.IsNullOrWhiteSpace(rules.victory) && !string.IsNullOrWhiteSpace(rules.defeat), PATH + ".rules", "전투 표기 문자열이 필요합니다.");
    }

    void ValidatePlayer(PlayerData _player)
    {
        if (_player == null)
        {
            Check(false, "Gameplay.player", "플레이어 설정이 필요합니다.");
            return;
        }
        Positive(_player.maxHealth, "Gameplay.player.maxHealth");
        Positive(_player.moveSpeed, "Gameplay.player.moveSpeed");
        IntRange(_player.dashCharges, 0, 10, "Gameplay.player.dashCharges");
        Nonnegative(_player.dashCooldown, "Gameplay.player.dashCooldown");
        Positive(_player.interactRange, "Gameplay.player.interactRange");
    }

    void ValidateUpgrades(TableManager _tables)
    {
        var list = _tables.Gameplay.upgrades ?? Array.Empty<UpgradeData>();
        Check(list.Length > 0, "GameplayTableData.upgrades", "해금 효과 목록이 비어 있습니다.");
        foreach (var upgrade in list)
        {
            if (upgrade == null || string.IsNullOrEmpty(upgrade.id))
            {
                continue;
            }
            string path = "Gameplay.upgrades." + upgrade.id;
            Check(!string.IsNullOrEmpty(upgrade.displayName) || !string.IsNullOrEmpty(upgrade.id), path, "표시 이름이 필요합니다.");
            Check(!string.IsNullOrEmpty(upgrade.description), path + ".description", "설명이 비어 있으면 테이블 오류다(표기가 단일 출처이므로 코드에 폴백을 두지 않는다).");
            Check(TryCategory(upgrade.category, out _), path + ".category", $"EffectCategory 이름이어야 합니다: {upgrade.category}");
            Reference(upgrade.nextUpgrade, path + ".nextUpgrade", _tables.TryGetUpgrade(upgrade.nextUpgrade, out _), "다음 해금", _allowEmpty: true);
            Check(!string.Equals(upgrade.nextUpgrade, upgrade.id, StringComparison.Ordinal), path + ".nextUpgrade", "자기 자신을 다음 해금으로 연결했습니다.");
        }
        Reference(_tables.FirstUpgradeId, "Gameplay.sequence.firstUpgrade", _tables.TryGetUpgrade(_tables.FirstUpgradeId, out _), "시작 해금");

        // 사슬이 순환하면 다음 해금을 영원히 계산하지 못한다.
        var walked = new HashSet<string>(StringComparer.Ordinal);
        for (string id = _tables.FirstUpgradeId; !string.IsNullOrEmpty(id);)
        {
            if (!walked.Add(id))
            {
                Check(false, "Gameplay.upgrades." + id + ".nextUpgrade", "해금 사슬에 순환이 있습니다.");
                break;
            }
            if (!_tables.TryGetUpgrade(id, out var node))
            {
                break;
            }
            id = node.nextUpgrade;
        }
    }

    void ValidateScenes(TableManager _tables)
    {
        var list = _tables.Flow.scenes ?? Array.Empty<SceneLinkData>();
        Check(list.Length > 0, "FlowTableData.scenes", "씬 진행 목록이 비어 있습니다.");
        foreach (var link in list)
        {
            if (link == null || string.IsNullOrEmpty(link.scene))
            {
                continue;
            }
            string path = "Flow.scenes." + link.scene;
            Reference(link.nextScene, path + ".nextScene", _tables.TryGetScene(link.nextScene, out _), "다음 씬", _allowEmpty: true);
            Reference(link.restartScene, path + ".restartScene", _tables.TryGetScene(link.restartScene, out _), "재시작 씬", _allowEmpty: true);
            Check(!string.Equals(link.nextScene, link.scene, StringComparison.Ordinal), path + ".nextScene", "자기 자신을 다음 씬으로 연결했습니다.");
        }
    }

    // 문자열 키를 열거형으로 해석한다. 실패하면 false(호출 측이 Check로 보고한다).
    public static bool TryCategory(string _key, out EffectCategory _category)
    {
        _category = EffectCategory.Utility;
        return !string.IsNullOrEmpty(_key) && Enum.TryParse(_key, out _category);
    }

    public static EffectCategory Category(string _key)
    {
        return TryCategory(_key, out var category) ? category : EffectCategory.Utility;
    }
}

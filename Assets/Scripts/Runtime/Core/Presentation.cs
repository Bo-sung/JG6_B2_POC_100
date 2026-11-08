using UnityEngine;

// 색과 설명 문자열의 단일 출처.
// 선택지 카드·정보 패널·슬롯 핍·장식 오브젝트가 모두 여기를 읽고, 개별 UI가 자기 색과 문구를 가지지 않는다.
// 설명 원문은 테이블의 description이 원본이고, 이 파일은 정렬·강조 같은 표기만 덧붙인다.
public enum EffectCategory
{
    Offense, Mobility, Defense, Utility, BaseStat
}

public static class Presentation
{
    public const string RESTART = "다시 도전";
    public const string START = "100명 출전";
    public static Color ArenaBackground => new Color(0.055f, 0.065f, 0.105f);
    public static Color ArenaGround => new Color(0.25f, 0.29f, 0.37f);
    public static Color ArenaPlatform => new Color(0.33f, 0.40f, 0.50f);
    public static Color ArenaInk => new Color(0.94f, 0.95f, 0.99f);
    public static Color ArenaPanel => new Color(0.08f, 0.10f, 0.16f, 0.96f);
    public static Color ArenaAccent => new Color(0.99f, 0.78f, 0.27f);

    public static Color HazardColor(int _lifeId, bool _warning)
    {
        Color value = _lifeId == 0 ? new Color(1f, 0.25f, 0.30f) : new Color(0.30f, 0.92f, 1f);
        var rules = TableManager.Instance.Arena.rules;
        value.a = _warning ? rules.effectWarningAlpha : rules.effectActiveAlpha;
        return value;
    }

    public static Color PlayerColor(bool _phasing, bool _safe, bool _locked)
    {
        if (_phasing)
        {
            return new Color(0.40f, 0.85f, 1f, 0.45f);
        }
        if (_safe)
        {
            return ArenaAccent;
        }
        return _locked ? new Color(0.75f, 0.60f, 0.95f) : ArenaInk;
    }

    public static Color BossColor(int _phase, bool _attacking, bool _recovering)
    {
        return _recovering ? new Color(0.50f, 0.48f, 0.65f) : _attacking
            ? new Color(1f, 0.28f, 0.22f) : _phase == 2 ? new Color(0.78f, 0.28f, 0.67f) : new Color(0.62f, 0.36f, 0.48f);
    }

    public static string Ability(AbilityData _data)
    {
        return $"<b>{_data.id} · {_data.displayName}</b>\n{_data.description}\n{Dim(_data.input)}";
    }

    public static string BossHp(float _hp, float _maxHp, int _phase)
    {
        return $"{TableManager.Instance.Arena.rules.bossName}   {_hp:0}/{_maxHp:0}   ·   PHASE {_phase}";
    }

    public static string Lives(int _remaining, int _total)
    {
        return $"LIVES  {_remaining:000} / {_total}";
    }

    public static string Cause(string _source)
    {
        var data = TableManager.Instance.Arena;
        var ability = data.abilities.Find(_entry => _entry.id == _source);
        if (ability != null)
        {
            return ability.displayName;
        }
        var pattern = data.patterns.Find(_entry => _entry.id == _source);
        if (pattern != null)
        {
            return data.rules.bossName + " · " + pattern.displayName;
        }
        return _source switch
        {
            "fall" => "낙사", "wall" => "벽", "ceiling" => "천장", "debug" => "개발용 사망", _ => _source
        };
    }

    public static string KillFeed(GameEvents.DeathInfo _info)
    {
        return $"{Cause(_info.source)}  ×  용사 #{_info.lifeId:000}";
    }

    public static string Result(GameEvents.BattleEndInfo _info)
    {
        var data = TableManager.Instance.Arena.rules;
        return $"<size=150%><b>{(_info.victory ? data.victory : data.defeat)}</b></size>\n\n" +
            $"희생한 용사  {_info.livesUsed} / {_info.total}\n남은 보스 HP  {_info.bossHpRemaining:0}\n" +
            $"누적 피해  {data.bossHealth - _info.bossHpRemaining:0}";
    }

    // 범주 팔레트. 새 범주를 추가하면 여기 색과 Title을 함께 추가한다.
    public static Color Color(EffectCategory _category)
    {
        return _category switch
        {
            EffectCategory.Offense => new Color(0.90f, 0.33f, 0.29f),
            EffectCategory.Mobility => new Color(0.33f, 0.58f, 0.95f),
            EffectCategory.Defense => new Color(0.35f, 0.76f, 0.44f),
            EffectCategory.BaseStat => new Color(0.96f, 0.78f, 0.27f),
            _ => new Color(0.72f, 0.74f, 0.80f),
        };
    }

    public static string Title(EffectCategory _category)
    {
        return _category switch
        {
            EffectCategory.Offense => "공격",
            EffectCategory.Mobility => "이동",
            EffectCategory.Defense => "방어",
            EffectCategory.BaseStat => "기본 능력치",
            _ => "유틸",
        };
    }

    public static EffectCategory Category(UpgradeData _upgrade)
    {
        return GameTableRules.Category(_upgrade == null ? null : _upgrade.category);
    }
    public static Color Color(UpgradeData _upgrade)
    {
        return Color(Category(_upgrade));
    }
    public static string Title(UpgradeData _upgrade)
    {
        return Title(Category(_upgrade));
    }

    public static string Name(UpgradeData _upgrade)
    {
        return _upgrade == null ? "비어 있음" : string.IsNullOrEmpty(_upgrade.displayName) ? _upgrade.id : _upgrade.displayName;
    }

    // 설명은 테이블 원본. 비어 있으면 GameTableRules가 테이블 오류로 보고하므로 여기서 폴백 문구를 두지 않는다.
    public static string Describe(UpgradeData _upgrade)
    {
        return _upgrade == null ? string.Empty : _upgrade.description ?? string.Empty;
    }

    // 같은 색에서 채도·명도만 낮춰 '기존 보유/약화' 표현을 만든다(팔레트를 새로 만들지 않는다).
    public static Color Shade(Color _color, float _saturation, float _value)
    {
        UnityEngine.Color.RGBToHSV(_color, out float hue, out float sat, out float val);
        return UnityEngine.Color.HSVToRGB(hue, sat * _saturation, val * _value);
    }

    // ── 리치 텍스트 표기 ──
    // 값 강조·Dim·제목 서식을 카드와 패널이 같이 쓴다. 서식 문자열을 개별 UI에 복사하지 않는다.
    const string VALUE_TAG = "#FFE08A";
    const string DIM_TAG = "#AEB6C2";
    public static string Accent(string _value)
    {
        return $"<color={VALUE_TAG}>{_value}</color>";
    }
    public static string Dim(string _value)
    {
        return $"<color={DIM_TAG}>{_value}</color>";
    }
    public static string Heading(string _title, Color _color)
    {
        return $"<size=135%><b><color={Hex(_color)}>{_title}</color></b></size>";
    }
    public static string Hex(Color _color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(_color);
    }

    // 테이블 항목 하나의 카드 본문(이름·범주·설명). 카드와 목록이 같은 출력을 쓴다.
    public static string Card(UpgradeData _upgrade)
    {
        if (_upgrade == null)
        {
            return Dim("유효하지 않은 항목");
        }
        return $"{Heading(Name(_upgrade), Color(_upgrade))}\n{Dim(Title(_upgrade))}\n{Describe(_upgrade)}";
    }
}

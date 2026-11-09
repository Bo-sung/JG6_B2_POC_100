using TMPro;
using UnityEngine;

public sealed class ArenaHudView : PanelBase
{
    public TMP_Text bossLabel, livesLabel, abilityLabel, controlsLabel, patternLabel, playerStatusLabel;
    public UnityEngine.UI.Image hpFill;
    public TMP_Text[] feedLabels;

    public void ShowHealth(GameEvents.BossHpInfo _info)
    {
        bossLabel.text = Presentation.BossHp(_info.hp, _info.maxHp, _info.phase);
        hpFill.fillAmount = _info.maxHp > 0 ? _info.hp / _info.maxHp : 0;
    }

    public void ShowLife(GameEvents.LifeInfo _info)
    {
        livesLabel.text = Presentation.Lives(_info.remaining, _info.total);
        abilityLabel.text = Presentation.Ability(TableManager.Instance.Arena.abilities.Find(_entry => _entry.id == _info.abilityId));
    }

    public void ShowFeed(int _index, string _text, float _alpha)
    {
        feedLabels[_index].text = _text;
        feedLabels[_index].alpha = _alpha;
    }
}

using UnityEngine;

// Game models own the facts; this presenter updates only scene-authored views/slots.
public sealed class ArenaHudPresenter : MonoBehaviour
{
    public ArenaSession session;
    public ArenaHudView hud;
    public ArenaResultView result;
    string[] lines;
    double[] expiry;

    void Awake()
    {
        int count = TableManager.Instance.Arena.rules.feedRows;
        lines = new string[count];
        expiry = new double[count];
    }

    void OnEnable()
    {
        GameEvents.LifeChanged += OnLife;
        GameEvents.BossHpChanged += OnHealth;
        GameEvents.Death += OnDeath;
        GameEvents.BattleEnded += OnEnded;
        result.restart.onClick.AddListener(session.Restart);
    }

    void OnDisable()
    {
        GameEvents.LifeChanged -= OnLife;
        GameEvents.BossHpChanged -= OnHealth;
        GameEvents.Death -= OnDeath;
        GameEvents.BattleEnded -= OnEnded;
        result.restart.onClick.RemoveListener(session.Restart);
    }

    void Start()
    {
        hud.controlsLabel.text = session.Data.rules.controls;
    }

    void OnLife(GameEvents.LifeInfo _info)
    {
        hud.ShowLife(_info);
    }

    void OnHealth(GameEvents.BossHpInfo _info)
    {
        hud.ShowHealth(_info);
    }

    void OnDeath(GameEvents.DeathInfo _info)
    {
        for (int i = lines.Length - 1; i > 0; i--)
        {
            lines[i] = lines[i - 1];
            expiry[i] = expiry[i - 1];
        }
        lines[0] = Presentation.KillFeed(_info);
        expiry[0] = TelemetryLog.GameTime + session.Data.rules.feedDuration;
    }

    void OnEnded(GameEvents.BattleEndInfo _info)
    {
        result.ShowResult(_info);
    }

    void Update()
    {
        if (lines == null)
        {
            return;
        }
        for (int i = 0; i < lines.Length; i++)
        {
            float fraction = (float)((expiry[i] - TelemetryLog.GameTime) / session.Data.rules.feedDuration);
            hud.ShowFeed(i, lines[i] ?? string.Empty, Mathf.Clamp01(fraction));
        }
        string patternId = session.boss.PatternId;
        var pattern = session.Data.patterns.Find(_entry => _entry.id == patternId);
        hud.patternLabel.text = pattern == null ? string.Empty : pattern.displayName + " · " + pattern.hint;
    }
}

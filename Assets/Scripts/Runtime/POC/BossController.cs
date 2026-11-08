using System;
using System.Linq;
using UnityEngine;

public sealed class BossController : MonoBehaviour
{
    public ArenaSession session;
    public BoxCollider2D hurtbox;
    public SpriteRenderer visual;
    public float Health { get; private set; }
    public int Phase => Health <= session.Data.rules.bossHealth * session.Data.rules.phaseThreshold ? 2 : 1;
    public string PatternId => pattern == null ? string.Empty : pattern.Data.id;
    public bool Attacking => pattern != null && pattern.Age >= pattern.Data.warning && pattern.Age < pattern.Data.warning + pattern.Data.duration;
    public bool Recovering => pattern != null && pattern.Age >= pattern.Data.warning + pattern.Data.duration;
    BossPatternRuntime pattern;
    float pause;
    int patternIndex;
    string queuedPattern;
    bool following;

    public void Initialize()
    {
        Health = session.Data.rules.bossHealth;
        patternIndex = 0;
        ClearAttack();
        PublishHealth();
    }

    public float TakeDamage(float _damage)
    {
        float dealt = Mathf.Min(Health, Mathf.Max(0, _damage));
        Health -= dealt;
        PublishHealth();
        return dealt;
    }

    void PublishHealth()
    {
        GameEvents.RaiseBossHpChanged(new GameEvents.BossHpInfo(Health, session.Data.rules.bossHealth, Phase));
    }

    public void Push(float _distance)
    {
        Vector3 position = transform.position;
        float half = hurtbox.bounds.extents.x;
        position.x = Mathf.Clamp(position.x + _distance, session.Data.rules.leftWall + half, session.Data.rules.rightWall - half);
        transform.position = position;
    }

    public void ClearAttack()
    {
        pattern = null;
        queuedPattern = null;
        following = false;
        pause = session.Data.rules.bossPause;
        session.effects.Clear(0, true);
        transform.position = new Vector3(session.Data.rules.bossRestX, session.Data.rules.bossRestY, 0);
    }

    void FixedUpdate()
    {
        if (!session.life.Alive || session.life.Safe || session.Finished || Health <= 0)
        {
            return;
        }
        float dt = Time.fixedDeltaTime;
        if (pattern == null)
        {
            pause -= dt;
            if (pause > 0)
            {
                return;
            }
            BossPatternData next;
            if (!string.IsNullOrEmpty(queuedPattern))
            {
                next = session.Data.patterns.Find(_entry => _entry.id == queuedPattern);
                queuedPattern = null;
                following = true;
            }
            else
            {
                var choices = session.Data.patterns.Where(_entry => _entry.phase <= Phase).OrderBy(_entry => _entry.id, StringComparer.Ordinal).ToArray();
                next = choices[patternIndex++ % choices.Length];
                following = false;
            }
            pattern = BossPatternRuntime.Create(next, session);
            pattern.Begin();
        }
        pattern.Tick(dt);
        visual.color = Presentation.BossColor(Phase, Attacking, Recovering);
        if (pattern.Age >= pattern.Data.warning + pattern.Data.duration + pattern.Data.recovery)
        {
            queuedPattern = Phase == 2 && !following ? pattern.Data.followup : null;
            pattern = null;
            pause = session.Data.rules.bossPause;
        }
    }

#if UNITY_EDITOR || DEBUG
    public void SetHealth(float _health)
    {
        Health = Mathf.Clamp(_health, 0, session.Data.rules.bossHealth);
        PublishHealth();
    }

    public void ForcePattern(string _id)
    {
        ClearAttack();
        pattern = BossPatternRuntime.Create(session.Data.patterns.Find(_entry => _entry.id == _id), session);
        pattern.Begin();
    }
#endif
}

using System.Linq;
using UnityEngine;

public sealed class LifeManager : MonoBehaviour
{
    public ArenaSession session;
    public int LifeId { get; private set; }
    public int Remaining { get; private set; }
    public int Total { get; private set; }
    public int Deaths { get; private set; }
    public bool Alive { get; private set; }
    public string AbilityId { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool GuardVulnerable { get; private set; }
    public bool DeathPending => pending;
    public bool Safe => Alive && TelemetryLog.GameTime < safeUntil;
    public float BossDamage { get; set; }
    public AbilityDeck Deck { get; private set; }
    double safeUntil, spawnedAt, respawnAt;
    bool pending;
    HitContext pendingHit;

    public void Initialize()
    {
        Deck = new AbilityDeck(session.Data.abilities.Select(_entry => _entry.id), session.Data.rules.sets, session.Data.rules.seed);
        Total = Remaining = Deck.cards.Count;
        LifeId = Deaths = 0;
        Spawn();
    }

    void Update()
    {
        if (!Alive && Remaining > 0 && !session.Finished && TelemetryLog.GameTime >= respawnAt)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        session.effects.Clear();
        session.abilities.Cleanup();
        session.actions.ResetState();
        session.motor.ResetState(session.spawn.position);
        session.boss.ClearAttack();
        LifeId++;
        BossDamage = 0;
        pending = false;
        Alive = true;
        spawnedAt = TelemetryLog.GameTime;
        safeUntil = spawnedAt + session.Data.rules.spawnSafety;
        AbilityId = Deck.cards[Deaths];
        session.abilities.Equip(AbilityId);
        GameEvents.RaiseLifeChanged(new GameEvents.LifeInfo(LifeId, Remaining, Total, AbilityId));
    }

    public void ResetHealth(float _maximum = 0)
    {
        MaxHealth = _maximum > 0 ? _maximum : session.Data.player.maxHealth;
        Health = Alive ? MaxHealth : 0;
        GuardVulnerable = false;
        PublishState("reset");
    }

    public void ArmGuardVulnerability()
    {
        if (!GuardVulnerable)
        {
            GuardVulnerable = true;
            PublishState("guard_vulnerability");
        }
    }

    public void PublishState(string _reason, string _source = "", float _damage = 0)
    {
        GameEvents.RaisePlayerStateChanged(new GameEvents.PlayerStateInfo(LifeId, Health, MaxHealth,
            session.actions.Guarding, GuardVulnerable, _reason, _source, _damage, session.motor.Position, TelemetryLog.GameTime));
    }

    public void TakeDamage(HitContext _hit)
    {
        if (!Alive || pending)
        {
            return;
        }
        Health = _hit.fatal || GuardVulnerable ? 0 : Mathf.Max(0, Health - _hit.damage);
        PublishState("damage", _hit.source, _hit.damage);
        if (Health <= 0)
        {
            RequestDeath(_hit);
        }
    }

    public void RequestDeath(HitContext _hit)
    {
        if (!Alive || pending || session.Finished)
        {
            return;
        }
        pendingHit = _hit;
        pending = true;
    }

    public void ResolvePendingDeath()
    {
        if (!pending || !Alive)
        {
            return;
        }
        pending = false;
        Alive = false;
        Remaining--;
        Deaths++;
        var info = new GameEvents.DeathInfo(LifeId, Deaths, AbilityId, pendingHit.source,
            pendingHit.kind.ToString(), session.boss.PatternId, (float)(TelemetryLog.GameTime - spawnedAt),
            BossDamage, session.Data.rules.seed, session.motor.Position, TelemetryLog.GameTime);
        GameEvents.RaiseDeath(info);
        session.abilities.Cleanup();
        session.effects.Clear();
        session.boss.ClearAttack();
        session.actions.ResetState();
        session.motor.body.linearVelocity = Vector2.zero;
        session.motor.body.simulated = false;
        session.motor.visual.enabled = false;
        respawnAt = TelemetryLog.GameTime + session.Data.rules.respawnDelay;
        GameEvents.RaiseLifeChanged(new GameEvents.LifeInfo(LifeId, Remaining, Total, info.abilityId));
    }

#if UNITY_EDITOR || DEBUG
    public void ForceAbility(string _id)
    {
        if (!Alive || session.Finished)
        {
            return;
        }
        session.effects.Clear(LifeId);
        session.abilities.Cleanup();
        session.motor.ResetState(session.spawn.position);
        session.actions.ResetState();
        pending = false;
        AbilityId = _id;
        session.abilities.Equip(AbilityId);
        GameEvents.RaiseLifeChanged(new GameEvents.LifeInfo(LifeId, Remaining, Total, _id));
    }
#endif
}

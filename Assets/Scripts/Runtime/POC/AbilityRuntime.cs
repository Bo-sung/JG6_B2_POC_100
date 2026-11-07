using System;
using System.Collections.Generic;
using UnityEngine;

// One equipped ability per life. Every number comes from the ArenaTableData row handed to
// Create; subclasses only translate table fields into motor and effect calls. Unknown ids
// fall back to the no-op base runtime instead of breaking the arena.
public class AbilityRuntime
{
    static Dictionary<string, Func<AbilityData, ArenaSession, AbilityRuntime>> factories;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetFactories()
    {
        factories = null;
    }

    // Explicit id table. No reflection and no per-ability switch anywhere in player code.
    static void EnsureFactories()
    {
        if (factories != null)
        {
            return;
        }
        factories = new Dictionary<string, Func<AbilityData, ArenaSession, AbilityRuntime>>
        {
            { "A01", (_data, _session) => new A01Runtime(_data, _session) },
            { "A02", (_data, _session) => new A02Runtime(_data, _session) },
            { "A03", (_data, _session) => new A03Runtime(_data, _session) },
            { "A04", (_data, _session) => new A04Runtime(_data, _session) },
            { "A05", (_data, _session) => new A05Runtime(_data, _session) },
            { "A06", (_data, _session) => new A06Runtime(_data, _session) },
            { "A07", (_data, _session) => new A07Runtime(_data, _session) },
            { "A08", (_data, _session) => new A08Runtime(_data, _session) },
            { "A09", (_data, _session) => new A09Runtime(_data, _session) },
            { "A10", (_data, _session) => new A10Runtime(_data, _session) },
            { "A11", (_data, _session) => new A11Runtime(_data, _session) },
            { "A12", (_data, _session) => new A12Runtime(_data, _session) },
            { "A13", (_data, _session) => new A13Runtime(_data, _session) },
            { "A14", (_data, _session) => new A14Runtime(_data, _session) },
            { "A15", (_data, _session) => new A15Runtime(_data, _session) },
            { "A16", (_data, _session) => new A16Runtime(_data, _session) },
            { "A17", (_data, _session) => new A17Runtime(_data, _session) },
            { "A18", (_data, _session) => new A18Runtime(_data, _session) },
            { "A19", (_data, _session) => new A19Runtime(_data, _session) },
            { "A20", (_data, _session) => new A20Runtime(_data, _session) },
        };
    }

    public static AbilityRuntime Create(AbilityData _data, ArenaSession _session)
    {
        EnsureFactories();
        if (factories.TryGetValue(_data.id, out var build))
        {
            return build(_data, _session);
        }
        Debug.LogError("Missing ability runtime implementation: " + _data.id);
        return new AbilityRuntime(_data, _session);
    }

    public AbilityData Data { get; }
    public bool Ready => cooldownRemaining <= 0f;
    protected readonly ArenaSession session;
    float cooldownRemaining;

    public AbilityRuntime(AbilityData _data, ArenaSession _session)
    {
        Data = _data;
        session = _session;
    }

    public virtual void Modify(ref PlayerActionRequest _request)
    {
    }

    public virtual void After(PlayerActionRequest _request)
    {
    }

    public virtual void Landed(float _speed)
    {
    }

    public virtual bool Defend(HitContext _hit)
    {
        return false;
    }

    public virtual void Activate()
    {
    }

    public virtual void Tick(float _dt)
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= _dt;
        }
    }

    public virtual void Cleanup()
    {
    }

    protected void SpendCooldown()
    {
        cooldownRemaining = Data.cooldown;
    }

    protected HitContext MakeHit(DamageKind _kind, float _damage, bool _bypassSafety = false)
    {
        return new HitContext(session.life.LifeId, Data.displayName, _kind, _damage, _bypassSafety);
    }

    protected EffectSlot SpawnEffect(Vector2 _position, Vector2 _size, float _delay, float _duration,
        HitContext _hit, bool _boss, bool _player, Vector2 _velocity = default, bool _suicide = false)
    {
        return session.effects.Spawn(_position, _size, _delay, _duration, _hit, _boss, _player, _velocity, _suicide);
    }

    // Player HP is 1, so any applied self hit is lethal. Source stays the ability name so
    // the kill feed always states the true cause of death.
    protected bool ApplySelfHit(DamageKind _kind, bool _bypassSafety = false)
    {
        return session.damage.Apply(false, MakeHit(_kind, 1f, _bypassSafety));
    }

    protected Vector2 AimToBoss()
    {
        var target = (Vector2)session.boss.hurtbox.bounds.center;
        var direction = target - session.motor.Position;
        if (direction.sqrMagnitude < 0.01f)
        {
            return new Vector2(session.motor.Facing, 0);
        }
        return direction.normalized;
    }
}

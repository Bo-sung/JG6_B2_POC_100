using System.Collections.Generic;
using UnityEngine;

public enum DamageKind { Physical, Magic, Self, Fall, Wall, Ceiling, Explosion }

public struct GuardPolicy
{
    public bool physical, magic;
    public float halfAngle;
}

public struct HitContext
{
    public int lifeId, attackId;
    public string source;
    public DamageKind kind;
    public float damage;
    public bool bypassSafety, fatal, guardable, hasOrigin;
    public Vector2 sourcePosition, incomingDirection;

    public HitContext(int _lifeId, string _source, DamageKind _kind, float _damage, bool _bypassSafety = false)
    {
        lifeId = _lifeId;
        attackId = 0;
        source = _source;
        kind = _kind;
        damage = _damage;
        bypassSafety = _bypassSafety;
        fatal = _kind == DamageKind.Fall || _kind == DamageKind.Wall || _kind == DamageKind.Ceiling;
        guardable = hasOrigin = false;
        sourcePosition = incomingDirection = Vector2.zero;
    }
}

public sealed class DamageResolver : MonoBehaviour
{
    public ArenaSession session;
    public int BossHits { get; private set; }
    readonly HashSet<long> resolved = new HashSet<long>();

    public void ResetHits()
    {
        resolved.Clear();
    }

    public bool Apply(bool _toBoss, HitContext _hit)
    {
        if (session.Finished || !session.life.Alive || (_hit.lifeId > 0 && _hit.lifeId != session.life.LifeId))
        {
            return false;
        }
        long key = ((long)_hit.attackId << 1) | (_toBoss ? 1L : 0L);
        if (_hit.attackId != 0 && resolved.Contains(key))
        {
            return false;
        }
        if (_toBoss)
        {
            float dealt = session.boss.TakeDamage(_hit.damage);
            session.life.BossDamage += dealt;
            if (dealt > 0)
            {
                BossHits++;
                if (_hit.attackId != 0)
                {
                    resolved.Add(key);
                }
            }
            return dealt > 0;
        }
        if (_hit.damage <= 0 || session.life.DeathPending)
        {
            return false;
        }
        if (!_hit.bypassSafety && (session.life.Safe || session.motor.Invulnerable))
        {
            return false;
        }
        if (!_hit.bypassSafety && session.actions.TryGuard(_hit))
        {
            if (_hit.attackId != 0)
            {
                resolved.Add(key);
            }
            return true;
        }
        if (_hit.attackId != 0)
        {
            resolved.Add(key);
        }
        session.abilities.Runtime?.Damaged(_hit);
        session.life.TakeDamage(_hit);
        return true;
    }
}

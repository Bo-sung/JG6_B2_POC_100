using UnityEngine;

public enum DamageKind { Physical, Magic, Self, Fall, Wall, Ceiling, Explosion }

public struct HitContext
{
    public int lifeId;
    public string source;
    public DamageKind kind;
    public float damage;
    public bool bypassSafety;

    public HitContext(int _lifeId, string _source, DamageKind _kind, float _damage, bool _bypassSafety = false)
    {
        lifeId = _lifeId;
        source = _source;
        kind = _kind;
        damage = _damage;
        bypassSafety = _bypassSafety;
    }
}

public sealed class DamageResolver : MonoBehaviour
{
    public ArenaSession session;
    public int BossHits { get; private set; }

    public bool Apply(bool _toBoss, HitContext _hit)
    {
        if (session.Finished || !session.life.Alive || (_hit.lifeId > 0 && _hit.lifeId != session.life.LifeId))
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
            }
            return dealt > 0;
        }
        if (!_hit.bypassSafety && session.life.Safe)
        {
            return false;
        }
        if (!_hit.bypassSafety && session.motor.Invulnerable)
        {
            return false;
        }
        if (!_hit.bypassSafety && session.abilities.Defend(_hit))
        {
            return false;
        }
        session.life.RequestDeath(_hit);
        return true;
    }
}

using UnityEngine;

public sealed class EffectSlot : MonoBehaviour
{
    public SpriteRenderer visual;
    [System.NonSerialized] public bool live;
    [System.NonSerialized] public HitContext hit;
    public float Remaining => delay + duration - age;
    public DamageKind SuicideKind { get; set; }
    public float BossPush { get; set; }
    float delay, duration, age;
    bool boss, player, bossHit, playerHit, suicide;
    Vector2 velocity, size, previousPosition;

    public void Configure(Vector2 _position, Vector2 _size, float _delay, float _duration,
        HitContext _hit, bool _boss, bool _player, Vector2 _velocity, bool _suicide)
    {
        transform.position = _position;
        transform.localScale = new Vector3(_size.x, _size.y, 1);
        size = _size;
        delay = _delay;
        duration = _duration;
        hit = _hit;
        boss = _boss;
        player = _player;
        velocity = _velocity;
        previousPosition = _position;
        suicide = _suicide;
        SuicideKind = DamageKind.Explosion;
        BossPush = 0;
        age = 0;
        bossHit = false;
        playerHit = false;
        live = true;
        gameObject.SetActive(true);
        visual.color = Presentation.HazardColor(hit.lifeId, true);
    }

    public void Tick(float _dt, ArenaSession _session)
    {
        age += _dt;
        if (age < delay)
        {
            return;
        }
        visual.color = Presentation.HazardColor(hit.lifeId, false);
        Vector2 old = previousPosition;
        transform.position += (Vector3)(velocity * _dt);
        // Swept box bounds avoid fast projectiles tunnelling between physics ticks.
        Vector2 step = (Vector2)transform.position - old;
        previousPosition = transform.position;
        var bounds = new Bounds(old + step * 0.5f, new Vector3(size.x + Mathf.Abs(step.x), size.y + Mathf.Abs(step.y), 1));
        if (boss && !bossHit && bounds.Intersects(_session.boss.hurtbox.bounds))
        {
            bossHit = _session.damage.Apply(true, hit);
            if (bossHit && BossPush != 0)
            {
                _session.boss.Push(BossPush);
            }
        }
        if (player && !playerHit && bounds.Intersects(_session.motor.hurtbox.bounds))
        {
            playerHit = _session.damage.Apply(false, hit);
        }
        if (suicide)
        {
            _session.damage.Apply(false, new HitContext(hit.lifeId, hit.source, SuicideKind, 1, true));
            suicide = false;
        }
        if (age >= delay + duration)
        {
            Clear();
        }
    }

    public void Clear()
    {
        live = false;
        gameObject.SetActive(false);
    }
}

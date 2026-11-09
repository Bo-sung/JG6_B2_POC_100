using UnityEngine;

public sealed class EffectSlot : MonoBehaviour
{
    public SpriteRenderer visual;
    [System.NonSerialized] public bool live;
    [System.NonSerialized] public HitContext hit;
    public float Remaining => delay + duration - age;
    public DamageKind SuicideKind { get; set; }
    public float BossPush { get; set; }
    float delay, duration, age, angle;
    bool boss, player, bossHit, playerHit, suicide;
    Vector2 velocity, size, previousPosition;
    public Transform FollowTarget { get; set; }
    public Vector2 FollowOffset { get; set; }
    public Transform HomingTarget { get; set; }
    public float HomingSpeed { get; set; }

    public void ReturnToward(Vector2 _target, float _speed)
    {
        HomingTarget = null;
        velocity = (_target - (Vector2)transform.position).normalized * _speed;
        angle = EffectSystem.Angle(velocity);
        transform.rotation = Quaternion.Euler(0, 0, angle);
        player = true;
    }

    public void Configure(Vector2 _position, Vector2 _size, float _delay, float _duration,
        HitContext _hit, bool _boss, bool _player, Vector2 _velocity, bool _suicide, float _angle = 0)
    {
        FollowTarget = HomingTarget = null;
        FollowOffset = Vector2.zero;
        HomingSpeed = 0;
        transform.position = _position;
        angle = _angle;
        transform.rotation = Quaternion.Euler(0, 0, angle);
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
        if (FollowTarget)
        {
            transform.position = (Vector2)FollowTarget.position + FollowOffset;
        }
        else if (HomingTarget)
        {
            Vector2 target = HomingTarget.position;
            velocity = (target - (Vector2)transform.position).normalized * HomingSpeed;
            transform.position = Vector2.MoveTowards(transform.position, target, HomingSpeed * _dt);
        }
        else
        {
            transform.position += (Vector3)(velocity * _dt);
        }
        if (velocity.sqrMagnitude > 0.0001f)
        {
            hit.incomingDirection = velocity.normalized;
        }
        // Sweep the actual oriented box, without widening a diagonal into an AABB.
        Vector2 step = (Vector2)transform.position - old;
        previousPosition = transform.position;
        if (boss && !bossHit && EffectSystem.Overlaps(old, size, angle, _session.boss.hurtbox.bounds, step))
        {
            bossHit = _session.damage.Apply(true, hit);
            if (bossHit && BossPush != 0)
            {
                _session.boss.Push(BossPush);
            }
        }
        if (player && !playerHit && EffectSystem.Overlaps(old, size, angle, _session.motor.hurtbox.bounds, step))
        {
            playerHit = _session.damage.Apply(false, hit);
        }
        if (suicide)
        {
            _session.damage.Apply(false, new HitContext(hit.lifeId, hit.source, SuicideKind, _session.life.MaxHealth, true) { fatal = true });
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

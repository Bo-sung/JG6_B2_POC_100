using UnityEngine;

// Scene-authored reusable slots. A single hit path handles boss and owner damage, boss first.
[DefaultExecutionOrder(100)]
public sealed class EffectSystem : MonoBehaviour
{
    public ArenaSession session;
    public EffectSlot[] slots;
    public int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.live)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public EffectSlot Spawn(Vector2 _position, Vector2 _size, float _delay, float _duration,
        HitContext _hit, bool _boss, bool _player, Vector2 _velocity = default, bool _suicide = false, float _angle = 0)
    {
        if (_hit.attackId == 0)
        {
            _hit.attackId = GameEvents.NextId();
        }
        foreach (var slot in slots)
        {
            if (!slot.live)
            {
                slot.Configure(_position, _size, _delay, _duration, _hit, _boss, _player, _velocity, _suicide, _angle);
                return slot;
            }
        }
        Debug.LogError("Arena effect pool exhausted.");
        return null;
    }

    public static float Angle(Vector2 _direction)
    {
        return Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
    }

    // Continuous SAT: intersect the time intervals of separation along the four box axes.
    // The same oriented rectangle is used by contact checks, boss hits and owner hits.
    public static bool Overlaps(Vector2 _center, Vector2 _size, float _angle, Bounds _target, Vector2 _step = default)
    {
        float radians = _angle * Mathf.Deg2Rad;
        var right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        var up = new Vector2(-right.y, right.x);
        var half = _size * 0.5f;
        Vector2 targetHalf = _target.extents;
        Vector2 delta = (Vector2)_target.center - _center;
        float enter = 0;
        float exit = 1;
        return OverlapsAxis(Vector2.right, right, up, half, targetHalf, delta, _step, ref enter, ref exit)
            && OverlapsAxis(Vector2.up, right, up, half, targetHalf, delta, _step, ref enter, ref exit)
            && OverlapsAxis(right, right, up, half, targetHalf, delta, _step, ref enter, ref exit)
            && OverlapsAxis(up, right, up, half, targetHalf, delta, _step, ref enter, ref exit);
    }

    static bool OverlapsAxis(Vector2 _axis, Vector2 _right, Vector2 _up, Vector2 _half,
        Vector2 _targetHalf, Vector2 _delta, Vector2 _step, ref float _enter, ref float _exit)
    {
        float radius = _half.x * Mathf.Abs(Vector2.Dot(_axis, _right))
            + _half.y * Mathf.Abs(Vector2.Dot(_axis, _up))
            + _targetHalf.x * Mathf.Abs(_axis.x) + _targetHalf.y * Mathf.Abs(_axis.y);
        float distance = Vector2.Dot(_delta, _axis);
        float speed = Vector2.Dot(_step, _axis);
        if (Mathf.Abs(speed) < 0.000001f)
        {
            return Mathf.Abs(distance) <= radius;
        }
        float first = (distance - radius) / speed;
        float last = (distance + radius) / speed;
        _enter = Mathf.Max(_enter, Mathf.Min(first, last));
        _exit = Mathf.Min(_exit, Mathf.Max(first, last));
        return _enter <= _exit;
    }

    public void Clear(int _lifeId = 0, bool _bossOnly = false)
    {
        foreach (var slot in slots)
        {
            if (_bossOnly ? slot.hit.lifeId == 0 : _lifeId == 0 || slot.hit.lifeId == _lifeId)
            {
                slot.Clear();
            }
        }
    }

    void FixedUpdate()
    {
        if (session.Finished || !session.life.Alive)
        {
            return;
        }
        foreach (var slot in slots)
        {
            if (!slot.live)
            {
                continue;
            }
            slot.Tick(Time.fixedDeltaTime, session);
        }
    }
}

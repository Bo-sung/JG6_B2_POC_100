using UnityEngine;

// Scene-authored reusable slots. A single hit path handles boss and owner damage, boss first.
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
        HitContext _hit, bool _boss, bool _player, Vector2 _velocity = default, bool _suicide = false)
    {
        foreach (var slot in slots)
        {
            if (!slot.live)
            {
                slot.Configure(_position, _size, _delay, _duration, _hit, _boss, _player, _velocity, _suicide);
                return slot;
            }
        }
        Debug.LogError("Arena effect pool exhausted.");
        return null;
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

using UnityEngine;

// A09 갈래나무 창 — the spear auto-tracks the boss wherever you face, but when the boss is
// attacking the spear flinches out of the way and the whole attack whiffs.
// range/height: thrust box, duration: thrust life, damage: thrust, cooldown: per thrust.
public sealed class A09Runtime : AbilityRuntime
{
    public A09Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled)
        {
            return;
        }
        if (session.boss.Attacking)
        {
            _request.cancelled = true;
        }
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled || !Ready)
        {
            return;
        }
        SpendCooldown();
        var direction = AimToBoss();
        var center = session.motor.Position + direction * (Data.range * 0.5f);
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, false);
    }
}

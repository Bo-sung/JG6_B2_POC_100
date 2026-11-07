using UnityEngine;

// A08 만뢰의 창 — every jump reserves a thunder strike at the takeoff point; linger around
// your own launch pad and the lightning lands on you too. delay: strike fuse,
// range/height: strike column, damage: strike, cooldown: per jump.
public sealed class A08Runtime : AbilityRuntime
{
    public A08Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Jump || _request.cancelled || !Ready)
        {
            return;
        }
        SpendCooldown();
        SpawnEffect(session.motor.Position, new Vector2(Data.range, Data.height), Data.delay,
            Data.duration, MakeHit(DamageKind.Magic, Data.damage), true, true);
    }
}

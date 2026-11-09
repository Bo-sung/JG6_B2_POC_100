using System.Collections.Generic;
using UnityEngine;

public sealed class A10Runtime : AbilityRuntime
{
    readonly List<EffectSlot> liquid = new List<EffectSlot>();

    public A10Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind == PlayerActionKind.Attack)
        {
            _request.replaceBasic = true;
        }
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || !Ready)
        {
            return;
        }
        SpendCooldown();
        liquid.RemoveAll(_slot => !_slot.live || _slot.hit.source != Data.id);
        Vector2 direction = _request.direction;
        var half = session.motor.hurtbox.bounds.extents;
        float support = Mathf.Abs(direction.x) * half.x + Mathf.Abs(direction.y) * half.y;
        var muzzle = session.motor.Position + direction * (support + Data.range * .5f + Data.spawnOffset);
        var shot = SpawnEffect(muzzle, new Vector2(Data.range, Data.height), 0, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, true, direction * Data.speed, _angle: EffectSystem.Angle(direction));
        if (shot)
        {
            liquid.Add(shot);
            if (session.actions.Guarding)
            {
                shot.ReturnToward(session.motor.Position, Data.speed);
            }
        }
    }

    public override void GuardStarted()
    {
        foreach (var shot in liquid)
        {
            if (shot.live && shot.hit.lifeId == session.life.LifeId && shot.hit.source == Data.id)
            {
                shot.ReturnToward(session.motor.Position, Data.speed);
            }
        }
    }

    public override void Cleanup()
    {
        liquid.Clear();
    }
}

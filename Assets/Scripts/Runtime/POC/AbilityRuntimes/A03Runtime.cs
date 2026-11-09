using UnityEngine;

// A03 백염의 병: 몸에서 조준 방향으로 광선을 발사한다. 발동 위치에 위험 지역을 만들지 않는다.
public sealed class A03Runtime : AbilityRuntime
{
    public A03Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        var direction = castDirection;
        var center = session.motor.Position + direction * (Data.range * 0.5f);
        var beam = SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false, _angle: EffectSystem.Angle(direction));
        if (beam)
        {
            beam.FollowTarget = session.motor.transform;
            beam.FollowOffset = direction * (Data.range * .5f);
        }
    }
}

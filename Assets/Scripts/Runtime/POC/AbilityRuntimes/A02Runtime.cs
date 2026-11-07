using UnityEngine;

// A02 티알파의 날개 — the dash becomes an uncontrollable supercharge with contact damage
// along its path; braking is impossible and wall contact at that speed is fatal
// (PlayerMotor2D applies the wall check). speed/duration: dash, range/height: trail box.
public sealed class A02Runtime : AbilityRuntime
{
    public A02Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Dash || _request.cancelled || session.motor.Locked)
        {
            return;
        }
        _request.speed = Data.speed;
        _request.duration = Data.duration;
        _request.direction = _request.direction.sqrMagnitude > 0.01f
            ? _request.direction.normalized
            : new Vector2(session.motor.Facing, 0);
        SpawnEffect(session.motor.Position, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, false, _request.direction * Data.speed);
    }
}

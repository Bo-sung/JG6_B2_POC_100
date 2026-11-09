using UnityEngine;

// The authored sword tip and pooled world-space chords follow the same timed sweep.
public sealed class A01Runtime : AbilityRuntime
{
    bool swinging;
    float age;
    int emitted;
    Vector2 direction, previousTip;
    HitContext traceHit;

    public A01Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
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
        if (_request.kind != PlayerActionKind.Attack || !Ready || swinging)
        {
            return;
        }
        SpendCooldown();
        swinging = true;
        age = 0;
        emitted = 0;
        direction = _request.direction;
        previousTip = Tip(0);
        traceHit = MakeHit(DamageKind.Physical, Data.damage);
        traceHit.attackId = GameEvents.NextId();
        ShowSword(0);
    }

    Vector2 Tip(float _progress)
    {
        float angle = EffectSystem.Angle(direction) + Mathf.Lerp(-Data.swingArc * .5f, Data.swingArc * .5f, _progress);
        float radians = angle * Mathf.Deg2Rad;
        return session.motor.Position + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * Data.range;
    }

    void ShowSword(float _progress)
    {
        var motor = session.motor;
        if (motor.weaponPivot && motor.weaponVisual)
        {
            motor.weaponPivot.position = motor.Position;
            motor.weaponPivot.rotation = Quaternion.Euler(0, 0,
                EffectSystem.Angle(direction) + Mathf.Lerp(-Data.swingArc * .5f, Data.swingArc * .5f, _progress));
            motor.weaponVisual.transform.localPosition = Vector2.right * (Data.range * .7f);
            motor.weaponVisual.transform.localScale = new Vector3(Data.range * .6f, Data.traceWidth, 1);
            motor.weaponVisual.gameObject.SetActive(true);
        }
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (!swinging)
        {
            return;
        }
        age += _dt;
        float progress = Mathf.Clamp01(age / Data.swingDuration);
        ShowSword(progress);
        int samples = Mathf.Min(Data.emissionCount, Mathf.FloorToInt(progress * Data.emissionCount));
        while (emitted < samples)
        {
            emitted++;
            Vector2 tip = Tip((float)emitted / Data.emissionCount);
            Vector2 chord = tip - previousTip;
            SpawnEffect((tip + previousTip) * .5f, new Vector2(chord.magnitude + Data.traceWidth, Data.traceWidth),
                0, Data.duration, traceHit, true, true, _angle: EffectSystem.Angle(chord));
            previousTip = tip;
        }
        if (progress >= 1)
        {
            swinging = false;
            session.motor.weaponVisual.gameObject.SetActive(false);
        }
    }

    public override void Cleanup()
    {
        swinging = false;
        if (session.motor.weaponVisual)
        {
            session.motor.weaponVisual.gameObject.SetActive(false);
        }
    }
}

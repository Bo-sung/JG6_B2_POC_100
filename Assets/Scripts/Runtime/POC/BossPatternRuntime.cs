using System;
using UnityEngine;

public abstract class BossPatternRuntime
{
    public BossPatternData Data { get; }
    public float Age { get; private set; }
    protected readonly ArenaSession session;
    protected Vector2 Origin => session.boss.transform.position;
    protected Vector2 Target => session.motor.Position;
    protected float Floor => session.Data.rules.floorY;

    protected BossPatternRuntime(BossPatternData _data, ArenaSession _session)
    {
        Data = _data;
        session = _session;
    }

    public static BossPatternRuntime Create(BossPatternData _data, ArenaSession _session)
    {
        return _data.id switch
        {
            "B01" => new BossSlash(_data, _session),
            "B02" => new BossSlam(_data, _session),
            "B03" => new BossRush(_data, _session),
            "B04" => new BossWaves(_data, _session),
            "B05" => new BossBolts(_data, _session),
            "B06" => new BossMeteors(_data, _session),
            "B07" => new BossLaser(_data, _session),
            _ => throw new ArgumentException("Unknown boss pattern " + _data.id)
        };
    }

    public abstract void Begin();

    public virtual void Tick(float _dt)
    {
        Age += _dt;
    }

    protected void Hazard(Vector2 _position, Vector2 _size, float _delay, float _duration,
        DamageKind _kind = DamageKind.Physical, Vector2 _velocity = default)
    {
        session.effects.Spawn(_position, _size, _delay, _duration,
            new HitContext(0, Data.id, _kind, 1), false, true, _velocity);
    }
}

public sealed class BossSlash : BossPatternRuntime
{
    public BossSlash(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        float facing = Target.x < Origin.x ? -1 : 1;
        Hazard(new Vector2(Origin.x + facing * Data.width * 0.5f, Floor + Data.height * 0.5f),
            new Vector2(Data.width, Data.height), Data.warning, Data.duration);
    }
}

public sealed class BossSlam : BossPatternRuntime
{
    public BossSlam(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        Hazard(new Vector2(Target.x, Floor + Data.height * 0.5f), new Vector2(Data.width, Data.height), Data.warning, Data.duration);
    }
}

public sealed class BossRush : BossPatternRuntime
{
    Vector2 start, destination;

    public BossRush(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        start = Origin;
        float half = session.boss.hurtbox.bounds.extents.x;
        float endX = Target.x < start.x ? session.Data.rules.leftWall + half : session.Data.rules.rightWall - half;
        destination = new Vector2(endX, start.y);
        // Show the route, then damage only the moving body: the corridor never becomes one long hitbox.
        session.effects.Spawn(new Vector2((start.x + endX) * 0.5f, Floor + Data.height * 0.5f),
            new Vector2(Mathf.Abs(endX - start.x), Data.height), Data.warning, 0,
            new HitContext(0, Data.id, DamageKind.Physical, 1), false, false);
        Hazard(new Vector2(start.x, Floor + Data.height * 0.5f), new Vector2(Data.width, Data.height),
            Data.warning, Data.duration, DamageKind.Physical,
            new Vector2(Mathf.Sign(endX - start.x) * Data.speed, 0));
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (Age >= Data.warning && Age <= Data.warning + Data.duration)
        {
            session.boss.transform.position = Vector2.MoveTowards(Origin, destination, Data.speed * _dt);
        }
    }
}

public sealed class BossWaves : BossPatternRuntime
{
    public BossWaves(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        for (int direction = -1; direction <= 1; direction += 2)
        {
            Hazard(new Vector2(Origin.x + direction * Data.width, Floor + Data.height * 0.5f),
                new Vector2(Data.width, Data.height), Data.warning, Data.duration, DamageKind.Physical,
                new Vector2(direction * Data.speed, 0));
        }
    }
}

public sealed class BossBolts : BossPatternRuntime
{
    public BossBolts(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        Vector2 direction = (Target - Origin).normalized;
        for (int i = 0; i < 3; i++)
        {
            Hazard(Origin, new Vector2(Data.width, Data.height), Data.warning + i * Data.interval,
                Data.duration - i * Data.interval, DamageKind.Magic, direction * Data.speed);
        }
    }
}

public sealed class BossMeteors : BossPatternRuntime
{
    public BossMeteors(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        for (int i = -1; i <= 1; i++)
        {
            float x = Mathf.Clamp(Target.x + i * Data.interval, session.Data.rules.leftWall, session.Data.rules.rightWall);
            Hazard(new Vector2(x, Floor + Data.height * 0.5f), new Vector2(Data.width, Data.height),
                Data.warning, Data.duration, DamageKind.Magic);
        }
    }
}

public sealed class BossLaser : BossPatternRuntime
{
    public BossLaser(BossPatternData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Begin()
    {
        float facing = Target.x < Origin.x ? -1 : 1;
        // Successive horizontal strips sweep upwards. Their own delayed markers show exact hit positions.
        int strips = Mathf.Max(1, Mathf.FloorToInt(Data.duration / Data.interval));
        for (int i = 0; i < strips; i++)
        {
            Hazard(new Vector2(Origin.x + facing * Data.width * 0.5f, Floor + Data.height * (i + 0.5f)),
                new Vector2(Data.width, Data.height), Data.warning + i * Data.interval,
                Data.interval, DamageKind.Magic);
        }
    }
}

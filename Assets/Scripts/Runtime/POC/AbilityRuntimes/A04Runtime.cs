using UnityEngine;

public sealed class A04Runtime : AbilityRuntime
{
    public A04Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready || !session.motor.Grounded)
        {
            return;
        }
        SpendCooldown();
        float feet = session.motor.hurtbox.bounds.min.y;
        for (int i = 0; i < Data.emissionCount; i++)
        {
            float x = session.motor.Position.x + (i - (Data.emissionCount - 1) * .5f) * Data.spacing;
            var surfaces = Physics2D.RaycastAll(new Vector2(x, feet + Data.surfaceReach), Vector2.down,
                Data.surfaceReach * 2, GameLayers.GROUND_OBSTACLE_MASK);
            bool found = false;
            float top = 0;
            float nearest = float.PositiveInfinity;
            foreach (var surface in surfaces)
            {
                float distance = Mathf.Abs(surface.point.y - feet);
                if (surface.normal.y > .5f && distance < nearest)
                {
                    found = true;
                    nearest = distance;
                    top = surface.point.y;
                }
            }
            if (found)
            {
                SpawnEffect(new Vector2(x, top + Data.height * .5f), new Vector2(Data.range, Data.height),
                    Data.delay + i * Data.extra, Data.duration, MakeHit(DamageKind.Physical, Data.damage), true, true);
            }
        }
    }
}

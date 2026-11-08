using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class PlayerMotor2D : MonoBehaviour
{
    public ArenaSession session;
    public Rigidbody2D body;
    public BoxCollider2D hurtbox;
    public SpriteRenderer visual;
    public int Facing { get; private set; } = 1;
    public bool Grounded { get; private set; }
    public bool CanJump => coyote > 0 && !Locked;
    public bool Locked => lockTimer > 0;
    public bool Invulnerable => dashTimer > 0 && !dangerDash;
    public bool Phasing => dashTimer > 0 && phase;
    public Vector2 Position => body.position;
    float coyote, dashTimer, lockTimer, impulseTimer, lastFallSpeed;
    Vector2 movement, dashVelocity;
    bool dangerDash, phase;
    readonly ContactPoint2D[] contacts = new ContactPoint2D[12];

    public void SetMovement(Vector2 _movement)
    {
        movement = _movement;
        if (Mathf.Abs(movement.x) > 0.1f && dashTimer <= 0 && !Locked)
        {
            Facing = movement.x < 0 ? -1 : 1;
        }
    }

    public void ResetState(Vector2 _spawn)
    {
        body.simulated = false;
        transform.localScale = Vector3.one;
        hurtbox.isTrigger = false;
        body.position = _spawn;
        transform.position = _spawn;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0;
        body.gravityScale = session.Data.player.gravity;
        movement = Vector2.zero;
        coyote = dashTimer = lockTimer = impulseTimer = lastFallSpeed = 0;
        dangerDash = phase = Grounded = false;
        Facing = 1;
        visual.enabled = true;
        body.simulated = true;
        Physics2D.SyncTransforms();
    }

    public void Jump()
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, session.Data.player.jumpSpeed);
        coyote = 0;
        Grounded = false;
    }

    public void Dash(PlayerActionRequest _request)
    {
        dashTimer = _request.duration;
        dashVelocity = _request.direction * _request.speed;
        dangerDash = _request.speed > session.Data.player.wallDeathSpeed;
        phase = _request.phase;
    }

    public void Impulse(Vector2 _velocity, float _duration)
    {
        body.linearVelocity = _velocity;
        impulseTimer = _duration;
    }

    public void Lock(float _seconds)
    {
        lockTimer = Mathf.Max(lockTimer, _seconds);
    }

    void FixedUpdate()
    {
        if (!session.life || !session.life.Alive || session.Finished)
        {
            return;
        }
        var data = session.Data.player;
        float dt = Time.fixedDeltaTime;
        bool wasGrounded = Grounded;
        Grounded = false;
        int count = hurtbox.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.5f)
            {
                Grounded = true;
            }
        }
        if (!Grounded && !Phasing && body.linearVelocity.y <= 0)
        {
            var bounds = hurtbox.bounds;
            Vector2 probeOrigin = new Vector2(bounds.center.x, bounds.min.y + data.groundProbe);
            Grounded = Physics2D.BoxCast(probeOrigin, new Vector2(bounds.size.x * 0.8f, data.groundProbe),
                0, Vector2.down, data.groundProbe, GameLayers.GROUND_OBSTACLE_MASK).collider;
        }
        if (Grounded && !wasGrounded)
        {
            session.abilities.Landed(lastFallSpeed);
        }
        if (!Grounded)
        {
            lastFallSpeed = Mathf.Max(0, -body.linearVelocity.y);
        }
        coyote = Grounded ? data.coyoteTime : Mathf.Max(0, coyote - dt);
        lockTimer -= dt;
        impulseTimer -= dt;
        if (dashTimer > 0)
        {
            dashTimer -= dt;
            hurtbox.isTrigger = phase;
            body.gravityScale = 0;
            body.linearVelocity = dashVelocity;
            if (dangerDash)
            {
                var ray = Physics2D.Raycast(body.position, dashVelocity.normalized, dashVelocity.magnitude * dt + hurtbox.bounds.extents.x, GameLayers.WORLD_MASK);
                if (ray.collider && Mathf.Abs(ray.normal.x) > 0.5f)
                {
                    session.damage.Apply(false, new HitContext(session.life.LifeId, "wall", DamageKind.Wall, 1));
                }
            }
        }
        else
        {
            if (hurtbox.isTrigger)
            {
                // Ending a phase inside solid geometry cannot eject the player to a free safe position.
                var solid = Physics2D.OverlapBox(body.position, hurtbox.bounds.size * 0.9f, 0, GameLayers.WORLD_MASK);
                if (solid)
                {
                    bool belowFloor = body.position.y < session.Data.rules.floorY;
                    session.damage.Apply(false, new HitContext(session.life.LifeId, belowFloor ? "fall" : "wall",
                        belowFloor ? DamageKind.Fall : DamageKind.Wall, 1, true));
                }
            }
            hurtbox.isTrigger = false;
            body.gravityScale = data.gravity;
            if (impulseTimer <= 0)
            {
                float target = Locked ? 0 : movement.x * data.speed;
                body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, target, data.acceleration * dt), body.linearVelocity.y);
            }
            if (movement.y < -0.5f && !Grounded)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, -data.fallSpeed));
            }
        }
        if (body.position.y < session.Data.rules.deathY)
        {
            session.damage.Apply(false, new HitContext(session.life.LifeId, "fall", DamageKind.Fall, 1, true));
        }
        visual.color = Presentation.PlayerColor(Phasing, session.life.Safe, Locked);
    }

    void OnCollisionEnter2D(Collision2D _collision)
    {
        if (!session.life.Alive)
        {
            return;
        }
        for (int i = 0; i < _collision.contactCount; i++)
        {
            var normal = _collision.GetContact(i).normal;
            if (Mathf.Abs(normal.x) > 0.5f && _collision.relativeVelocity.magnitude > session.Data.player.wallDeathSpeed)
            {
                session.damage.Apply(false, new HitContext(session.life.LifeId, "wall", DamageKind.Wall, 1));
            }
            if (normal.y < -0.5f && transform.localScale.y > 1)
            {
                session.damage.Apply(false, new HitContext(session.life.LifeId, "ceiling", DamageKind.Ceiling, 1));
            }
        }
    }
}

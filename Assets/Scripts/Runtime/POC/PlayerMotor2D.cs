using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class PlayerMotor2D : MonoBehaviour
{
    public ArenaSession session;
    public Rigidbody2D body;
    public BoxCollider2D hurtbox;
    public SpriteRenderer visual;
    public Transform eyes;
    public Transform aimPivot;
    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public int Facing { get; private set; } = 1;
    public bool Grounded { get; private set; }
    public bool CanJump => coyote > 0 && !Locked;
    public bool Locked => lockTimer > 0 || freezeTimer > 0;
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
    }

    public static Vector2 NormalizeAim(Vector2 _direction, Vector2 _fallback)
    {
        float lengthSquared = _direction.sqrMagnitude;
        if (float.IsNaN(lengthSquared) || float.IsInfinity(lengthSquared) || lengthSquared < 0.0001f)
        {
            return _fallback;
        }
        return _direction / Mathf.Sqrt(lengthSquared);
    }

    public void SetAim(Vector2 _direction)
    {
        if (aimLocked && freezeTimer > 0)
        {
            return;
        }
        AimDirection = NormalizeAim(_direction, AimDirection);
        if (Mathf.Abs(AimDirection.x) > 0.0001f)
        {
            Facing = AimDirection.x < 0 ? -1 : 1;
        }
        if (eyes)
        {
            var position = eyes.localPosition;
            position.x = Mathf.Abs(position.x) * Facing;
            eyes.localPosition = position;
        }
        if (aimPivot)
        {
            aimPivot.localRotation = Quaternion.Euler(0, 0, EffectSystem.Angle(AimDirection));
        }
    }

    void LateUpdate()
    {
        bool visible = visual.enabled;
        if (eyes)
        {
            eyes.gameObject.SetActive(visible);
        }
        if (aimPivot)
        {
            aimPivot.gameObject.SetActive(visible);
        }
    }

    public SpriteRenderer guardVisual;
    public Transform weaponPivot;
    public SpriteRenderer weaponVisual, chargeVisual;
    public bool Recoiling => impulseTimer > 0;
    public bool Heavy => heavyTimer > 0;
    public float AirPeak { get; private set; }
    public float FallDistance => Mathf.Max(0, AirPeak - Position.y);
    float freezeTimer, heavyTimer, heavyGravity, heavySpeed, sinkAcceleration;
    bool aimLocked;
    Vector2 freezePosition;
    Vector2 previousVelocity;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[16];

    public void ClearTransientState()
    {
        if (transform.localScale != Vector3.one)
        {
            SetScale(1);
        }
        coyote = dashTimer = lockTimer = impulseTimer = lastFallSpeed = freezeTimer = heavyTimer = sinkAcceleration = 0;
        dangerDash = phase = aimLocked = false;
        hurtbox.isTrigger = false;
        body.gravityScale = session.Data.player.gravity;
        if (guardVisual)
        {
            guardVisual.gameObject.SetActive(false);
        }
        if (weaponVisual)
        {
            weaponVisual.gameObject.SetActive(false);
        }
        if (chargeVisual)
        {
            chargeVisual.gameObject.SetActive(false);
        }
    }

    public void ResetState(Vector2 _spawn)
    {
        body.simulated = false;
        ClearTransientState();
        body.position = _spawn;
        transform.position = _spawn;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0;
        previousVelocity = movement = Vector2.zero;
        Grounded = false;
        AirPeak = _spawn.y;
        SetAim(AimDirection);
        visual.enabled = true;
        body.simulated = true;
        Physics2D.SyncTransforms();
    }

    public void SetScale(float _scale)
    {
        // Preserve the feet when grounded. Visual and collider use the same positive scale.
        float oldBottom = hurtbox.bounds.min.y;
        transform.localScale = Vector3.one * _scale;
        Physics2D.SyncTransforms();
        if (Grounded)
        {
            body.position += Vector2.up * (oldBottom - hurtbox.bounds.min.y);
            transform.position = body.position;
            Physics2D.SyncTransforms();
        }
    }

    public void CancelDash()
    {
        dashTimer = impulseTimer = 0;
        dangerDash = false;
        // Keep an existing phase trigger until the normal overlap recovery check runs.
    }

    public void Immobilize(float _seconds, bool _lockAim = false)
    {
        CancelDash();
        freezePosition = body.position;
        freezeTimer = Mathf.Max(freezeTimer, _seconds);
        aimLocked = _lockAim;
        body.gravityScale = 0;
        body.linearVelocity = previousVelocity = Vector2.zero;
    }

    public void StartHeavy(float _duration, float _gravity, float _speed)
    {
        CancelDash();
        heavyTimer = _duration;
        heavyGravity = _gravity;
        heavySpeed = _speed;
        body.gravityScale = session.Data.player.gravity * _gravity;
        body.linearVelocity = Grounded ? Vector2.zero : Vector2.down * _speed;
        previousVelocity = body.linearVelocity;
    }

    public void EndHeavy()
    {
        heavyTimer = 0;
        body.gravityScale = session.Data.player.gravity;
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
        sinkAcceleration = _request.sinkAcceleration;
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
            if (contacts[i].normal.y > 0.5f && !Phasing)
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
            EndHeavy();
            lastFallSpeed = 0;
            AirPeak = Position.y;
        }
        if (!Grounded)
        {
            AirPeak = Mathf.Max(AirPeak, Position.y);
            lastFallSpeed = Mathf.Max(lastFallSpeed, Mathf.Max(0, -previousVelocity.y));
        }
        else
        {
            AirPeak = Position.y;
        }
        coyote = Grounded ? data.coyoteTime : Mathf.Max(0, coyote - dt);
        lockTimer -= dt;
        impulseTimer -= dt;
        heavyTimer = Mathf.Max(0, heavyTimer - dt);
        if (freezeTimer > 0)
        {
            freezeTimer = Mathf.Max(0, freezeTimer - dt);
            body.gravityScale = 0;
            body.position = freezePosition;
            body.linearVelocity = Vector2.zero;
            previousVelocity = Vector2.zero;
            if (freezeTimer <= 0)
            {
                aimLocked = false;
            }
        }
        else if (dashTimer > 0)
        {
            dashTimer -= dt;
            hurtbox.isTrigger = phase;
            body.gravityScale = 0;
            if (phase)
            {
                dashVelocity += Vector2.down * (sinkAcceleration * dt);
            }
            body.linearVelocity = dashVelocity;
            if (dangerDash && !phase)
            {
                var bounds = hurtbox.bounds;
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = GameLayers.WORLD_MASK, useTriggers = false };
                int hits = Physics2D.BoxCast(bounds.center, (Vector2)bounds.size * 0.98f, 0,
                    dashVelocity.normalized, filter, castHits, dashVelocity.magnitude * dt);
                for (int i = 0; i < hits; i++)
                {
                    if (castHits[i].distance > 0.0001f)
                    {
                        CheckImpact(castHits[i].normal, dashVelocity);
                    }
                }
            }
        }
        else
        {
            if (hurtbox.isTrigger)
            {
                // A phase ends where it really is. Never teleport/eject to safety.
                var solid = Physics2D.OverlapBox(body.position, hurtbox.bounds.size * 0.9f, 0, GameLayers.WORLD_MASK);
                if (solid)
                {
                    bool belowFloor = body.position.y < session.Data.rules.floorY;
                    session.damage.Apply(false, new HitContext(session.life.LifeId, belowFloor ? "fall" : "wall",
                        belowFloor ? DamageKind.Fall : DamageKind.Wall, session.life.MaxHealth, true));
                }
            }
            hurtbox.isTrigger = false;
            phase = false;
            body.gravityScale = data.gravity * (Heavy ? heavyGravity : 1);
            if (impulseTimer <= 0)
            {
                float target = Locked ? 0 : movement.x * data.speed;
                body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, target, data.acceleration * dt), body.linearVelocity.y);
            }
            if (Heavy)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(body.linearVelocity.y, -heavySpeed));
            }
            if (movement.y < -0.5f && !Grounded)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, -data.fallSpeed));
            }
        }
        if (body.position.y < session.Data.rules.deathY)
        {
            session.damage.Apply(false, new HitContext(session.life.LifeId, "fall", DamageKind.Fall, session.life.MaxHealth, true));
        }
        if (chargeVisual)
        {
            chargeVisual.gameObject.SetActive(freezeTimer > 0);
        }
        visual.color = Presentation.PlayerColor(Phasing, session.life.Safe, Locked, Heavy);
        previousVelocity = body.linearVelocity;
    }

    void CheckImpact(Vector2 _normal, Vector2 _velocity)
    {
        float normalSpeed = Mathf.Max(0, -Vector2.Dot(_velocity, _normal));
        if (normalSpeed > session.Data.player.wallDeathSpeed)
        {
            DamageKind kind = _normal.y > 0.5f ? DamageKind.Fall : _normal.y < -0.5f ? DamageKind.Ceiling : DamageKind.Wall;
            string source = kind == DamageKind.Fall ? "fall" : kind == DamageKind.Ceiling ? "ceiling" : "wall";
            session.damage.Apply(false, new HitContext(session.life.LifeId, source, kind, session.life.MaxHealth));
        }
    }

    void OnCollisionEnter2D(Collision2D _collision)
    {
        if (!session.life.Alive || (GameLayers.WORLD_MASK & (1 << _collision.gameObject.layer)) == 0)
        {
            return;
        }
        for (int i = 0; i < _collision.contactCount; i++)
        {
            var normal = _collision.GetContact(i).normal;
            // Pre-solver motion retains speed when the physics solver has already stopped the body.
            // Tangential ground motion has zero normal approach, in every dash/recoil direction.
            float measuredSpeed = Mathf.Abs(Vector2.Dot(_collision.relativeVelocity, normal));
            Vector2 impactVelocity = measuredSpeed > Mathf.Max(0, -Vector2.Dot(previousVelocity, normal))
                ? -normal * measuredSpeed : previousVelocity;
            CheckImpact(normal, impactVelocity);
            if (normal.y < -0.5f && transform.localScale.y > 1)
            {
                session.damage.Apply(false, new HitContext(session.life.LifeId, "ceiling", DamageKind.Ceiling, session.life.MaxHealth));
            }
        }
    }
}

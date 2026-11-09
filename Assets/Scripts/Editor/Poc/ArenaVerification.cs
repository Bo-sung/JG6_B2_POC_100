using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicitly invoked Play-mode integration checks. No test assembly or runtime input binding is added.
public static class ArenaVerification
{
    [Serializable]
    public sealed class Report
    {
        public bool running, passed;
        public string stage, data, error, telemetry;
        public int frameStart, frameEnd, deathsObserved, lifeEvents, endEvents, exhaustionDeaths, exhaustionEndEvents;
        public List<Probe> abilities = new List<Probe>();
        public List<string> patterns = new List<string>();
        public bool exhaustion, simultaneousVictory, cleanup, hpPersistent, passiveSkillGated;
    }

    [Serializable]
    public sealed class Probe
    {
        public string id, runtime;
        public bool active, triggered, magicBlocked, physicalBlocked;
        public int effectPeak;
        public float bossDamage, displacement;
    }

    public static Report Current { get; private set; }
    static int index, state;
    static double deadline;
    static float originalTimeScale, initialBossHp;
    static Vector2 initialPosition;
    static Probe probe;
    static bool originalRunBackground;
    static int cleanupIndex, cleanupLifeId;

    [Obsolete("Legacy ba7b72a integration scenarios do not cover the guard/health revision.")]
    public static void Start()
    {
        throw new InvalidOperationException("Legacy integration scenarios are retired: universal 1HP, K-only guards and airborne-S A19 are obsolete. Use the current manual checklist in Docs/POC_HANDOFF.md; request a revised automated play suite explicitly.");
#pragma warning disable CS0162
        Stop();
        Current = new Report { running = true, stage = "data", frameStart = Time.frameCount };
        Current.data = SmokeCheck.VerifyArenaData();
        originalTimeScale = Time.timeScale;
        originalRunBackground = Application.runInBackground;
        Time.timeScale = 8;
        Application.runInBackground = true;
        index = state = cleanupIndex = 0;
        Current.passiveSkillGated = true;
        deadline = TelemetryLog.GameTime + 2;
        GameEvents.Death += Death;
        GameEvents.LifeChanged += Life;
        GameEvents.BattleEnded += End;
        EditorApplication.update += Tick;
    }

#pragma warning restore CS0162
    public static void Stop()
    {
        EditorApplication.update -= Tick;
        GameEvents.Death -= Death;
        GameEvents.LifeChanged -= Life;
        GameEvents.BattleEnded -= End;
        if (Current != null && Current.running)
        {
            Current.running = false;
            Time.timeScale = originalTimeScale;
            Application.runInBackground = originalRunBackground;
        }
    }

    static void Death(GameEvents.DeathInfo _info)
    {
        Current.deathsObserved++;
    }
    static void Life(GameEvents.LifeInfo _info)
    {
        Current.lifeEvents++;
    }
    static void End(GameEvents.BattleEndInfo _info)
    {
        Current.endEvents++;
    }

    static void Check(bool _condition, string _message)
    {
        if (!_condition)
        {
            throw new InvalidOperationException(_message);
        }
    }

    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Play mode stopped during verification.");
            }
            var session = SceneServices.Find<ArenaSession>(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (!session || session.Data == null || TelemetryLog.GameTime < deadline)
            {
                return;
            }
            session.actions.enabled = false;
            session.boss.enabled = false;
            switch (state)
            {
                case 0:
                    Current.stage = "abilities";
                    if (!session.life.Alive)
                    {
                        return;
                    }
                    session.boss.SetHealth(session.Data.rules.bossHealth);
                    string id = "A" + (++index).ToString("00");
                    session.life.ForceAbility(id);
                    session.motor.ResetState(new Vector2(session.boss.hurtbox.bounds.min.x - .25f, session.Data.rules.floorY + .75f));
                    if (id == "A04")
                    {
                        // Cast above the floor blades, as after jumping from an authored platform.
                        session.motor.ResetState(new Vector2(session.motor.Position.x, session.Data.rules.floorY + session.abilities.Runtime.Data.height + .8f));
                    }
                    session.motor.body.linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    var data = session.abilities.Runtime.Data;
                    probe = new Probe { id = id, runtime = session.abilities.Runtime.GetType().Name, active = data.active };
                    Current.abilities.Add(probe);
                    initialBossHp = session.boss.Health;
                    initialPosition = session.motor.Position;
                    if (data.active)
                    {
                        Check(session.abilities.Runtime.Ready, id + " not ready on equip");
                        session.abilities.Activate();
                        probe.triggered = !session.abilities.Runtime.Ready;
                        if (id == "A14")
                        {
                            session.actions.Execute(PlayerActionKind.Attack, Vector2.right);
                        }
                        if (id == "A04")
                        {
                            session.motor.Impulse(new Vector2(0, session.Data.player.jumpSpeed), session.Data.player.coyoteTime);
                        }
                        if (id == "A05" || id == "A11")
                        {
                            probe.magicBlocked = session.abilities.Defend(new HitContext(0, "B05", DamageKind.Magic, 1));
                            probe.physicalBlocked = session.abilities.Defend(new HitContext(0, "B01", DamageKind.Physical, 1));
                        }
                    }
                    else
                    {
                        int before = session.effects.ActiveCount;
                        Vector3 scaleBefore = session.motor.transform.localScale;
                        session.abilities.Activate();
                        Check(before == session.effects.ActiveCount && scaleBefore == session.motor.transform.localScale, "Passive responds to skill: " + id);
                        var kind = id == "A02" || id == "A20" ? PlayerActionKind.Dash : id == "A08" ? PlayerActionKind.Jump : id == "A19" ? PlayerActionKind.Dive : PlayerActionKind.Attack;
                        if (id == "A19")
                        {
                            session.motor.ResetState(new Vector2(initialPosition.x, session.Data.rules.floorY + 2));
                        }
                        session.actions.Execute(kind, id == "A20" ? Vector2.down : Vector2.right);
                        probe.triggered = true;
                    }
                    probe.effectPeak = session.effects.ActiveCount;
                    deadline = TelemetryLog.GameTime + Mathf.Max(data.delay + data.duration, 2) + .2;
                    state = 1;
                    break;
                case 1:
                    probe.bossDamage = Mathf.Max(0, initialBossHp - session.boss.Health);
                    probe.displacement = Vector2.Distance(initialPosition, session.motor.Position);
                    probe.effectPeak = Mathf.Max(probe.effectPeak, session.effects.ActiveCount);
                    Check(probe.triggered, probe.id + " did not trigger.");
                    if (probe.id != "A11" && probe.id != "A20")
                    {
                        Check(probe.bossDamage > 0, probe.id + " did not damage the boss in its probe opportunity.");
                    }
                    Check(!session.Finished, "Ability probe unexpectedly ended run.");
                    if (index < 20)
                    {
                        state = 0;
                    }
                    else
                    {
                        index = 0;
                        Current.stage = "patterns";
                        state = 2;
                    }
                    deadline = TelemetryLog.GameTime + 1;
                    break;
                case 2:
                    if (!session.life.Alive)
                    {
                        return;
                    }
                    session.life.ForceAbility("A11");
                    session.abilities.Cleanup();
                    session.boss.SetHealth(session.Data.rules.bossHealth * .4f);
                    string patternId = "B" + (++index).ToString("00");
                    session.boss.ForcePattern(patternId);
                    Check(session.boss.PatternId == patternId, "Boss pattern factory missing " + patternId);
                    Check(session.effects.ActiveCount > 0, "Pattern has no warning/effect " + patternId);
                    Current.patterns.Add(patternId);
                    session.boss.enabled = true;
                    deadline = TelemetryLog.GameTime + .2;
                    state = index < 7 ? 2 : 3;
                    break;
                case 3:
                    if (!session.life.Alive)
                    {
                        return;
                    }
                    Current.stage = "death cleanup";
                    string cleanupId = cleanupIndex == 0 ? "A14" : cleanupIndex == 1 ? "A20" : "A02";
                    session.life.ForceAbility(cleanupId);
                    session.boss.SetHealth(session.Data.rules.bossHealth * .4f);
                    initialBossHp = session.boss.Health;
                    cleanupLifeId = session.life.LifeId;
                    if (cleanupId == "A14")
                    {
                        session.abilities.Activate();
                    }
                    else
                    {
                        session.actions.Execute(PlayerActionKind.Dash, Vector2.right);
                    }
                    session.effects.Spawn(session.motor.Position, Vector2.one, 10, 1,
                        new HitContext(cleanupLifeId, cleanupId, DamageKind.Magic, 1), true, true);
                    session.damage.Apply(false, new HitContext(cleanupLifeId, "debug", DamageKind.Self, session.life.MaxHealth, true) { fatal = true });
                    deadline = TelemetryLog.GameTime + session.Data.rules.respawnDelay + .15;
                    state = 8;
                    break;
                case 4:
                    Check(session.life.Remaining == 100 && session.life.Deaths == 0, "Restart did not restore 100 lives.");
                    Current.deathsObserved = Current.endEvents = 0;
                    Current.stage = "100 deaths";
                    state = 5;
                    break;
                case 5:
                    if (session.Finished)
                    {
                        Check(!session.Victory && session.life.Remaining == 0 && session.life.Deaths == 100, "Exhaustion result incorrect.");
                        Check(Current.deathsObserved == 100 && Current.endEvents == 1, "Death/end events duplicated or lost.");
                        Check(session.effects.ActiveCount == 0, "Effects leaked after exhaustion.");
                        Current.exhaustion = true;
                        Current.exhaustionDeaths = Current.deathsObserved;
                        Current.exhaustionEndEvents = Current.endEvents;
                        Current.stage = "restart for simultaneous victory";
                        Directory.CreateDirectory("Artifacts/POC");
                        ScreenCapture.CaptureScreenshot("Artifacts/POC/defeat.png");
                        deadline = TelemetryLog.GameTime + .4;
                        state = 10;
                    }
                    else if (session.life.Alive)
                    {
                        session.damage.Apply(false, new HitContext(session.life.LifeId, "debug", DamageKind.Self, session.life.MaxHealth, true) { fatal = true });
                        deadline = TelemetryLog.GameTime + .05;
                    }
                    break;
                case 6:
                    if (!session.life.Alive)
                    {
                        return;
                    }
                    session.life.ForceAbility("A16");
                    session.boss.SetHealth(1);
                    session.motor.ResetState(session.boss.transform.position);
                    Current.endEvents = 0;
                    session.abilities.Activate();
                    var bomb = session.abilities.Runtime.Data;
                    deadline = TelemetryLog.GameTime + bomb.delay + bomb.duration + .3;
                    state = 7;
                    break;
                case 7:
                    Check(session.Finished && session.Victory && session.boss.Health == 0, "A16 simultaneous victory failed.");
                    Check(Current.endEvents == 1, "Victory was emitted more than once.");
                    Current.simultaneousVictory = true;
                    Current.frameEnd = Time.frameCount;
                    Check(Current.frameEnd > Current.frameStart, "Player loop did not advance.");
                    Current.telemetry = TelemetryLog.FilePath;
                    Current.passed = true;
                    Current.stage = "complete";
                    Finish();
                    break;
                case 8:
                    Check(session.life.Alive && session.life.LifeId == cleanupLifeId + 1, "Cleanup respawn missing.");
                    Check(session.boss.Health == initialBossHp && session.boss.Phase == 2, "Boss HP/phase did not persist.");
                    Check(session.effects.ActiveCount == 0 && session.motor.transform.localScale == Vector3.one &&
                        !session.motor.hurtbox.isTrigger && !session.motor.Phasing && !session.motor.Invulnerable,
                        "State leaked across real death/respawn.");
                    Current.hpPersistent = true;
                    cleanupIndex++;
                    if (cleanupIndex < 3)
                    {
                        state = 3;
                    }
                    else
                    {
                        Current.cleanup = true;
                        Current.stage = "restart for 100 deaths";
                        session.Restart();
                        deadline = TelemetryLog.GameTime + 1;
                        state = 4;
                    }
                    break;
                case 10:
                    Check(session.Finished && session.life.Remaining == 0 && Current.endEvents == 1,
                        "Exhaustion spawned an extra life or end event.");
                    session.Restart();
                    deadline = TelemetryLog.GameTime + 1;
                    state = 6;
                    break;
            }
        }
        catch (Exception error)
        {
            Current.error = error.ToString();
            Current.passed = false;
            Finish();
            Debug.LogException(error);
        }
    }

    static void Finish()
    {
        Stop();
        Directory.CreateDirectory("Artifacts/POC");
        File.WriteAllText("Artifacts/POC/integration-results.json", JsonUtility.ToJson(Current, true));
        TelemetryLog.Flush();
    }

}

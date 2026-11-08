using UnityEngine;
using UnityEngine.SceneManagement;

// The authored scene service owns the entire run; only validated table data is global.
[DefaultExecutionOrder(-200)]
public sealed class ArenaSession : MonoBehaviour
{
    public PlayerMotor2D motor;
    public PlayerActionController actions;
    public AbilityController abilities;
    public LifeManager life;
    public BossController boss;
    public DamageResolver damage;
    public EffectSystem effects;
    public Transform spawn;
    public ArenaTableData Data { get; private set; }
    public bool Finished { get; private set; }
    public bool Victory { get; private set; }

    void Awake()
    {
        Data = TableManager.Instance.RequireValid().Arena;
        Finished = Victory = false;
    }

    void Start()
    {
        boss.Initialize();
        life.Initialize();
        GameEvents.RaiseRunStarted(gameObject.scene.name, TelemetryLog.GameTime);
    }

    void LateUpdate()
    {
        if (Finished)
        {
            return;
        }
        // Settle all hits in this frame before deciding: boss death always wins a tie.
        life.ResolvePendingDeath();
        if (boss.Health <= 0 || life.Remaining == 0)
        {
            Finished = true;
            Victory = boss.Health <= 0;
            abilities.Cleanup();
            effects.Clear();
            boss.ClearAttack();
            motor.body.linearVelocity = Vector2.zero;
            motor.body.simulated = false;
            var result = new GameEvents.BattleEndInfo(Victory, life.Deaths, life.Total,
                boss.Health, Data.rules.seed, TelemetryLog.GameTime);
            GameEvents.RaiseBattleEnded(result);
            GameEvents.RaiseRunEnded(gameObject.scene.name, result.time, Victory ? "victory" : "defeat");
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(TableManager.Instance.RestartSceneOf(gameObject.scene.name));
    }
}

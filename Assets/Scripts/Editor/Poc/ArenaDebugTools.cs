using System;
using UnityEditor;
using UnityEngine;

public sealed class ArenaDebugTools : EditorWindow
{
    int ability, pattern;
    float health;

    [MenuItem("Tools/POC/Debug Arena")]
    static void Open()
    {
        GetWindow<ArenaDebugTools>(false, "Arena Debug");
    }

    void OnGUI()
    {
        var session = SceneServices.Find<ArenaSession>(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        if (!EditorApplication.isPlaying || !session)
        {
            EditorGUILayout.HelpBox("ArenaPOC Play mode only", MessageType.Info);
            return;
        }
        ability = EditorGUILayout.IntSlider("Ability A01–A20", ability, 1, 20);
        if (GUILayout.Button("Equip ability"))
        {
            session.life.ForceAbility("A" + ability.ToString("00"));
        }
        pattern = EditorGUILayout.IntSlider("Pattern B01–B07", pattern, 1, 7);
        if (GUILayout.Button("Start pattern"))
        {
            session.boss.ForcePattern("B" + pattern.ToString("00"));
        }
        health = EditorGUILayout.FloatField("Boss HP", health);
        if (GUILayout.Button("Set boss HP"))
        {
            session.boss.SetHealth(health);
        }
        if (GUILayout.Button("Lose one life"))
        {
            session.damage.Apply(false, new HitContext(session.life.LifeId, "debug", DamageKind.Self, 1, true));
        }
        if (GUILayout.Button("Dump deck"))
        {
            Debug.Log(string.Join(",", session.life.Deck.cards));
        }
        if (GUILayout.Button("One feed sample"))
        {
            GameEvents.RaiseDeath(new GameEvents.DeathInfo(session.life.LifeId, session.life.Deaths, session.abilities.Id,
                "debug", "Self", session.boss.PatternId, 0, 0, session.Data.rules.seed, session.motor.Position, TelemetryLog.GameTime));
        }
    }
}

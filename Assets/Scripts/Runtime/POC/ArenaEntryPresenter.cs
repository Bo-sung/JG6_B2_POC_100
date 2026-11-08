using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ArenaEntryPresenter : MonoBehaviour
{
    public UnityEngine.UI.Button startButton;
    public TMPro.TMP_Text title, controls;

    void OnEnable()
    {
        if (startButton)
        {
            startButton.onClick.AddListener(Next);
        }
    }

    void OnDisable()
    {
        if (startButton)
        {
            startButton.onClick.RemoveListener(Next);
        }
    }

    void Start()
    {
        var data = TableManager.Instance.RequireValid().Arena;
        if (gameObject.scene.name == "Preload")
        {
            Next();
            return;
        }
        title.text = data.rules.title;
        controls.text = data.rules.controls;
        startButton.Select();
    }

    void Next()
    {
        SceneManager.LoadScene(TableManager.Instance.NextSceneOf(gameObject.scene.name));
    }
}

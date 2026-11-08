using TMPro;

public sealed class ArenaResultView : PanelBase
{
    public TMP_Text summary;
    public UnityEngine.UI.Button restart;

    public void ShowResult(GameEvents.BattleEndInfo _info)
    {
        summary.text = Presentation.Result(_info);
        Open();
        restart.Select();
    }
}

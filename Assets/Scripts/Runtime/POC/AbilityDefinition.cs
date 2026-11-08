using UnityEngine;

// Immutable identity only. Balance and presentation are resolved from the validated JSON table.
[CreateAssetMenu(menuName = "100 Lives/Ability")]
public sealed class AbilityDefinition : ScriptableObject
{
    public string id;
    public AbilityData Data => TableManager.Instance.Arena.abilities.Find(_entry => _entry.id == id);
}

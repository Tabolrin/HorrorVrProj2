using UnityEngine;

/// <summary>
/// ScriptableObject holding player stat values.
/// Create via Assets - Scriptable Objects - PlayerStats.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    public float MaxHp;
}
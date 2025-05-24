// Filename: PlayerBackgroundSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerBackground", menuName = "Project Dublin/Core/Player Background")]
public class PlayerBackgroundSO : ScriptableObject
{
    [Tooltip("The name of the player background, e.g., Ex-Cop, Street Urchin")]
    public string backgroundName;

    [TextArea(3, 5)]
    [Tooltip("A description of this background and its potential implications.")]
    public string description;
}
// Filename: AttributeSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttribute", menuName = "Project Dublin/Core/Attribute")]
public class AttributeSO : ScriptableObject
{
    [Tooltip("The display name of the attribute, e.g., Luck, Strength, Agility")]
    public string attributeName;

    [TextArea(3, 5)]
    [Tooltip("A brief description of what this attribute represents or influences.")]
    public string description;

    // Unlike skills which might go 1-100, attributes might have a smaller range (e.g., 1-10)
    // However, the SO itself just defines WHAT the attribute IS.
    // The PlayerProfileSO will store the player's VALUE for this attribute.
}
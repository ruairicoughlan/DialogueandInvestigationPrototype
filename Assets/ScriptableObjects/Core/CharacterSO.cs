// Filename: CharacterSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Project Dublin/Core/Character")]
public class CharacterSO : ScriptableObject
{
    [Tooltip("The display name of the character.")]
    public string characterName = "Character Name";

    [Tooltip("Portrait sprite for this character, used in dialogue UI.")]
    public Sprite characterPortrait;

    // We can add more character-specific persistent data here if needed later,
    // like default faction alignment, personality traits etc.
}
// Filename: SkillSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Project Dublin/Core/Skill")]
public class SkillSO : ScriptableObject
{
    [Tooltip("The display name of the skill, e.g., Charisma, Intimidation")]
    public string skillName;

    [TextArea(3, 5)]
    [Tooltip("A brief description of what this skill represents or does.")]
    public string description;
}
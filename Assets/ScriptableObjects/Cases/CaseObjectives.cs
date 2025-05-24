// Filename: CaseObjectiveSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewCaseObjective", menuName = "Project Dublin/Cases/Case Objective")]
public class CaseObjectiveSO : ScriptableObject
{
    [Tooltip("Unique identifier for this objective, e.g., Dewey_TalkToSerena. Must be unique within its Case.")]
    public string objectiveId;

    [TextArea(3, 5)]
    [Tooltip("Player-facing description of the objective, e.g., 'Find out what Serena knows about Dewey.'")]
    public string description;

    [Tooltip("Is this objective hidden from the player initially?")]
    public bool isHidden = false;

    // We can add more here later, like "isOptional" or rewards, if needed.
}
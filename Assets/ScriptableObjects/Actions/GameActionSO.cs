// Filename: GameActionSO.cs
using UnityEngine;

public abstract class GameActionSO : ScriptableObject
{
    [TextArea(2,4)]
    [Tooltip("Optional description of what this action does, for designer reference.")]
    public string developerDescription = "";

    /// <summary>
    /// Executes the action.
    /// </summary>
    /// <param name="executor">The GameObject that triggered this action, if any (e.g., DialogueManager, InvestigationManager).</param>
    /// <param name="playerProfile">Reference to the player's profile for context.</param>
    /// <param name="caseManager">Reference to the case manager to interact with cases.</param>
    public abstract void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager);
    // Note: We'll need to create a placeholder CaseManager script soon to avoid compile errors.
}
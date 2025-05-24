// Filename: ProgressCaseObjectiveActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressObjectiveAction", menuName = "Project Dublin/Actions/Progress Case Objective")]
public class ProgressCaseObjectiveActionSO : GameActionSO
{
    [Tooltip("The case this objective belongs to.")]
    public CaseSO targetCase;
    [Tooltip("The ID of the objective to progress/complete. Must match an objectiveId within the targetCase.")]
    public string objectiveIdToProgress;
    // Alternatively, you could use: public CaseObjectiveSO objectiveToProgress;
    // Using string ID is more robust if objective SOs are frequently changed/recreated.

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (targetCase == null || string.IsNullOrEmpty(objectiveIdToProgress) || caseManager == null)
        {
            Debug.LogWarning($"ProgressCaseObjectiveActionSO: Target case, objective ID, or CaseManager is null/empty. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing ProgressCaseObjectiveAction: {targetCase.caseName} - Objective: {objectiveIdToProgress}");
        caseManager.ProgressObjective(targetCase, objectiveIdToProgress);
    }
}
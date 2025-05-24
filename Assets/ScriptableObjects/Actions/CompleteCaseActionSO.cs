// Filename: CompleteCaseActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CompleteCaseAction", menuName = "Project Dublin/Actions/Complete Case")]
public class CompleteCaseActionSO : GameActionSO
{
    public CaseSO caseToComplete;

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (caseToComplete == null || caseManager == null)
        {
            Debug.LogWarning($"CompleteCaseActionSO: Case to complete or CaseManager is null. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing CompleteCaseAction: {caseToComplete.caseName}");
        caseManager.CompleteCase(caseToComplete);
    }
}
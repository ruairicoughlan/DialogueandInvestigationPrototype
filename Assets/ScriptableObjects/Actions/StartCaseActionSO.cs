// Filename: StartCaseActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "StartCaseAction", menuName = "Project Dublin/Actions/Start Case")]
public class StartCaseActionSO : GameActionSO
{
    public CaseSO caseToStart;

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (caseToStart == null || caseManager == null)
        {
            Debug.LogWarning($"StartCaseActionSO: Case to start or CaseManager is null. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing StartCaseAction: {caseToStart.caseName}");
        caseManager.StartCase(caseToStart);
    }
}
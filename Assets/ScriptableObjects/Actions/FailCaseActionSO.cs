// Filename: FailCaseActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "FailCaseAction", menuName = "Project Dublin/Actions/Fail Case")]
public class FailCaseActionSO : GameActionSO
{
    public CaseSO caseToFail;

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (caseToFail == null || caseManager == null)
        {
            Debug.LogWarning($"FailCaseActionSO: Case to fail or CaseManager is null. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing FailCaseAction: {caseToFail.caseName}");
        caseManager.FailCase(caseToFail);
    }
}
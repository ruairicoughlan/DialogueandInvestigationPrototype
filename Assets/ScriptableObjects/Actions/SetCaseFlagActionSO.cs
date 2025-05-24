// Filename: SetCaseFlagActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SetCaseFlagAction", menuName = "Project Dublin/Actions/Set Case Flag")]
public class SetCaseFlagActionSO : GameActionSO
{
    [Tooltip("The case whose flag will be set.")]
    public CaseSO targetCase;
    [Tooltip("The ID of the flag to set (must be one of the flags defined in the CaseSO's initialCaseFlags or a new one).")]
    public string flagId;
    public bool flagValue;

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (targetCase == null || string.IsNullOrEmpty(flagId) || caseManager == null)
        {
            Debug.LogWarning($"SetCaseFlagActionSO: Target case, flag ID, or CaseManager is null/empty. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing SetCaseFlagAction: {targetCase.caseName} - Flag: {flagId} = {flagValue}");
        caseManager.SetCaseFlagValue(targetCase, flagId, flagValue);
    }
}
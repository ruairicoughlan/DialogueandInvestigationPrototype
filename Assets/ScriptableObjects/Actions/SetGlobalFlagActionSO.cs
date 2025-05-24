// Filename: SetGlobalFlagActionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SetGlobalFlagAction", menuName = "Project Dublin/Actions/Set Global Flag")]
public class SetGlobalFlagActionSO : GameActionSO
{
    [Tooltip("The ID of the global flag to set on the PlayerProfile.")]
    public string globalFlagId;
    public bool flagValue;

    public override void Execute(GameObject executor, PlayerProfileSO playerProfile, CaseManager caseManager)
    {
        if (string.IsNullOrEmpty(globalFlagId) || playerProfile == null)
        {
            Debug.LogWarning($"SetGlobalFlagActionSO: Global flag ID or PlayerProfile is null/empty. Action not executed for SO: {name}");
            return;
        }
        Debug.Log($"Executing SetGlobalFlagAction: {globalFlagId} = {flagValue}");
        playerProfile.SetGlobalFlag(globalFlagId, flagValue);
    }
}
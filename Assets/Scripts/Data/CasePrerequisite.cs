// Filename: CasePrerequisite.cs
using UnityEngine; // Keep for [System.Serializable]

// No CreateAssetMenu attribute as this is not a ScriptableObject
[System.Serializable]
public class CasePrerequisite
{
    [Tooltip("The case that this prerequisite checks against.")]
    public CaseSO caseToCheck; // Reference the CaseSO directly

    [Tooltip("The required status for the specified case.")]
    public CaseManager.CaseStatus requiredStatus; // Using the enum from CaseManager
}
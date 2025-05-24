// Filename: CaseSO.cs
using UnityEngine;
using System.Collections.Generic;

// Helper class for initial case flags, editable in Inspector for CaseSO
[System.Serializable]
public class CaseFlagInit
{
    public string flagId;
    public bool initialValue;
}

[CreateAssetMenu(fileName = "NewCase", menuName = "Project Dublin/Cases/Case")]
public class CaseSO : ScriptableObject
{
    [Tooltip("Unique identifier for this case, e.g., case_001_dewey.")]
    public string caseId;

    [Tooltip("Player-facing name of the case.")]
    public string caseName = "New Case";

    [TextArea(3, 10)]
    [Tooltip("A general description or summary of the case.")]
    public string description;

    [Tooltip("List of objectives associated with this case.")]
    public List<CaseObjectiveSO> objectives;

    [Tooltip("Initial states for case-specific flags. These flags are local to this case.")]
    public List<CaseFlagInit> initialCaseFlags;

    // We could add other case-wide properties here, like:
    // - Prerequisite cases to be completed
    // - Rewards upon completion
    // - Associated location(s)
}
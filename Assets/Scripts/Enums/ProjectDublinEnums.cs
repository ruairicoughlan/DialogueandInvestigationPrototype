// Filename: ProjectDublinEnums.cs

// No "using UnityEngine;" or other inheritance needed if it *only* contains enums.

public enum DialogueSpeakerType
{
    Player,
    NPC
}

public enum DialogueTransitionState
{
    None,
    NextConversation,
    Fight,
    BeginInvestigation,
    ScopeLocation,
    Leave
}

// You can add other global enums here as your project grows, for example:
// public enum SkillCheckOutcome { Pending, Success, Failure }
// public enum CaseStatus { NotStarted, Active, Completed, Failed, Archived }
// Filename: DialogueNodeSO.cs
using UnityEngine; // Required for AudioClip, CreateAssetMenu, Tooltip, Range, TextArea
using System.Collections.Generic; // Required for List<>
using System.Linq; // Required for .Any() in OnValidate if you use it

// Ensure your enums are globally accessible (e.g., from a ProjectDublinEnums.cs file)
// Example of what should be in ProjectDublinEnums.cs (or similar):
// public enum DialogueSpeakerType { Player, NPC }
// public enum DialogueTransitionState { None, NextConversation, Fight, BeginInvestigation, ScopeLocation, Leave }
// public class CasePrerequisite { /* fields like CaseSO caseToCheck; CaseStatus requiredStatus; */ } // Ensure CasePrerequisite is defined
// public class GameActionSO : ScriptableObject { /* abstract Execute method */ } // Ensure GameActionSO base class is defined
// public class SkillSO : ScriptableObject { /* fields like skillName */ } // Ensure SkillSO is defined
// public class PlayerBackgroundSO : ScriptableObject { /* ... */ } // Ensure PlayerBackgroundSO is defined
// public class CharacterSO : ScriptableObject { /* ... */ } // Ensure CharacterSO is defined


[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Project Dublin/Dialogue/Dialogue Node")]
public class DialogueNodeSO : ScriptableObject
{
    [Header("Node Identification")]
    [Tooltip("Unique ID for this node (e.g., CharacterName_Location_Number). Used for linking and lookups.")]
    public string nodeID;

    [Header("Participants")]
    [Tooltip("Who is speaking this line?")]
    public DialogueSpeakerType speakerType = DialogueSpeakerType.NPC;

    [Tooltip("The CharacterSO of the primary speaker. If Player is speaking, this can be null. If NPC is speaking, assign the NPC.")]
    public CharacterSO primarySpeakerCharacter;

    [Tooltip("Other characters actively involved or listening to this specific line of dialogue.")]
    public List<CharacterSO> otherParticipants;

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    [Tooltip("The actual dialogue text or player choice text.")]
    public string dialogueText;

    [Header("Audio (VO)")]
    [Tooltip("Optional voice-over audio clip for this dialogue line.")]
    public AudioClip voiceOverClip;

    [Header("Display Conditions (for Player Options)")]
    [Tooltip("Does this node require previous specific nodes to be completed to appear as an option?")]
    public bool requiresPreviousNodesCompleted;

    [Tooltip("List of Node IDs that must have been completed for this option to appear.")]
    public List<string> requiredNodeCompletionIDs;

    [Tooltip("Does this node require the player to have a certain background to appear?")]
    public bool requiresPlayerBackground;

    [Tooltip("Which background is needed for this dialogue node to appear as an option?")]
    public PlayerBackgroundSO requiredPlayerBackgroundType;

    [Tooltip("Does this node require a global flag to be active for this option to appear?")]
    public bool requiresGlobalFlag;

    [Tooltip("ID of the global flag to check.")]
    public string requiredGlobalFlagID;
    // public bool requiredGlobalFlagState = true; // Optional: If you need to check for flag being false

    [Tooltip("List of case prerequisites for this node to appear as an option.")]
    public List<CasePrerequisite> casePrerequisites;


    [Header("Player Option Skill Check (New System)")]
    [Tooltip("Is this player option gated by a skill check with probabilistic success/failure, using the new display format?")]
    public bool isSkillCheckOption;

    [Tooltip("If IsSkillCheckOption is true, which skill is checked?")]
    public SkillSO skillCheckType;

    [Range(1, 100)]
    [Tooltip("If IsSkillCheckOption is true, what is the base difficulty for the skill check?")]
    public int skillCheckDifficulty;


    [Header("Node Behavior & Transitions")]
    [Tooltip("If this is a player choice: does its outcome (which next node to go to) depend on a success/fail roll (e.g., from a skill check)? If true, Next Node IDs should have 2 entries.")]
    public bool isOutcomeDeterminedBySkillCheck;

    [Tooltip("Is this player option considered a 'bad' option (can be highlighted by Charisma)?")]
    public bool isBadOption;

    [Tooltip("Does this node ITSELF lead to a different game state or a new conversation?")]
    public bool isTransitionNode;

    [Tooltip("If IsTransitionNode is true, which state/type of transition?")]
    public DialogueTransitionState transitionState = DialogueTransitionState.None;

    [Tooltip("Does this dialogue option's text get a [Transition Hint] like [Attack] or [Leave]? This is purely a UI hint.")]
    public bool addTransitionMarkerToText;


    [Header("Consequences & Next Steps")]
    [Tooltip("ID(s) of the next node(s). If IsOutcomeDeterminedBySkillCheck is true, first is success, second is fail. If NPC line or simple player choice, usually one. If NPC line presenting choices, list of player option NodeIDs.")]
    public List<string> nextNodeIDs;

    [Tooltip("Actions to perform when this node is selected/completed.")]
    public List<GameActionSO> actionsOnComplete;

    [Tooltip("Global flag ID to set when this node is completed/selected. Leave empty if no flag is set.")]
    public string setGlobalFlagOnComplete;


    // Helper method
    public bool IsPlayerChoice()
    {
        return speakerType == DialogueSpeakerType.Player;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(nodeID))
        {
            Debug.LogWarning($"DialogueNodeSO '{name}' (asset name) has an empty Node ID. Please assign a unique ID.", this);
        }

        if (speakerType == DialogueSpeakerType.NPC && primarySpeakerCharacter == null)
        {
             Debug.LogWarning($"DialogueNodeSO '{nodeID}' ({name}) is spoken by an NPC but has no Primary Speaker Character assigned.", this);
        }
        
        if (speakerType == DialogueSpeakerType.Player && primarySpeakerCharacter != null)
        {
             Debug.LogWarning($"DialogueNodeSO '{nodeID}' ({name}) is spoken by the Player but has a Primary Speaker Character assigned. This should usually be null for player lines, as the player's avatar is taken from PlayerProfileSO.", this);
        }

        if (IsPlayerChoice())
        {
            if (isSkillCheckOption) // Checks for the new skill check system
            {
                if (skillCheckType == null)
                    Debug.LogWarning($"Node '{nodeID}' ({name}) is a Skill Check Option but no Skill Check Type (SkillSO) is assigned.", this);
                // skillCheckDifficulty has [Range(1,100)], so negative/zero check is mostly for if Range is removed.
                if (skillCheckDifficulty <= 0) 
                    Debug.LogWarning($"Node '{nodeID}' ({name}) is a Skill Check Option but Skill Check Difficulty is not positive.", this);
                
                // If it's a new skill check option, it should almost always determine its outcome by that check.
                if (!isOutcomeDeterminedBySkillCheck)
                    Debug.LogWarning($"Node '{nodeID}' ({name}) is a Skill Check Option but 'Is Outcome Determined By Skill Check' is FALSE. This is usually true for skill check options that lead to success/fail branches.", this);
                
                if (isOutcomeDeterminedBySkillCheck && (nextNodeIDs == null || nextNodeIDs.Count != 2))
                    Debug.LogWarning($"Node '{nodeID}' ({name}) is a skill check option that determines outcome but does not have exactly 2 Next Node IDs (for success/fail). Found: {(nextNodeIDs != null ? nextNodeIDs.Count.ToString() : "null list")}", this);
            }
            else // Not using the new "isSkillCheckOption" flag
            {
                // If it's *not* a new skill check option, but *is* determining outcome by skill (old way, or hidden),
                // it still needs 2 next nodes.
                if (isOutcomeDeterminedBySkillCheck && (nextNodeIDs == null || nextNodeIDs.Count != 2))
                {
                    Debug.LogWarning($"Node '{nodeID}' ({name}) has 'Is Outcome Determined By Skill Check' TRUE (but is NOT a new 'Is Skill Check Option'), but does not have 2 Next Node IDs. Found: {(nextNodeIDs != null ? nextNodeIDs.Count.ToString() : "null list")}", this);
                }
            }
        }
        else // NPC Speaker
        {
            if (isSkillCheckOption)
                 Debug.LogWarning($"Node '{nodeID}' ({name}) is an NPC line but 'Is Skill Check Option' is TRUE. This flag is for player choices.", this);
            
            // An NPC line typically doesn't determine its own outcome via a skill check leading to different branches for itself.
            if (isOutcomeDeterminedBySkillCheck)
                Debug.LogWarning($"Node '{nodeID}' ({name}) is an NPC line but 'Is Outcome Determined By Skill Check' is TRUE. This is generally for player choices that follow it.", this);

            if (isBadOption)
                Debug.LogWarning($"Node '{nodeID}' ({name}) is an NPC line but marked as 'Is Bad Option'. This is for player choices.", this);
        }

        if (isTransitionNode && transitionState == DialogueTransitionState.None)
        {
            Debug.LogWarning($"Node '{nodeID}' ({name}) is marked as a transition node, but Transition State is 'None'.", this);
        }
        
        if (addTransitionMarkerToText && speakerType == DialogueSpeakerType.NPC)
        {
            Debug.LogWarning($"Node '{nodeID}' ({name}) is an NPC line but has 'Add Transition Marker To Text' enabled. This is usually for player choices.", this);
        }

        // Check that primarySpeakerCharacter is not in otherParticipants
        if (primarySpeakerCharacter != null && otherParticipants != null && otherParticipants.Contains(primarySpeakerCharacter))
        {
            Debug.LogWarning($"DialogueNodeSO '{nodeID}' ({name}): Primary Speaker Character '{primarySpeakerCharacter.name}' should not also be listed in 'Other Participants'.", this);
        }
    }
#endif
}
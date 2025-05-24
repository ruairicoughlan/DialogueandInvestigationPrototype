// Filename: DialogueManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for .Any()

[RequireComponent(typeof(AudioSource))] // Ensures AudioSource is on this GameObject
public class DialogueManager : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerProfileSO playerProfile;
    public CaseManager caseManager;
    public DialogueUI dialogueUI;
    [Tooltip("Assign the 'Luck' AttributeSO here for skill check calculations.")]
    public AttributeSO luckAttribute;

    [Header("Dialogue State")]
    private DialogueConversationSO currentConversation;
    private DialogueNodeSO currentNode; // Represents the node whose line is CURRENTLY displayed or was just chosen
    private CharacterSO currentNpcSpeaker; // The main NPC of the conversation
    private HashSet<string> completedNodesInThisSession = new HashSet<string>();

    private AudioSource voiceOverAudioSource;
    private DialogueNodeSO _pendingChoiceNodeForResponse; // Stores the player's choice node that needs an NPC response

    public static DialogueManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        voiceOverAudioSource = GetComponent<AudioSource>();
        voiceOverAudioSource.playOnAwake = false;
    }

    void Start()
    {
        if (playerProfile == null) Debug.LogError("DialogueManager: PlayerProfileSO not assigned!");
        if (caseManager == null) Debug.LogError("DialogueManager: CaseManager not assigned!");
        if (dialogueUI == null) Debug.LogError("DialogueManager: DialogueUI not assigned!");
        if (luckAttribute == null) Debug.LogError("DialogueManager: Luck AttributeSO not assigned! Skill checks involving luck will default to 0 luck.");
        
        if (dialogueUI != null) dialogueUI.HideDialogue();
    }

    public void StartConversation(DialogueConversationSO conversation, CharacterSO npcEngagedWith)
    {
        if (conversation == null || string.IsNullOrEmpty(conversation.startNodeID))
        {
            Debug.LogError("DialogueManager: Cannot start conversation. Conversation or startNodeID is null/empty.");
            return;
        }

        Debug.Log($"Starting conversation: {conversation.name} with NPC: {(npcEngagedWith != null ? npcEngagedWith.characterName : "N/A")}");

        currentConversation = conversation;
        currentNpcSpeaker = npcEngagedWith;
        completedNodesInThisSession.Clear();
        _pendingChoiceNodeForResponse = null; // Clear any pending response from previous interactions
        
        if (currentConversation.allNodesInConversation == null || currentConversation.allNodesInConversation.Count == 0) {
            Debug.LogError($"Conversation '{currentConversation.name}' has no nodes defined!");
            EndConversation();
            return;
        }
        currentConversation.InitializeLookup(); 

        StopVoiceOver(); 

        DialogueNodeSO startNode = currentConversation.GetNodeByID(conversation.startNodeID);
        if (startNode == null)
        {
            Debug.LogError($"DialogueManager: Start node '{conversation.startNodeID}' not found in conversation '{conversation.name}'.");
            currentConversation = null;
            return;
        }

        if (dialogueUI != null) dialogueUI.ShowDialogue();
        ProcessNode(startNode);
    }

    private void ProcessNode(DialogueNodeSO node)
    {
        if (node == null)
        {
            Debug.LogWarning("DialogueManager: Tried to process a null node.");
            EndConversation();
            return;
        }

        StopVoiceOver(); 

        currentNode = node; // This node's line is about to be displayed
        completedNodesInThisSession.Add(node.nodeID);
        Debug.Log($"Processing Node: {node.nodeID} ('{node.dialogueText.Substring(0, Mathf.Min(node.dialogueText.Length, 30))}...') - Type: {node.speakerType}");

        ExecuteNodeActions(node);
        if (!string.IsNullOrEmpty(node.setGlobalFlagOnComplete))
        {
            playerProfile.SetGlobalFlag(node.setGlobalFlagOnComplete, true);
        }

        PlayVoiceOver(node.voiceOverClip); 

        if (node.isTransitionNode)
        {
            HandleTransition(node.transitionState);
            return;
        }

        if (node.speakerType == DialogueSpeakerType.NPC)
        {
            DisplayNpcLine(node);
        }
        else // Player node (e.g., a "thought" or an auto-progressing player line NOT chosen from options)
        {
            if (dialogueUI != null)
            {
                dialogueUI.DisplayPlayerChoiceLine(playerProfile.chosenCharacterAvatar, node.dialogueText);
            }
            
            // After a player "thought" or auto-line, game waits for a click to continue
            // OnContinueClicked will then determine how to proceed based on this player node's nextNodeIDs
            _pendingChoiceNodeForResponse = currentNode; // Store this player node to process its next steps on click
                                                        // This ensures ProcessNpcResponseAfterPlayerChoice works even for non-choice player lines
            
            // If this player "thought" node has no next nodes and is not a transition,
            // OnContinueClicked (via ProcessNpcResponseAfterPlayerChoice) will lead to EndConversation.
        }
    }
    
    private void CheckEndOfPlayerThought() { // This method might be obsolete if player thoughts also wait for click
        if (currentNode != null && currentNode.speakerType == DialogueSpeakerType.Player && 
            (currentNode.nextNodeIDs == null || currentNode.nextNodeIDs.Count == 0) && 
            !currentNode.isTransitionNode) {
            EndConversation(); 
        }
    }

    private void PlayVoiceOver(AudioClip clip)
    {
        if (voiceOverAudioSource != null && clip != null)
        {
            voiceOverAudioSource.clip = clip;
            voiceOverAudioSource.Play();
        }
    }

    private void StopVoiceOver()
    {
        if (voiceOverAudioSource != null && voiceOverAudioSource.isPlaying)
        {
            voiceOverAudioSource.Stop();
            voiceOverAudioSource.clip = null; 
        }
    }

    private void DisplayNpcLine(DialogueNodeSO npcNode)
    {
        CharacterSO speaker = npcNode.primarySpeakerCharacter != null ? npcNode.primarySpeakerCharacter : currentNpcSpeaker;
        List<DialogueNodeSO> playerChoices = GetAvailablePlayerChoices(npcNode.nextNodeIDs);
        Debug.Log($"DIALOGUE_DEBUG: DisplayNpcLine (for node '{npcNode.nodeID}') - Found {playerChoices.Count} available player choices to pass to UI.");
        if (dialogueUI != null)
        {
            dialogueUI.DisplayNpcLine(speaker, npcNode.dialogueText, playerChoices, currentNpcSpeaker);
        }
    }

    public List<DialogueNodeSO> GetAvailablePlayerChoices(List<string> choiceNodeIDs)
    {
        List<DialogueNodeSO> availableChoices = new List<DialogueNodeSO>();
        if (currentConversation == null || choiceNodeIDs == null || choiceNodeIDs.Count == 0) {
            Debug.Log("DIALOGUE_DEBUG: GetAvailablePlayerChoices - No choiceNodeIDs provided or no current conversation.");
            return availableChoices;
        }
        Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Processing {choiceNodeIDs.Count} potential choice IDs from current NPC node '{currentNode?.nodeID}'.");

        foreach (string choiceNodeID_string in choiceNodeIDs) {
            if (string.IsNullOrEmpty(choiceNodeID_string)) {
                Debug.LogWarning("DIALOGUE_DEBUG: GetAvailablePlayerChoices - Encountered an empty or null choiceNodeID string.");
                continue; 
            }
            DialogueNodeSO choiceNode = currentConversation.GetNodeByID(choiceNodeID_string);
            if (choiceNode != null) {
                Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Found node asset for ID '{choiceNodeID_string}'. Node's actual ID: '{choiceNode.nodeID}', SpeakerType: {choiceNode.speakerType}");
                if (choiceNode.speakerType == DialogueSpeakerType.Player) {
                    if (IsPlayerChoiceAvailable(choiceNode)) {
                        Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Choice '{choiceNodeID_string}' IS AVAILABLE. Adding to list.");
                        availableChoices.Add(choiceNode);
                    } else {
                        Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Choice '{choiceNodeID_string}' conditions NOT met.");
                    }
                } else {
                    Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Node '{choiceNodeID_string}' is NOT Player type. Actual: {choiceNode.speakerType}.");
                }
            } else {
                Debug.LogWarning($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Node with ID '{choiceNodeID_string}' NOT FOUND in conversation '{currentConversation.name}'.");
            }
        }
        Debug.Log($"DIALOGUE_DEBUG: GetAvailablePlayerChoices - Returning {availableChoices.Count} available choices.");
        return availableChoices;
    }
    
    public bool IsPlayerChoiceAvailable(DialogueNodeSO choiceNode)
    {
        if (playerProfile == null || caseManager == null || choiceNode == null) return false; 
        if (choiceNode.requiresPreviousNodesCompleted) { /* ... */ } // Keep existing condition checks
        if (choiceNode.requiresPlayerBackground && (playerProfile.playerBackground != choiceNode.requiredPlayerBackgroundType)) { Debug.Log($"DIALOGUE_DEBUG: IsPlayerChoiceAvailable for '{choiceNode.nodeID}': FAILED requiresPlayerBackground."); return false; }
        if (choiceNode.requiresGlobalFlag && (string.IsNullOrEmpty(choiceNode.requiredGlobalFlagID) || !playerProfile.HasGlobalFlag(choiceNode.requiredGlobalFlagID))) { Debug.Log($"DIALOGUE_DEBUG: IsPlayerChoiceAvailable for '{choiceNode.nodeID}': FAILED requiresGlobalFlag."); return false; }
        if (choiceNode.casePrerequisites != null) { foreach (var prereq in choiceNode.casePrerequisites) { if (prereq.caseToCheck == null) continue; if (caseManager.GetCaseStatus(prereq.caseToCheck) != prereq.requiredStatus) { Debug.Log($"DIALOGUE_DEBUG: IsPlayerChoiceAvailable for '{choiceNode.nodeID}': FAILED casePrerequisite."); return false; } } }
        return true;
    }

    public void OnPlayerChoiceSelected(DialogueNodeSO choiceNode)
    {
        StopVoiceOver(); 

        if (choiceNode == null || choiceNode.speakerType != DialogueSpeakerType.Player)
        {
            Debug.LogError("DialogueManager: Invalid player choice selected.");
            return;
        }

        Debug.Log($"Player selected choice: {choiceNode.nodeID} ('{choiceNode.dialogueText.Substring(0, Mathf.Min(choiceNode.dialogueText.Length, 30))}...')");
        
        currentNode = choiceNode; 
        completedNodesInThisSession.Add(choiceNode.nodeID);

        PlayVoiceOver(choiceNode.voiceOverClip); 

        if (dialogueUI != null)
        {
            dialogueUI.DisplayPlayerChoiceLine(playerProfile.chosenCharacterAvatar, choiceNode.dialogueText);
        }

        ExecuteNodeActions(choiceNode);
        if (!string.IsNullOrEmpty(choiceNode.setGlobalFlagOnComplete))
        {
            playerProfile.SetGlobalFlag(choiceNode.setGlobalFlagOnComplete, true);
        }
        
        // Store the player's choice that was just displayed. OnContinueClicked will use this.
        _pendingChoiceNodeForResponse = currentNode; 

        if (choiceNode.isTransitionNode) // If player's choice itself is a direct transition
        {
            HandleTransition(choiceNode.transitionState);
            // No further progression in this dialogue branch if it's a direct transition.
        }
        // Otherwise, DialogueUI is now showing player's line. Awaiting click handled by OnContinueClicked.
    }
    
    public void OnContinueClicked() 
    {
        StopVoiceOver(); 

        if (currentNode == null) {
            Debug.LogWarning("OnContinueClicked: currentNode is null. Ending conversation.");
            EndConversation();
            return;
        }

        Debug.Log($"DIALOGUE_DEBUG: OnContinueClicked - Current node is '{currentNode.nodeID}', speaker type: {currentNode.speakerType}");

        if (currentNode.speakerType == DialogueSpeakerType.Player)
        {
            Debug.Log("DIALOGUE_DEBUG: OnContinueClicked - Player previously spoke. Processing NPC response.");
            // _pendingChoiceNodeForResponse should be the player's line (which is also currentNode here)
            ProcessNpcResponseAfterPlayerChoice(); 
        }
        else if (currentNode.speakerType == DialogueSpeakerType.NPC)
        {
            Debug.Log("DIALOGUE_DEBUG: OnContinueClicked - NPC previously spoke. Advancing NPC line or ending.");
            bool hadPotentialChoices = currentNode.nextNodeIDs != null && 
                                      currentNode.nextNodeIDs.Any(id => currentConversation.GetNodeByID(id)?.speakerType == DialogueSpeakerType.Player);
            
            if (hadPotentialChoices) {
                List<DialogueNodeSO> availablePlayerChoices = GetAvailablePlayerChoices(currentNode.nextNodeIDs);
                if (availablePlayerChoices.Count > 0) {
                    Debug.LogWarning("DialogueManager: OnContinueClicked for NPC line, but available choices exist. Player should click a choice, not the panel.");
                    return; 
                }
            }

            if (currentNode.nextNodeIDs != null && currentNode.nextNodeIDs.Count == 1)
            {
                DialogueNodeSO nextNode = currentConversation.GetNodeByID(currentNode.nextNodeIDs[0]);
                ProcessNode(nextNode);
            }
            else if (currentNode.nextNodeIDs == null || currentNode.nextNodeIDs.Count == 0)
            {
                EndConversation();
            }
            else 
            {
                bool foundNextNpcLine = false;
                foreach(var nextNodeId in currentNode.nextNodeIDs)
                {
                    DialogueNodeSO nextNode = currentConversation.GetNodeByID(nextNodeId);
                    if (nextNode != null && nextNode.speakerType == DialogueSpeakerType.NPC) 
                    {
                        ProcessNode(nextNode);
                        foundNextNpcLine = true;
                        break;
                    }
                }
                if (!foundNextNpcLine) EndConversation(); 
            }
        }
    }

    private void ProcessNpcResponseAfterPlayerChoice()
    {
        DialogueNodeSO choiceNodeThatLedHere = _pendingChoiceNodeForResponse; 
        
        if (choiceNodeThatLedHere == null) { 
            Debug.LogError("ProcessNpcResponseAfterPlayerChoice: _pendingChoiceNodeForResponse (player's chosen line) was null!");
            EndConversation();
            return;
        }
        // Defensive check, though if flow is correct, choiceNodeThatLedHere should be the current player node
        if (choiceNodeThatLedHere.speakerType != DialogueSpeakerType.Player) {
             Debug.LogError($"ProcessNpcResponseAfterPlayerChoice: Expected _pendingChoiceNodeForResponse to be a Player node, but it was '{choiceNodeThatLedHere.speakerType}'. Node ID: '{choiceNodeThatLedHere.nodeID}'");
             EndConversation();
             return;
        }

        Debug.Log($"DIALOGUE_DEBUG: ProcessNpcResponseAfterPlayerChoice - Processing response to player's choice '{choiceNodeThatLedHere.nodeID}'");
        DialogueNodeSO nextNpcResponseNode = null;

        if (choiceNodeThatLedHere.isSkillCheckOption && choiceNodeThatLedHere.isOutcomeDeterminedBySkillCheck)
        {
            if (choiceNodeThatLedHere.skillCheckType == null) {
                Debug.LogError($"Node {choiceNodeThatLedHere.nodeID} is a skill check option but skillCheckType is null."); EndConversation(); return;
            }
            bool success = PerformSkillCheck(choiceNodeThatLedHere.skillCheckType, choiceNodeThatLedHere.skillCheckDifficulty);
            if (choiceNodeThatLedHere.nextNodeIDs != null && choiceNodeThatLedHere.nextNodeIDs.Count == 2) {
                nextNpcResponseNode = currentConversation.GetNodeByID(success ? choiceNodeThatLedHere.nextNodeIDs[0] : choiceNodeThatLedHere.nextNodeIDs[1]);
            } else {
                Debug.LogError($"Player choice '{choiceNodeThatLedHere.nodeID}' (skill check outcome) lacks 2 nextNodeIDs."); EndConversation(); return;
            }
        }
        else 
        {
            if (choiceNodeThatLedHere.nextNodeIDs != null && choiceNodeThatLedHere.nextNodeIDs.Count > 0)
            {
                nextNpcResponseNode = currentConversation.GetNodeByID(choiceNodeThatLedHere.nextNodeIDs[0]);
            }
            else if (!choiceNodeThatLedHere.isTransitionNode) 
            {
                 Debug.Log($"Player choice '{choiceNodeThatLedHere.nodeID}' has no next nodes and is not a transition. Ending branch.");
                 EndConversation();
                 return;
            }
        }

        _pendingChoiceNodeForResponse = null; // Clear after use

        if (nextNpcResponseNode != null)
        {
            ProcessNode(nextNpcResponseNode); 
        }
        else if (!choiceNodeThatLedHere.isTransitionNode) 
        {
            Debug.Log($"Player choice '{choiceNodeThatLedHere.nodeID}' led to a null next NPC response node and was not a transition. Ending branch.");
            EndConversation();
        }
    }

    public bool PerformSkillCheck(SkillSO skill, int difficulty)
    {
        if (skill == null || playerProfile == null) {
            Debug.LogError("PerformSkillCheck: SkillSO or PlayerProfileSO is null!");
            return false; 
        }
        
        int playerSkillValue = playerProfile.GetSkillValue(skill);
        int playerLuckValue = 0; 
        if (luckAttribute != null) {
            playerLuckValue = playerProfile.GetAttributeValue(luckAttribute);
        }
        
        if (playerSkillValue >= difficulty) {
            Debug.Log($"Skill Check: {skill.skillName} ({playerSkillValue}/{difficulty}). Auto-SUCCESS.");
            return true;
        }
        if (difficulty <= 0) {
             Debug.LogWarning($"Skill Check: {skill.skillName} ({playerSkillValue}/{difficulty}). Invalid difficulty (<=0). Assuming auto-SUCCESS.");
             return true;
        }
        
        float baseSuccessChance = (float)Mathf.Max(0, playerSkillValue) / difficulty;
        baseSuccessChance = Mathf.Clamp01(baseSuccessChance);
        float finalSuccessChance = baseSuccessChance;
        string luckDebugInfo = "";

        if (playerLuckValue > 0) {
            float luckTriggerChance = Mathf.Clamp01(playerLuckValue * 0.05f); 
            float luckRollForTrigger = Random.value; 
            if (luckRollForTrigger < luckTriggerChance) { 
                float luckBonusToAdd = playerLuckValue * 0.02f; 
                finalSuccessChance += luckBonusToAdd;
                finalSuccessChance = Mathf.Clamp01(finalSuccessChance); 
                luckDebugInfo = $"LUCK TRIGGERED! (Luck: {playerLuckValue}, Trigger Roll: {luckRollForTrigger:F2} < {luckTriggerChance:F2}, Bonus: +{luckBonusToAdd * 100:F0}%).";
            } else {
                luckDebugInfo = $"Luck (Luck: {playerLuckValue}) did not trigger (Trigger Roll: {luckRollForTrigger:F2} >= {luckTriggerChance:F2}).";
            }
        } else {
            luckDebugInfo = (luckAttribute == null) ? "Luck N/A (AttributeSO not assigned)." : "No player luck.";
        }

        float outcomeRoll = Random.value; 
        bool actualSuccess = outcomeRoll < finalSuccessChance;
        Debug.Log($"Skill Check: {skill.skillName} ({playerSkillValue}/{difficulty}). BaseChance: {baseSuccessChance * 100:F0}%. {luckDebugInfo} FinalChance: {finalSuccessChance * 100:F0}%. OutcomeRoll: {outcomeRoll:F2}. Result: {(actualSuccess ? "SUCCESS" : "FAIL")}");
        return actualSuccess;
    }

    public struct SkillCheckDisplayInfo { public int playerSkill; public int requiredDifficulty; public int displayChance; public string skillName; }
    public SkillCheckDisplayInfo GetSkillCheckDisplayInfo(SkillSO skill, int difficulty)
    {
        if (skill == null || playerProfile == null) {
            Debug.LogError("GetSkillCheckDisplayInfo: SkillSO or PlayerProfileSO is null.");
            return new SkillCheckDisplayInfo { playerSkill = 0, requiredDifficulty = difficulty, displayChance = 0, skillName = "ERROR"};
        }
        int pSkill = playerProfile.GetSkillValue(skill);
        int baseDisplayChance;
        if (difficulty <= 0) { baseDisplayChance = 100; } 
        else if (pSkill >= difficulty) { baseDisplayChance = 100; } 
        else if (pSkill < 0) { baseDisplayChance = 0; } 
        else { baseDisplayChance = Mathf.RoundToInt(((float)pSkill / difficulty) * 100f); }
        return new SkillCheckDisplayInfo { playerSkill = pSkill, requiredDifficulty = difficulty, displayChance = Mathf.Clamp(baseDisplayChance, 0, 100), skillName = skill.skillName };
    }

    public bool ShouldHighlightBadOption(DialogueNodeSO choiceNode) { /* ... (same as before) ... */ 
        if (!choiceNode.isBadOption || playerProfile == null) return false;
        SkillSO charismaSkill = GetCharismaSkill();
        if (charismaSkill == null) { return false; } 
        int charismaValue = playerProfile.GetSkillValue(charismaSkill);
        if (charismaValue >= 100) return true;
        if (charismaValue <= 0) return false;
        return Random.value < (charismaValue / 100.0f);
    }
    private SkillSO GetCharismaSkill() { /* ... (same as before) ... */ 
        if(playerProfile == null || playerProfile.skills == null) return null;
        foreach (var skillValue in playerProfile.skills) {
            if (skillValue.skill != null && skillValue.skill.skillName.Equals("Charisma", System.StringComparison.OrdinalIgnoreCase)) return skillValue.skill;
        }
        return null;
    }
    private void ExecuteNodeActions(DialogueNodeSO node) { /* ... (same as before) ... */ 
        if (node.actionsOnComplete != null) {
            foreach (GameActionSO action in node.actionsOnComplete) {
                if (action != null) action.Execute(this.gameObject, playerProfile, caseManager);
            }
        }
    }
    private void HandleTransition(DialogueTransitionState transitionState) { /* ... (same as before, ensure StopVoiceOver is called) ... */ 
        Debug.Log($"Transitioning to: {transitionState}");
        StopVoiceOver(); 
        if (dialogueUI != null) dialogueUI.HideDialogue();
        currentConversation = null; currentNode = null; _pendingChoiceNodeForResponse = null;
    }
    public void EndConversation() { /* ... (same as before, ensure StopVoiceOver and _pendingChoiceNodeForResponse = null) ... */ 
        Debug.Log("Ending conversation.");
        StopVoiceOver(); 
        if (dialogueUI != null) dialogueUI.HideDialogue();
        currentConversation = null; currentNode = null; currentNpcSpeaker = null; _pendingChoiceNodeForResponse = null;
    }
    public CharacterSO GetCurrentNpcPartner() => currentNpcSpeaker;
}
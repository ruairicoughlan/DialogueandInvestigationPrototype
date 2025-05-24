// Filename: TestDialogueTrigger.cs
using UnityEngine;

public class TestDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue To Trigger")]
    [Tooltip("Assign the DialogueConversationSO asset you want to test.")]
    public DialogueConversationSO conversationToStart;

    [Tooltip("Assign the CharacterSO of the NPC the player will be talking to.")]
    public CharacterSO npcPartner;

    [Header("Trigger Key")]
    public KeyCode triggerKey = KeyCode.Space; // Press Spacebar to start dialogue

    private DialogueManager dialogueManager;
    private bool dialogueActive = false; // To prevent re-triggering while active

    void Start()
    {
        // Find the DialogueManager in the scene
        dialogueManager = DialogueManager.Instance;

        if (dialogueManager == null)
        {
            Debug.LogError("TestDialogueTrigger: DialogueManager not found in the scene! Make sure it exists and is active.", this);
            enabled = false; // Disable this script if DialogueManager is missing
            return;
        }

        if (conversationToStart == null)
        {
            Debug.LogWarning("TestDialogueTrigger: 'Conversation To Start' is not assigned in the Inspector.", this);
        }
        if (npcPartner == null)
        {
            Debug.LogWarning("TestDialogueTrigger: 'NPC Partner' is not assigned in the Inspector.", this);
        }
    }

    void Update()
    {
        // Check if the DialogueManager's UI is currently active
        // We need a way to ask DialogueManager or DialogueUI if dialogue is ongoing.
        // For now, let's use our local 'dialogueActive' flag.
        // A better way would be: if (dialogueManager.IsConversationActive()) { ... }
        
        if (Input.GetKeyDown(triggerKey))
        {
            // Simple check: if DialogueUI's main panel is active, assume dialogue is running.
            // This isn't perfect but works for a basic test.
            bool isDialogueSystemActive = dialogueManager.dialogueUI != null && dialogueManager.dialogueUI.dialoguePanelMain.activeInHierarchy;

            if (!isDialogueSystemActive) // Only trigger if no dialogue is currently running
            {
                if (dialogueManager != null && conversationToStart != null && npcPartner != null)
                {
                    Debug.Log($"TestDialogueTrigger: Attempting to start conversation '{conversationToStart.name}' with '{npcPartner.name}'.");
                    dialogueManager.StartConversation(conversationToStart, npcPartner);
                    // dialogueActive = true; // Set our local flag (though checking UI state is better)
                }
                else
                {
                    Debug.LogError("TestDialogueTrigger: Cannot start dialogue. Ensure DialogueManager, Conversation, and NPC Partner are assigned and valid.", this);
                }
            }
            else
            {
                Debug.Log("TestDialogueTrigger: Dialogue is already active. Not starting new conversation.");
            }
        }

        // This is a very basic way to reset the local flag.
        // A more robust system would involve events or a state check from DialogueManager.
        // if (dialogueActive && (dialogueManager.dialogueUI == null || !dialogueManager.dialogueUI.dialoguePanelMain.activeInHierarchy))
        // {
        //     dialogueActive = false;
        // }
    }
}
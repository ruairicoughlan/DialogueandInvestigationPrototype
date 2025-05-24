// Filename: DialogueUI.cs
using UnityEngine;
using UnityEngine.UI; // Required for Image, Button, ScrollRect
using System.Collections.Generic;
using TMPro;          // For TextMeshProUGUI (if you're using it)
using UnityEngine.EventSystems; // Required for IPointerClickHandler

public class DialogueUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Main Panel (for click detection)")]
    [Tooltip("The root panel that dialogue lives on. Assign your main dialogue panel here.")]
    public GameObject dialoguePanelMain;

    [Header("NPC Dialogue Elements")]
    public GameObject npcDialogueGroup;
    public Image npcPortraitImage;
    public GameObject npcSpeechBubbleBackground; // The visual bubble for NPC
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI npcDialogueText;

    [Header("Player Dialogue Elements")]
    public GameObject playerDialogueGroup;
    public Image playerPortraitImage;
    public GameObject playerSpeechBubbleBackground; // The visual bubble for Player
    public TextMeshProUGUI playerDialogueText;

    [Header("Player Choice Elements")]
    public Transform choicesParent;
    public GameObject choiceButtonPrefab;
    public ScrollRect choicesScrollRect;

    private List<GameObject> currentChoiceButtonInstances = new List<GameObject>();
    
    private DialogueManager dialogueManager; 
    private PlayerProfileSO playerProfile;

    private bool isDisplayingChoices = false;
    private bool isInitialized = false; 

    void Start()
    {
        Initialize();
    }

    void Initialize() 
    {
        if (isInitialized) return; 

        dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            playerProfile = dialogueManager.playerProfile; 
        }
        else
        {
            Debug.LogError("DialogueUI: DialogueManager.Instance not found in Start! Ensure it's in the scene and initialized.", this);
            enabled = false; 
            return;
        }

        if (playerProfile == null && dialogueManager != null) 
        {
             Debug.LogError("DialogueUI: DialogueManager.Instance.playerProfile is null! Ensure PlayerProfileSO is assigned.", this);
             enabled = false;
             return;
        }

        // Setup for the main dialogue panel (for click-to-continue and background)
        if (dialoguePanelMain != null)
        {
            Image panelImage = dialoguePanelMain.GetComponent<Image>();
            if (panelImage == null)
            {
                // Add an Image component if one doesn't exist, primarily for raycast target.
                // If you intend this panel to have a visible background, ensure it has an Image component
                // and a Source Image assigned in the Inspector with desired Color/Alpha.
                panelImage = dialoguePanelMain.AddComponent<Image>();
                Debug.LogWarning("DialogueUI: Added Image component to dialoguePanelMain. If it's for background, assign Source Image and set Color/Alpha in Inspector. If just for clicks, make it transparent.", dialoguePanelMain);
                panelImage.color = new Color(1f, 1f, 1f, 0f); // Default to transparent if just added
            }
            
            // This script will ensure it can be clicked.
            // The visual appearance (background image, opacity) should be set in the Inspector.
            // The script will no longer force alpha to 0 if a sprite is present.
            panelImage.raycastTarget = true; 
        }
        else
        {
            Debug.LogError("DialogueUI: dialoguePanelMain is not assigned in the Inspector! Click-to-continue will not work.", this);
            enabled = false; 
            return;
        }

        // Ensure other critical UI references are assigned
        if (npcDialogueGroup == null || npcSpeechBubbleBackground == null ||
            playerDialogueGroup == null || playerSpeechBubbleBackground == null || 
            choicesParent == null || choiceButtonPrefab == null) {
            Debug.LogError("DialogueUI: One or more essential UI element references (NPC/Player groups, bubbles, choices setup) are not assigned in the Inspector! Please check all fields.", this);
            enabled = false;
            return;
        }

        HideDialogue(); 
        isInitialized = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInitialized) Initialize(); 
        if (!isInitialized || !dialoguePanelMain.activeInHierarchy) return; 

        bool playerIsCurrentlySpeaking = playerDialogueGroup.activeSelf && playerSpeechBubbleBackground.activeSelf;

        if (!isDisplayingChoices && !playerIsCurrentlySpeaking && dialogueManager != null)
        {
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                Button clickedButton = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Button>();
                bool onChoiceButton = false;
                if (clickedButton != null) {
                    foreach(var buttonInstance in currentChoiceButtonInstances) {
                        if (clickedButton.gameObject == buttonInstance) {
                            onChoiceButton = true;
                            break;
                        }
                    }
                }
                if (onChoiceButton) return;
            }
            dialogueManager.OnContinueClicked(); 
        }
        else if (playerIsCurrentlySpeaking && dialogueManager != null) 
        {
             dialogueManager.OnContinueClicked(); 
        }
    }

    public void ShowDialogue()
    {
        if (!isInitialized) Initialize();
        if (!isInitialized) return;

        if (dialoguePanelMain != null) dialoguePanelMain.SetActive(true);
        
        UpdateNpcVisualState(true, false);  
        UpdatePlayerVisualState(true, false); 
        
        ClearChoiceButtonsAndScroll();
        isDisplayingChoices = false;
    }

    public void HideDialogue()
    {
        if (dialoguePanelMain != null) dialoguePanelMain.SetActive(false);
        UpdateNpcVisualState(false, false);
        UpdatePlayerVisualState(false, false);
        ClearChoiceButtonsAndScroll();
        isDisplayingChoices = false;
    }
    
    private void UpdateNpcVisualState(bool showPortraitGroup, bool showSpeechBubble)
    {
        if (npcDialogueGroup != null)
        {
            npcDialogueGroup.SetActive(showPortraitGroup);
        }
        if (npcSpeechBubbleBackground != null)
        {
            npcSpeechBubbleBackground.SetActive(showPortraitGroup && showSpeechBubble);
        }
    }
    
    private void UpdatePlayerVisualState(bool showPortraitGroup, bool showSpeechBubble)
    {
        if (playerDialogueGroup != null)
        {
            playerDialogueGroup.SetActive(showPortraitGroup);
        }
        if (playerSpeechBubbleBackground != null)
        {
            playerSpeechBubbleBackground.SetActive(showPortraitGroup && showSpeechBubble);
        }
    }

    public void DisplayNpcLine(CharacterSO speaker, string text, List<DialogueNodeSO> choices, CharacterSO mainNpcForLayout_Unused)
    {
        if (!isInitialized) Initialize();
        if (!isInitialized || dialogueManager == null || playerProfile == null) return; 

        UpdatePlayerVisualState(true, false);  
        UpdateNpcVisualState(true, true);     

        if (npcDialogueGroup.activeSelf && npcSpeechBubbleBackground.activeSelf) 
        {
            if (speaker != null)
            {
                if (npcNameText != null) npcNameText.text = speaker.characterName;
                if (npcPortraitImage != null)
                {
                    npcPortraitImage.sprite = speaker.characterPortrait;
                    npcPortraitImage.enabled = (speaker.characterPortrait != null);
                }
            }
            else
            {
                if (npcNameText != null) npcNameText.text = "";
                if (npcPortraitImage != null) npcPortraitImage.enabled = false;
            }
            if (npcDialogueText != null) npcDialogueText.text = text;
        }

        if (choices != null && choices.Count > 0)
        {
            if (choicesScrollRect != null) {
                choicesScrollRect.gameObject.SetActive(true);
            } else if (choicesParent != null) { 
                choicesParent.gameObject.SetActive(true);
            } else {
                Debug.LogError("DialogueUI: Neither choicesScrollRect nor choicesParent is assigned. Cannot display choices.", this);
                isDisplayingChoices = false;
                return; 
            }
            PopulateChoiceButtons(choices);
            isDisplayingChoices = true;
        }
        else
        {
            ClearChoiceButtonsAndScroll(); 
            isDisplayingChoices = false; 
        }
    }

    public void DisplayPlayerChoiceLine(CharacterSO playerAvatar, string text)
    {
        if (!isInitialized) Initialize();
        if (!isInitialized || dialogueManager == null || playerProfile == null) return; 

        UpdateNpcVisualState(true, false);    
        UpdatePlayerVisualState(true, true);  
        ClearChoiceButtonsAndScroll();        

        if (playerDialogueGroup.activeSelf && playerSpeechBubbleBackground.activeSelf) 
        {
            if (playerAvatar != null && playerPortraitImage != null)
            {
                playerPortraitImage.sprite = playerAvatar.characterPortrait;
                playerPortraitImage.enabled = (playerAvatar.characterPortrait != null);
            }
            if (playerDialogueText != null) playerDialogueText.text = text;
        }
        isDisplayingChoices = false; 
    }

    private void PopulateChoiceButtons(List<DialogueNodeSO> choices)
    {
        Debug.Log($"DIALOGUE_DEBUG: DialogueUI.PopulateChoiceButtons - Called with {choices.Count} choices.");

        if (dialogueManager == null || playerProfile == null) {
             Debug.LogError("DialogueUI.PopulateChoiceButtons: DialogueManager or PlayerProfile not initialized properly!");
             return;
        }
        if (choicesParent == null || choiceButtonPrefab == null)
        {
            Debug.LogError("DialogueUI: ChoicesParent or ChoiceButtonPrefab is not assigned in the Inspector!", this);
            return;
        }
        
        if (!choicesParent.gameObject.activeInHierarchy) {
            if(choicesScrollRect != null && choicesScrollRect.gameObject.activeInHierarchy) {
                choicesParent.gameObject.SetActive(true); 
            } else {
                 Debug.LogWarning("DialogueUI.PopulateChoiceButtons: choicesParent is inactive. Choices might not be visible.");
            }
        }

        foreach (DialogueNodeSO choiceNode in choices)
        {
            GameObject choiceGO = Instantiate(choiceButtonPrefab, choicesParent);
            currentChoiceButtonInstances.Add(choiceGO);

            ChoiceButtonUI choiceButtonScript = choiceGO.GetComponent<ChoiceButtonUI>();
            if (choiceButtonScript != null)
            {
                choiceButtonScript.Setup(choiceNode, dialogueManager, playerProfile);
                Button btnComponent = choiceButtonScript.buttonComponent;
                if (btnComponent != null)
                {
                    btnComponent.onClick.RemoveAllListeners();
                    btnComponent.onClick.AddListener(() => OnChoiceButtonClicked(choiceNode));
                }
                else
                {
                    Debug.LogError("ChoiceButtonUI script on prefab is missing its 'buttonComponent' reference.", choiceGO);
                }
            }
            else
            {
                Debug.LogWarning($"ChoiceButtonUI script not found on instantiated choice button for node: {choiceNode.nodeID}. Setting basic text.", choiceGO);
                TextMeshProUGUI buttonText = choiceGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = choiceNode.dialogueText;
                Button btn = choiceGO.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnChoiceButtonClicked(choiceNode));
                }
            }
        }

        if (choicesScrollRect != null && choicesScrollRect.gameObject.activeInHierarchy) 
        {
            Canvas.ForceUpdateCanvases();
            RectTransform contentRect = choicesParent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0); 
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }
    }

    private void ClearChoiceButtonsAndScroll()
    {
        foreach (GameObject buttonGO in currentChoiceButtonInstances)
        {
            Destroy(buttonGO);
        }
        currentChoiceButtonInstances.Clear();

        if (choicesScrollRect != null)
        {
            choicesScrollRect.gameObject.SetActive(false); 
        }
        // isDisplayingChoices is set by the calling methods (DisplayNpcLine or DisplayPlayerChoiceLine or HideDialogue)
    }

    void OnChoiceButtonClicked(DialogueNodeSO choiceNode)
    {
        if (!isInitialized) Initialize(); 
        if (!isInitialized) return;

        if (dialogueManager != null && isDisplayingChoices) 
        {
            // isDisplayingChoices will be set to false by DisplayPlayerChoiceLine after this
            dialogueManager.OnPlayerChoiceSelected(choiceNode);
        }
    }
}
// Filename: ChoiceButtonUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceButtonUI : MonoBehaviour
{
    [Header("UI References (Assign in Prefab Inspector)")]
    public TextMeshProUGUI choiceTextComponent; // Renamed for clarity
    public Button buttonComponent; // Renamed for clarity
    public Image buttonBackground; // Optional: for changing background color

    [Header("Color Settings (Customize in Inspector)")]
    public Color defaultTextColor = Color.white;
    public Color skillPassTextColor = new Color(0.2f, 0.8f, 0.2f);   // Green
    public Color skillFailAttemptColor = new Color(0.9f, 0.2f, 0.2f); // Red (for attempting a check you're under for)
    
    [Header("Bad Option Colors")]
    public Color badOptionNormalTextColor = new Color(0.9f, 0.5f, 0.5f); // "Low red" for text
    // public Color badOptionNormalBackgroundColor; // If you want to tint the button BG itself
    public Color badOptionHoverSelectedColor = new Color(1f, 0.3f, 0.3f); // "More vivid red" for button highlight/selection

    // Store original button colors to revert to if not a bad option
    private ColorBlock originalButtonColors;
    private Color originalBackgroundColor; // If using buttonBackground

    void Awake()
    {
        if (buttonComponent == null) buttonComponent = GetComponent<Button>();
        if (choiceTextComponent == null) choiceTextComponent = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonBackground == null && buttonComponent != null) buttonBackground = buttonComponent.GetComponent<Image>();

        if (buttonComponent != null)
        {
            originalButtonColors = buttonComponent.colors;
        }
        if (buttonBackground != null)
        {
            originalBackgroundColor = buttonBackground.color;
        }
    }

    public void Setup(DialogueNodeSO choiceNode, DialogueManager dialogueManager, PlayerProfileSO playerProfile)
    {
        if (choiceNode == null || dialogueManager == null || playerProfile == null)
        {
            Debug.LogError("ChoiceButtonUI: Null argument provided to Setup!", this.gameObject);
            gameObject.SetActive(false);
            return;
        }

        string displayText = choiceNode.dialogueText;
        Color currentTextColor = defaultTextColor;
        bool isBadOptionHighlighted = false;

        // Reset button to original/default visual state before applying new styling
        if (buttonComponent != null) buttonComponent.colors = originalButtonColors;
        if (buttonBackground != null) buttonBackground.color = originalBackgroundColor;


        // 1. Handle Skill Check Option Display
        if (choiceNode.isSkillCheckOption)
        {
            if (choiceNode.skillCheckType == null)
            {
                Debug.LogError($"ChoiceButtonUI: Node '{choiceNode.nodeID}' isSkillCheckOption but skillCheckType is null!", choiceNode);
                displayText = $"[ERROR: SKILL NOT SET] {choiceNode.dialogueText}";
                currentTextColor = skillFailAttemptColor; // Make it look like an error
            }
            else
            {
                DialogueManager.SkillCheckDisplayInfo displayInfo = dialogueManager.GetSkillCheckDisplayInfo(
                    choiceNode.skillCheckType,
                    choiceNode.skillCheckDifficulty
                );

                // Format text: "[SKILL_NAME_UPPERCASE PlayerSkill/RequiredDifficulty - DisplayChance%] OriginalDialogueText"
                displayText = $"[{displayInfo.skillName.ToUpper()} {displayInfo.playerSkill}/{displayInfo.requiredDifficulty} - {displayInfo.displayChance}%] {choiceNode.dialogueText}";

                if (displayInfo.playerSkill >= displayInfo.requiredDifficulty) // Auto-pass condition
                {
                    currentTextColor = skillPassTextColor; // Green text
                }
                else // Chance-based roll will be needed upon selection
                {
                    currentTextColor = skillFailAttemptColor; // Red text (indicates player is below threshold but can attempt)
                }
            }
        }

        // 2. Handle Bad Option Highlighting (can override previous color settings)
        if (choiceNode.isBadOption && dialogueManager.ShouldHighlightBadOption(choiceNode))
        {
            isBadOptionHighlighted = true;
            currentTextColor = badOptionNormalTextColor; // "Low red" text for bad option

            if (buttonComponent != null)
            {
                ColorBlock badCb = buttonComponent.colors;
                // Normal color might be the original, or slightly tinted if you prefer.
                // The main "low red" indicator is the text.
                // The "more vivid red" is for hover/selection.
                badCb.highlightedColor = badOptionHoverSelectedColor;
                badCb.selectedColor = badOptionHoverSelectedColor; // Also apply to selected state
                buttonComponent.colors = badCb;

                // Example: If you wanted to also tint the background for bad options in normal state:
                // if (buttonBackground != null) buttonBackground.color = new Color(0.6f, 0.4f, 0.4f); // Darker low red
            }
        }

        // Apply final text and color
        if (choiceTextComponent != null)
        {
            choiceTextComponent.text = displayText;
            choiceTextComponent.color = currentTextColor;
        }

        // All created choice buttons are interactable by default.
        // DialogueManager.IsPlayerChoiceAvailable would have prevented creation if it's fundamentally unavailable.
        if (buttonComponent != null)
        {
            buttonComponent.interactable = true;
        }
    }
}
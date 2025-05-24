// Filename: ClueSO.cs
using UnityEngine;
using System.Collections.Generic; // For List of GameActionSO

// Forward declare GameActionSO if it's in a different namespace and not globally accessible,
// or ensure GameActionSO.cs is compiled first or in an assembly definition Dialogue can access.
// For now, assuming it's globally accessible as per our previous setup.

[CreateAssetMenu(fileName = "NewClue", menuName = "Project Dublin/Investigation/Clue")]
public class ClueSO : ScriptableObject
{
    [Tooltip("Unique identifier for this clue, e.g., bloody_knife_01")]
    public string clueID;

    [Tooltip("Player-facing name of the clue, e.g., 'Bloody Knife', 'Scuff Marks'")]
    public string clueName = "New Clue";

    [Header("In-Scene Representation")]
    [Tooltip("Icon or visual representation of the clue hotspot in the investigation scene before interaction.")]
    public Sprite iconForSceneHotspot; // Optional, could also be a 3D model or just an interactable area

    [Header("Interaction & Information")]
    [TextArea(3, 5)]
    [Tooltip("Text shown by default or if a perception/skill check for more info fails.")]
    public string descriptionNormal;

    [TextArea(3, 5)]
    [Tooltip("Additional text shown if a skill check for this clue passes (if applicable).")]
    public string descriptionSkillCheckSuccess; // Shown if interactionSkillCheck passes

    [Tooltip("Image to display in the information pop-up when the clue is examined.")]
    public Sprite informationPopupImage; // Optional

    [Header("Discovery & Interaction Checks")]
    [Range(0, 100)]
    [Tooltip("Perception skill value player needs to even *see* or interact with this clue. 0 means always visible/interactable.")]
    public int perceptionToSeeDifficulty = 0;

    [Tooltip("Does fully understanding/interacting with this clue require an additional skill check after spotting it?")]
    public bool requiresInteractionSkillCheck = false;

    [Tooltip("If requiresInteractionSkillCheck is true, which skill is needed?")]
    public SkillSO interactionSkillRequired; // Reference your SkillSO

    [Range(1, 100)]
    [Tooltip("If requiresInteractionSkillCheck is true, what's the difficulty of that check?")]
    public int interactionSkillDifficulty = 30;

    [Header("Consequences")]
    [Tooltip("Actions to perform when this clue is successfully discovered/interacted with.")]
    public List<GameActionSO> actionsOnDiscovery;

    [Tooltip("Is this piece of evidence key to progressing a case? (Used for UI feedback or CaseManager logic)")]
    public bool isKeyEvidence = false;

    // Future ideas:
    // - Sound effect on discovery
    // - Link to Casing System entries
}
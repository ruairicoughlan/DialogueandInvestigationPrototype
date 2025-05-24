// Filename: InvestigationSceneSO.cs
using UnityEngine;
using System.Collections.Generic;

// Ensure these dependent types are correctly defined and accessible in your project:
// public class ClueSO : ScriptableObject { /* ... */ }
// public class CharacterSO : ScriptableObject { /* ... */ }
// public class DialogueConversationSO : ScriptableObject { /* ... */ }
// public class LocationTypeSO : ScriptableObject { /* ... */ } // Crucially, this must be defined

// Helper Serializable class for placing clues in the scene (NOT a ScriptableObject)
[System.Serializable]
public class CluePlacementData // This definition should be here or accessible
{
    [Tooltip("The ClueSO asset representing this clue.")]
    public ClueSO clueAsset;
    [Tooltip("Normalized X,Y position for the clue hotspot on the panoramic image (0,0 is bottom-left, 1,1 is top-right). Z can be used for layering if needed.")]
    public Vector3 positionInScene = new Vector3(0.5f, 0.5f, 0f);
    [Tooltip("Initial scale of the clue hotspot visual in the scene.")]
    public Vector2 hotspotScale = Vector2.one;
}

// Helper Serializable class for placing characters in the scene
[System.Serializable]
public class CharacterPlacementData // This definition should be here or accessible
{
    [Tooltip("The CharacterSO asset representing this character.")]
    public CharacterSO characterAsset;
    [Tooltip("The DialogueConversationSO to start when this character is clicked.")]
    public DialogueConversationSO dialogueToStart;
    [Tooltip("Normalized X,Y position for the character hotspot on the panoramic image.")]
    public Vector3 positionInScene = new Vector3(0.75f, 0.5f, 0f);
    [Tooltip("Initial scale of the character hotspot visual.")]
    public Vector2 hotspotScale = Vector2.one;
}

// Helper Serializable class for off-screen witnesses
[System.Serializable]
public class OffScreenWitnessData // This definition should be here or accessible
{
    [Tooltip("The CharacterSO for the off-screen witness.")]
    public CharacterSO witnessCharacter;
    [Tooltip("The DialogueConversationSO to start when interacting with this witness via the UI pop-up.")]
    public DialogueConversationSO dialogueToStart;
    [Tooltip("Display name for the witness in the 'Witness Available' UI pop-up (e.g., 'Lorraine Brasco').")]
    public string displayNameForPopup = "Witness";
}


[CreateAssetMenu(fileName = "NewInvestigationScene", menuName = "Project Dublin/Investigation/Investigation Scene")]
public class InvestigationSceneSO : ScriptableObject
{
    [Header("Scene Identification")]
    [Tooltip("Unique identifier for this investigation scene, e.g., Docks_Warehouse_Night")]
    public string sceneID;
    [Tooltip("Player-facing name of the location, e.g., 'Warehouse Interior', 'Library Crime Scene'")]
    public string sceneName = "New Investigation Scene";

    [Header("Visuals & Setup")]
    [Tooltip("The main panoramic background image for this scene.")]
    public Sprite panoramicBackgroundImage;
    // public AudioClip ambientSound; 

    [Header("Interactables")]
    [Tooltip("List of clues present in this scene and their placement data.")]
    public List<CluePlacementData> cluesInScene;

    [Tooltip("List of characters physically present and interactable in this scene.")]
    public List<CharacterPlacementData> charactersInScene;

    [Tooltip("List of witnesses who are not visually in the scene but can be talked to via a UI pop-up.")]
    public List<OffScreenWitnessData> offScreenWitnesses;

    [Header("Gameplay Parameters")]
    [Tooltip("Base duration of the police timer for this scene, in seconds. Set to 0 or negative for no timer.")]
    public float basePoliceTimerSeconds = 180f;

    // --- THIS IS THE CORRECTED FIELD ---
    [Tooltip("The type of location this scene represents. Influences police timer adjustments based on reputation.")]
    public LocationTypeSO locationType; // <<< THIS FIELD WAS MISSING/INCORRECT IN PREVIOUS FULL SCRIPT
    // --- END CORRECTED FIELD ---

    [Header("Player Character Thoughts")]
    [TextArea(2,4)]
    [Tooltip("Player character's thought/remark upon entering this investigation scene.")]
    public string playerCharacterThoughtOnEnter;

    [TextArea(2,4)]
    [Tooltip("Player character's thought/remark when they might have found all crucial info (optional). Trigger logic for this is TBD.")]
    public string playerCharacterThoughtAllFound;
}
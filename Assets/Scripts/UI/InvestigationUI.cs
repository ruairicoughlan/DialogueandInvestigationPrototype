// Filename: InvestigationUI.cs
using UnityEngine;
using UnityEngine.UI;       // For Image, Button, Slider etc.
using TMPro;                // For TextMeshProUGUI
using System.Collections.Generic; // For List
// using UnityEngine.EventSystems; // Not strictly needed if not using IPointerClickHandler on this script directly for now

public class InvestigationUI : MonoBehaviour 
{
    [Header("Main Investigation Panel")]
    [Tooltip("The root GameObject for the entire investigation UI screen.")]
    public GameObject investigationScreenPanel;

    [Header("Scene Display")]
    [Tooltip("Image component to display the panoramic background of the scene.")]
    public Image panoramicBgImage;
    [Tooltip("Parent Transform where clue hotspot UI elements will be instantiated. This should overlay the panoramicBgImage.")]
    public Transform clueHotspotsParent;
    [Tooltip("Prefab for a single clue hotspot UI element.")]
    public GameObject clueHotspotPrefab; 

    [Header("Timer Display")]
    [Tooltip("Text component to display the police timer value.")]
    public TextMeshProUGUI policeTimerText;
    [Tooltip("Slider component to visually represent the police timer.")]
    public Slider policeTimerSlider;

    [Header("Player Thoughts")]
    [Tooltip("Parent GameObject for the player thought bubble (Image/Panel).")]
    public GameObject playerThoughtBubble;
    [Tooltip("Text component within the thought bubble for the thought's content.")]
    public TextMeshProUGUI playerThoughtText;

    [Header("Controls")]
    [Tooltip("Button to exit the investigation scene.")]
    public Button exitButton;
    [Tooltip("Button to open the case screen/file.")]
    public Button caseScreenButton;

    [Header("Clue Info Popup")]
    [Tooltip("Parent panel for the clue information display.")]
    public GameObject clueInfoPopupPanel;
    [Tooltip("Image component in the popup to show the clue's specific image (optional).")]
    public Image cluePopupImage;
    [Tooltip("Text for the clue's description in the popup.")]
    public TextMeshProUGUI cluePopupDescriptionText;
    [Tooltip("Button to close the clue info popup.")]
    public Button cluePopupContinueButton;

    [Header("Skill Required Popup (for Clue Interaction)")]
    [Tooltip("The parent GameObject of the skill required pop-up.")]
    public GameObject skillRequiredPopupParent;
    [Tooltip("Text component to display the name of the skill required (e.g., Lockpicking).")]
    public TextMeshProUGUI skillRequiredDynamicName;
    [Tooltip("Image component for the circle indicating skill value requirement.")]
    public Image skillRequiredCircleImage;
    [Tooltip("Text component inside the circle for the required skill value.")]
    public TextMeshProUGUI skillRequiredCircleText;

    [Header("Witness Available Popup")]
    [Tooltip("Parent GameObject of the witness available pop-up (should have a Button component).")]
    public GameObject witnessAvailablePopup;
    [Tooltip("Text component for the static 'Witness Available' part if you have one.")]
    public TextMeshProUGUI witnessAvailableStaticText;
    [Tooltip("Text component for the dynamic witness name.")]
    public TextMeshProUGUI witnessAvailableNameText;
    [Tooltip("Image component (child of mask) to display the witness's character portrait.")]
    public Image witnessCharacterPortraitImage;

    private InvestigationManager investigationManager;
    private InvestigationSceneSO currentSceneDataRef;
    private ClueSO currentClueInPopup;
    private OffScreenWitnessData currentWitnessInPopupData;

    private List<GameObject> activeClueHotspotGOs = new List<GameObject>();

    void Start()
    {
        investigationManager = InvestigationManager.Instance;
        if (investigationManager == null)
        {
            Debug.LogError("InvestigationUI: InvestigationManager.Instance not found! Disabling UI.", this);
            enabled = false;
            return;
        }

        // Add listeners for buttons
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
        else Debug.LogWarning("InvestigationUI: Exit Button not assigned.", this);

        if (caseScreenButton != null) caseScreenButton.onClick.AddListener(OnCaseScreenButtonClicked);
        else Debug.LogWarning("InvestigationUI: Case Screen Button not assigned.", this);
        
        if (cluePopupContinueButton != null) cluePopupContinueButton.onClick.AddListener(OnCluePopupContinueClicked);
        else Debug.LogWarning("InvestigationUI: Clue Popup Continue Button not assigned.", this);

        if (playerThoughtBubble != null) 
        {
            Button thoughtDismissButton = playerThoughtBubble.GetComponent<Button>();
            if (thoughtDismissButton != null) thoughtDismissButton.onClick.AddListener(OnPlayerThoughtDismissClicked);
        }
        
        if (witnessAvailablePopup != null)
        {
            Button witnessBtn = witnessAvailablePopup.GetComponent<Button>();
            if (witnessBtn != null) witnessBtn.onClick.AddListener(OnWitnessAvailableClicked);
            else Debug.LogError("InvestigationUI: WitnessAvailable_Popup is missing a Button component for interaction!", this);
        }
        
        HideAllPopups(); 
        HideInvestigationScreen(); 
    }

    public void ShowInvestigationScreen()
    {
        if (investigationScreenPanel != null) investigationScreenPanel.SetActive(true);
        else Debug.LogError("InvestigationUI: investigationScreenPanel (Main Panel) is not assigned!", this);
    }

    public void HideInvestigationScreen()
    {
        if (investigationScreenPanel != null) investigationScreenPanel.SetActive(false);
        HideAllPopups(); 
    }

    private void HideAllPopups()
    {
        if (clueInfoPopupPanel != null) clueInfoPopupPanel.SetActive(false);
        if (skillRequiredPopupParent != null) skillRequiredPopupParent.SetActive(false);
        if (witnessAvailablePopup != null) witnessAvailablePopup.SetActive(false);
        if (playerThoughtBubble != null) playerThoughtBubble.SetActive(false);
    }

    public void SetupScene(InvestigationSceneSO sceneData)
    {
        currentSceneDataRef = sceneData;
        if (panoramicBgImage != null && sceneData != null && sceneData.panoramicBackgroundImage != null)
        {
            panoramicBgImage.sprite = sceneData.panoramicBackgroundImage;
            panoramicBgImage.enabled = true;
        }
        else if (panoramicBgImage != null)
        {
            panoramicBgImage.enabled = false; 
            if(sceneData == null) Debug.LogWarning("InvestigationUI: SetupScene called with null sceneData.", this);
            else if(sceneData.panoramicBackgroundImage == null) Debug.LogWarning($"InvestigationUI: Scene '{sceneData.sceneName}' has no Panoramic Background Image.", this);
        }
        
        ClearActiveClueHotspots();
        DisplayWitnessAvailablePopupIfNeeded(sceneData);
        Debug.Log("INVESTIGATION_UI_DEBUG: SetupScene FINISHED. Panoramic sprite: " + (panoramicBgImage.sprite != null ? panoramicBgImage.sprite.name : "NULL") + ", Enabled: " + panoramicBgImage.enabled); // <<< DEBUG LOG ADDED
    }

    private void ClearActiveClueHotspots()
    {
        foreach(var hotspotGO in activeClueHotspotGOs)
        {
            if (hotspotGO != null) Destroy(hotspotGO);
        }
        activeClueHotspotGOs.Clear();
    }

    public void DisplayClueHotspots(List<CluePlacementData> spottableClues)
    {
        ClearActiveClueHotspots();
        if (spottableClues == null) {
             Debug.Log("INVESTIGATION_UI_DEBUG: DisplayClueHotspots - Received null list, no hotspots to display.");
             return;
        }
        Debug.Log($"INVESTIGATION_UI_DEBUG: DisplayClueHotspots - Received {spottableClues.Count} spottable clues to display.");

        if (clueHotspotsParent == null) {
            Debug.LogError("InvestigationUI: clueHotspotsParent is not assigned!", this);
            return;
        }
        if (clueHotspotPrefab == null) {
            Debug.LogError("InvestigationUI: clueHotspotPrefab is not assigned! Cannot create clue hotspots.", this);
            return;
        }

        // Use clueHotspotsParent directly as the reference for size and pivot for positioning calculation.
        // This assumes clueHotspotsParent is correctly set up to overlay the clickable area of your panoramic image.
        RectTransform placementAreaRect = clueHotspotsParent.GetComponent<RectTransform>();
        if (placementAreaRect == null) {
             Debug.LogError("InvestigationUI: ClueHotspots_Parent is missing a RectTransform component. This should not happen for UI elements.", this);
            return;
        }
        Vector2 placementAreaSize = placementAreaRect.rect.size;

        foreach (var cluePlacement in spottableClues)
        {
            if (cluePlacement.clueAsset == null) 
            {
                Debug.LogWarning("InvestigationUI: DisplayClueHotspots - CluePlacementData has null ClueSO. Skipping.");
                continue;
            }

            Debug.Log($"INVESTIGATION_UI_DEBUG: Loop Start - Processing Clue: {cluePlacement.clueAsset.clueName} (ID: {cluePlacement.clueAsset.clueID}) at NormPos: ({cluePlacement.positionInScene.x}, {cluePlacement.positionInScene.y})");

            GameObject hotspotGO = Instantiate(clueHotspotPrefab, clueHotspotsParent);
            activeClueHotspotGOs.Add(hotspotGO);
            hotspotGO.name = $"Hotspot_{cluePlacement.clueAsset.clueID}";

            RectTransform hotspotRect = hotspotGO.GetComponent<RectTransform>();
            if (hotspotRect == null) {
                Debug.LogError("ClueHotspot_Prefab is missing a RectTransform component!", hotspotGO);
                Destroy(hotspotGO); 
                activeClueHotspotGOs.Remove(hotspotGO);
                continue;
            }
            
            // --- SIMPLIFIED AND STANDARD POSITIONING LOGIC ---
            hotspotRect.anchorMin = Vector2.zero; // Anchor to bottom-left of ClueHotspots_Parent
            hotspotRect.anchorMax = Vector2.zero;
            hotspotRect.pivot = new Vector2(0.5f, 0.5f); // Hotspot's own pivot is its center

            // Calculate anchoredPosition based on normalized coordinates directly within the placementAreaSize.
            // This works correctly if ClueHotspots_Parent's pivot is (0,0) OR if it's (0.5,0.5) and stretched,
            // because anchoredPosition is relative to the anchors.
            float anchoredX = cluePlacement.positionInScene.x * placementAreaSize.x;
            float anchoredY = cluePlacement.positionInScene.y * placementAreaSize.y;
            
            // If ClueHotspots_Parent has a pivot NOT at (0,0) (e.g., 0.5, 0.5), we need to offset.
            // The (0,0) anchor means anchoredPosition is from bottom-left of parent.
            // If parent pivot is 0.5,0.5, then parent's bottom-left is at (-width/2, -height/2) relative to its pivot.
            // So, to place something at parent's actual visual bottom-left (normX=0, normY=0)
            // using this anchoring, the anchoredPosition would be (0,0).
            // To place at parent's center (normX=0.5, normY=0.5), anchoredPosition is (parentWidth/2, parentHeight/2).
            // This direct multiplication *should* work correctly.
            hotspotRect.anchoredPosition = new Vector2(anchoredX, anchoredY);
            // --- END SIMPLIFIED POSITIONING LOGIC ---

            Debug.Log($"HOTSPOT_DEBUG: Hotspot '{hotspotGO.name}' Clue: '{cluePlacement.clueAsset.clueName}' " +
                      $"NormPos: ({cluePlacement.positionInScene.x},{cluePlacement.positionInScene.y}), " +
                      $"PlacementAreaSize: {placementAreaSize}, " + // This is size of ClueHotspots_Parent
                      $"PlacementAreaPivot: {placementAreaRect.pivot}, " + // Pivot of ClueHotspots_Parent
                      $"HotspotPivot: {hotspotRect.pivot}, " +
                      $"==> Final AnchoredPos: ({anchoredX}, {anchoredY})");


            Image hotspotImage = hotspotGO.GetComponent<Image>();
            if (hotspotImage != null) {
                if (cluePlacement.clueAsset.iconForSceneHotspot != null) {
                    hotspotImage.sprite = cluePlacement.clueAsset.iconForSceneHotspot;
                    hotspotImage.SetNativeSize(); 
                    hotspotRect.localScale = cluePlacement.hotspotScale; 
                    hotspotImage.enabled = true;
                } else {
                    hotspotImage.enabled = false; 
                    Debug.LogWarning($"Clue '{cluePlacement.clueAsset.clueName}' has no iconForSceneHotspot assigned. Hotspot will be invisible unless prefab has a default image and color with alpha > 0.");
                }
            }

            Button hotspotButton = hotspotGO.GetComponent<Button>();
            if (hotspotButton != null) {
                hotspotButton.onClick.RemoveAllListeners();
                ClueSO currentClue = cluePlacement.clueAsset; 
                hotspotButton.onClick.AddListener(() => OnClueHotspotUIClicked(currentClue));
            } else {
                Debug.LogWarning("ClueHotspot_Prefab is missing a Button component.", hotspotGO);
            }
        }
    }
    
    private void OnClueHotspotUIClicked(ClueSO clue)
    {
        if (investigationManager != null)
        {
            investigationManager.OnClueHotspotClicked(clue);
        }
        else
        {
            Debug.LogError("InvestigationUI: Cannot handle clue click, InvestigationManager is null!", this);
        }
    }

    public void UpdateClueHotspotState(ClueSO clue, bool isDiscovered)
    {
        Debug.Log($"INVESTIGATION_UI_DEBUG: UpdateClueHotspotState for {clue?.clueName}, Discovered: {isDiscovered}");
        if(isDiscovered && investigationManager != null) {
             DisplayClueHotspots(investigationManager.GetSpottableCluePlacements());
        }
    }

    public void ShowClueInfoPopup(ClueSO clue, string description, bool skillCheckPassed_Unused)
    {
        if (clueInfoPopupPanel == null || clue == null) return;
        currentClueInPopup = clue; 
        if (cluePopupDescriptionText != null) cluePopupDescriptionText.text = description;
        if (cluePopupImage != null) {
            if (clue.informationPopupImage != null) {
                cluePopupImage.sprite = clue.informationPopupImage;
                cluePopupImage.enabled = true;
            } else {
                cluePopupImage.enabled = false;
            }
        }
        clueInfoPopupPanel.SetActive(true);
    }

    private void OnCluePopupContinueClicked()
    {
        if (clueInfoPopupPanel != null) clueInfoPopupPanel.SetActive(false);
        if (investigationManager != null && currentClueInPopup != null) {
            investigationManager.OnCluePopupClosed(currentClueInPopup);
            currentClueInPopup = null;
        }
    }

    public void ShowSkillRequiredPopup(Vector2 screenPosition, SkillSO skill, int playerSkillValue, int requiredValue)
    {
        if (skillRequiredPopupParent == null || skill == null) return;
        if (skillRequiredDynamicName != null) skillRequiredDynamicName.text = skill.skillName;
        if (skillRequiredCircleText != null) skillRequiredCircleText.text = requiredValue.ToString();
        if (skillRequiredCircleImage != null) {
            skillRequiredCircleImage.color = (playerSkillValue >= requiredValue) ? Color.green : Color.red;
        }
        
        RectTransform parentCanvasRect = investigationScreenPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Vector2 localPoint;
        Camera canvasCamera = (investigationScreenPanel.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay) ? null : investigationScreenPanel.GetComponentInParent<Canvas>().worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, screenPosition, canvasCamera, out localPoint))
        {
            (skillRequiredPopupParent.transform as RectTransform).anchoredPosition = localPoint;
        }
        skillRequiredPopupParent.SetActive(true);
    }

    public void HideSkillRequiredPopup()
    {
        if (skillRequiredPopupParent != null) skillRequiredPopupParent.SetActive(false);
    }

    public void ShowPlayerThought(string thoughtText)
    {
        if (playerThoughtBubble == null || playerThoughtText == null) return;
        playerThoughtText.text = thoughtText;
        playerThoughtBubble.SetActive(true);
    }

    private void OnPlayerThoughtDismissClicked() 
    {
        if (playerThoughtBubble != null) playerThoughtBubble.SetActive(false);
        if (investigationManager != null) investigationManager.OnPlayerThoughtDismissed();
    }

    public void UpdatePoliceTimer(float currentTime, float totalTime)
    {
        if (policeTimerText == null || policeTimerSlider == null) return;
        if (currentTime == float.PositiveInfinity) {
            policeTimerText.text = "Time: ---";
            policeTimerSlider.gameObject.SetActive(false); 
            return;
        }
        policeTimerSlider.gameObject.SetActive(true); 
        if (currentTime >= 0 && totalTime > 0) {
            float minutes = Mathf.FloorToInt(currentTime / 60);
            float seconds = Mathf.FloorToInt(currentTime % 60);
            policeTimerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            policeTimerSlider.value = Mathf.Clamp01(currentTime / totalTime); 
        } else {
            policeTimerText.text = "Time: 00:00";
            policeTimerSlider.value = 0f;
        }
    }

    public void ShowPoliceArrivedMessage()
    {
        if (policeTimerText != null) policeTimerText.text = "POLICE ARRIVED!";
        if (policeTimerSlider != null) policeTimerSlider.gameObject.SetActive(false);
        Debug.Log("InvestigationUI: Police have arrived!");
    }

    public void DisplayWitnessAvailablePopupIfNeeded(InvestigationSceneSO sceneData)
    {
        if (witnessAvailablePopup == null || sceneData == null) return;
        if (sceneData.offScreenWitnesses != null && sceneData.offScreenWitnesses.Count > 0) {
            currentWitnessInPopupData = sceneData.offScreenWitnesses[0]; 
            if (currentWitnessInPopupData.witnessCharacter != null) {
                if (witnessAvailableStaticText != null) witnessAvailableStaticText.text = "Witness Available:";
                if (witnessAvailableNameText != null) witnessAvailableNameText.text = currentWitnessInPopupData.displayNameForPopup;
                if (witnessCharacterPortraitImage != null) {
                    witnessCharacterPortraitImage.sprite = currentWitnessInPopupData.witnessCharacter.characterPortrait;
                    witnessCharacterPortraitImage.enabled = (witnessCharacterPortraitImage.sprite != null);
                }
                witnessAvailablePopup.SetActive(true);
            } else {
                witnessAvailablePopup.SetActive(false); 
            }
        } else {
            witnessAvailablePopup.SetActive(false); 
        }
    }

    private void OnWitnessAvailableClicked()
    {
        if (investigationManager != null && currentWitnessInPopupData != null && currentWitnessInPopupData.dialogueToStart != null) {
            Debug.Log($"Witness popup clicked for: {currentWitnessInPopupData.displayNameForPopup}.");
            if (witnessAvailablePopup != null) witnessAvailablePopup.SetActive(false); 
            // TODO: Tell InvestigationManager to initiate dialogue (which involves GameStateManager)
            // investigationManager.InitiateDialogueWithOffScreenWitness(currentWitnessInPopupData);
        }
    }
    
    private void OnExitButtonClicked()
    {
        Debug.Log("Exit Button Clicked");
        if (investigationManager != null) investigationManager.ExitInvestigation();
    }

    private void OnCaseScreenButtonClicked()
    {
        Debug.Log("Case Screen Button Clicked");
        // TODO: Implement logic to open/show the case screen UI
    }
}
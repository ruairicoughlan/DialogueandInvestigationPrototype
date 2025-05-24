// Filename: InvestigationManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for .Any()

public class InvestigationManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the PlayerProfileSO for skill checks (e.g., Perception).")]
    [SerializeField] private PlayerProfileSO playerProfile;

    [Tooltip("Reference to the PoliceReputationSettingsSO for timer calculations.")]
    [SerializeField] private PoliceReputationSettingsSO policeReputationSettings;

    [Tooltip("Reference to the CaseManager for updating case progress based on clues found.")]
    [SerializeField] private CaseManager caseManager;

    [Tooltip("Reference to the InvestigationUI script that will display the scene.")]
    [SerializeField] private InvestigationUI investigationUI; 
    
    [Header("Skill Configuration")]
    [Tooltip("Assign the 'Perception' SkillSO here for clue spotting checks.")]
    [SerializeField] private SkillSO perceptionSkill;

    [Header("Current Investigation State")]
    private InvestigationSceneSO currentSceneData;
    private float currentPoliceTimerValue;
    private bool isTimerRunning = false;
    private HashSet<ClueSO> discoveredClues = new HashSet<ClueSO>();
    private HashSet<ClueSO> spottedClues = new HashSet<ClueSO>();

    public static InvestigationManager Instance { get; private set; }

    /// <summary>
    /// Indicates if the main investigation UI screen is currently active and visible.
    /// Used by external scripts (like debug triggers) to check state.
    /// </summary>
    public bool IsUIScreenActive => investigationUI != null && 
                                    investigationUI.investigationScreenPanel != null && 
                                    investigationUI.investigationScreenPanel.activeInHierarchy;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        bool dependenciesMet = true;
        if (playerProfile == null) { Debug.LogError("InvestigationManager: PlayerProfileSO not assigned in Inspector!", this); dependenciesMet = false; }
        if (policeReputationSettings == null) { Debug.LogError("InvestigationManager: PoliceReputationSettingsSO not assigned in Inspector!", this); dependenciesMet = false; }
        if (caseManager == null) { Debug.LogError("InvestigationManager: CaseManager not assigned in Inspector!", this); dependenciesMet = false; }
        if (investigationUI == null) { Debug.LogError("InvestigationManager: InvestigationUI not assigned in Inspector!", this); dependenciesMet = false; }
        if (perceptionSkill == null) { Debug.LogError("InvestigationManager: Perception SkillSO not assigned in Inspector! Clue spotting will not work correctly.", this); dependenciesMet = false; }

        if (!dependenciesMet) {
            Debug.LogError("InvestigationManager: Critical dependencies missing. Disabling this manager component.", this);
            enabled = false; 
            return;
        }
        
        if (investigationUI != null) investigationUI.HideInvestigationScreen(); // Start hidden
    }

    void Update()
    {
        if (!enabled) return; // Don't run if disabled due to missing dependencies

        if (isTimerRunning && currentSceneData != null && currentSceneData.basePoliceTimerSeconds > 0 && currentPoliceTimerValue < float.PositiveInfinity)
        {
            currentPoliceTimerValue -= Time.deltaTime;
            if (investigationUI != null)
            {
                float totalTime = CalculateInitialTimerDuration(currentSceneData.basePoliceTimerSeconds, 
                                                              currentSceneData.locationType, 
                                                              playerProfile.policeReputationRank);
                investigationUI.UpdatePoliceTimer(currentPoliceTimerValue, totalTime);
            }

            if (currentPoliceTimerValue <= 0)
            {
                isTimerRunning = false;
                currentPoliceTimerValue = 0;
                HandlePoliceArrival();
            }
        }
    }

    public void StartInvestigation(InvestigationSceneSO sceneData)
    {
        if (!enabled) {
             Debug.LogWarning("InvestigationManager is disabled (check for missing dependencies in Inspector). Cannot start investigation.");
             return;
        }
        if (sceneData == null) { Debug.LogError("InvestigationManager: Attempted to start investigation with null sceneData.", this); return; }
        // Dependencies like playerProfile, policeReputationSettings, investigationUI, perceptionSkill are checked in Start()
        
        Debug.Log($"Starting Investigation: {sceneData.sceneName} (ID: {sceneData.sceneID})");
        currentSceneData = sceneData;
        discoveredClues.Clear();
        spottedClues.Clear();

        investigationUI.SetupScene(currentSceneData); // Sets background, prepares UI for this scene
        UpdateSpottedClues(); // Determines which clues are initially visible
        investigationUI.DisplayClueHotspots(GetSpottableCluePlacements()); // Tells UI to show interactable clue visuals

        if (!string.IsNullOrEmpty(currentSceneData.playerCharacterThoughtOnEnter))
        {
            PauseTimer(); // Pause timer while thought is displayed
            investigationUI.ShowPlayerThought(currentSceneData.playerCharacterThoughtOnEnter);
            // InvestigationUI.OnPlayerThoughtDismissed will call ResumeTimer()
        }

        bool timerRemovedByRep = policeReputationSettings.DoesRankRemoveTimer(currentSceneData.locationType, playerProfile.policeReputationRank);
        if (currentSceneData.basePoliceTimerSeconds > 0 && !timerRemovedByRep)
        {
            currentPoliceTimerValue = CalculateInitialTimerDuration(
                currentSceneData.basePoliceTimerSeconds,
                currentSceneData.locationType,
                playerProfile.policeReputationRank
            );
            // Only start timer if not already paused by an initial thought bubble
            if (string.IsNullOrEmpty(currentSceneData.playerCharacterThoughtOnEnter) || (playerThoughtBubble != null && !playerThoughtBubble.activeInHierarchy) ) {
                 isTimerRunning = true;
            }
            Debug.Log($"Police timer initiated: {currentPoliceTimerValue} seconds. Running: {isTimerRunning}");
        }
        else
        {
            currentPoliceTimerValue = float.PositiveInfinity; 
            isTimerRunning = false;
            Debug.Log(timerRemovedByRep ? "Police timer removed due to positive reputation." : "No police timer active for this scene (base timer <= 0 or thought displayed).");
        }
        
        float initialTotalTimeForDisplay = (currentPoliceTimerValue < float.PositiveInfinity && currentPoliceTimerValue > 0) ? currentPoliceTimerValue : currentSceneData.basePoliceTimerSeconds;
        investigationUI.UpdatePoliceTimer(currentPoliceTimerValue, initialTotalTimeForDisplay); 
        investigationUI.ShowInvestigationScreen(); // Make the whole investigation UI visible
    }

    private float CalculateInitialTimerDuration(float baseTime, LocationTypeSO locType, int repRank)
    {
        if (policeReputationSettings == null) {
             Debug.LogWarning("CalculateInitialTimerDuration: policeReputationSettings is null. Returning base time.");
            return baseTime;
        }
        policeReputationSettings.InitializeLookup(); // Ensure lookup is ready
        float multiplier = policeReputationSettings.GetTimerMultiplier(locType, repRank);
        return baseTime * multiplier;
    }

    public void UpdateSpottedClues() 
    {
        if (currentSceneData == null || playerProfile == null || perceptionSkill == null) return;
        spottedClues.Clear();
        int playerPerception = playerProfile.GetSkillValue(perceptionSkill);
        foreach (var cluePlacement in currentSceneData.cluesInScene)
        {
            if (cluePlacement.clueAsset == null) continue;
            if (playerPerception >= cluePlacement.clueAsset.perceptionToSeeDifficulty)
            {
                spottedClues.Add(cluePlacement.clueAsset);
            }
        }
        if (investigationUI != null && investigationUI.gameObject.activeInHierarchy) { // Check if UI is active
            investigationUI.DisplayClueHotspots(GetSpottableCluePlacements());
        }
    }
    
    public List<CluePlacementData> GetSpottableCluePlacements()
    {
        List<CluePlacementData> spottable = new List<CluePlacementData>();
        if (currentSceneData == null) return spottable;
        foreach (var cluePlacement in currentSceneData.cluesInScene)
        {
            if (cluePlacement.clueAsset != null && 
                spottedClues.Contains(cluePlacement.clueAsset) && 
                !discoveredClues.Contains(cluePlacement.clueAsset))
            {
                spottable.Add(cluePlacement);
            }
        }
        return spottable;
    }

    public void OnClueHotspotClicked(ClueSO clue)
    {
        if (!enabled) return;
        if (clue == null || discoveredClues.Contains(clue) || !spottedClues.Contains(clue)) return;
        
        PauseTimer(); 
        Debug.Log($"Player clicked on clue: {clue.clueName}");
        bool interactionSkillCheckPassed = false;
        if (clue.requiresInteractionSkillCheck)
        {
            if (clue.interactionSkillRequired == null) { 
                Debug.LogWarning($"Clue '{clue.clueName}' requires interaction skill check, but skill is not defined on ClueSO.");
            } else if (DialogueManager.Instance != null) { // Still using DialogueManager's check for now
                 interactionSkillCheckPassed = DialogueManager.Instance.PerformSkillCheck(clue.interactionSkillRequired, clue.interactionSkillDifficulty);
            } else { 
                Debug.LogError("Cannot perform interaction skill check for clue: DialogueManager.Instance is null.");
            }
        }
        string descriptionToShow = (clue.requiresInteractionSkillCheck && interactionSkillCheckPassed && !string.IsNullOrEmpty(clue.descriptionSkillCheckSuccess)) 
                                   ? clue.descriptionSkillCheckSuccess : clue.descriptionNormal;
        
        if(investigationUI != null) investigationUI.ShowClueInfoPopup(clue, descriptionToShow, interactionSkillCheckPassed);
    }

    public void OnCluePopupClosed(ClueSO clueJustViewed)
    {
        if (!enabled) return;
        if (clueJustViewed != null && spottedClues.Contains(clueJustViewed) && !discoveredClues.Contains(clueJustViewed))
        {
            discoveredClues.Add(clueJustViewed);
            ExecuteClueActions(clueJustViewed);
            if(investigationUI != null) investigationUI.UpdateClueHotspotState(clueJustViewed, true); 
            CheckForAllFoundThought();
        }
        ResumeTimer();
    }
    
    private void ExecuteClueActions(ClueSO clue) { 
        if (clue.actionsOnDiscovery != null && caseManager != null && playerProfile != null) {
            Debug.Log($"Executing {clue.actionsOnDiscovery.Count} actions for clue '{clue.clueName}'");
            foreach(var action in clue.actionsOnDiscovery) {
                if (action != null) action.Execute(this.gameObject, playerProfile, caseManager);
            }
        }
    }

    private void CheckForAllFoundThought() { 
        if (!enabled || currentSceneData == null || investigationUI == null || string.IsNullOrEmpty(currentSceneData.playerCharacterThoughtAllFound)) return;
        bool allKeyEvidenceFound = true; bool hasAnyKeyEvidence = false;
        foreach (var cp in currentSceneData.cluesInScene) { if (cp.clueAsset == null) continue; if (cp.clueAsset.isKeyEvidence) { hasAnyKeyEvidence = true; if (!discoveredClues.Contains(cp.clueAsset)) { allKeyEvidenceFound = false; break; } } }
        if (hasAnyKeyEvidence && allKeyEvidenceFound) { 
            PauseTimer(); // Pause for the thought
            investigationUI.ShowPlayerThought(currentSceneData.playerCharacterThoughtAllFound);
        }
    }
    
    public void OnPlayerThoughtDismissed() { 
        if (!enabled) return;
        ResumeTimer(); // Resume timer after player dismisses the thought
    }

    public void PauseTimer() { 
        if (!enabled) return;
        if (isTimerRunning && currentSceneData != null && currentSceneData.basePoliceTimerSeconds > 0 && currentPoliceTimerValue < float.PositiveInfinity) {
            isTimerRunning = false; Debug.Log("Police timer paused.");
        }
    }

    public void ResumeTimer() { 
        if (!enabled) return;
        if (currentSceneData != null && currentSceneData.basePoliceTimerSeconds > 0 && currentPoliceTimerValue > 0 && currentPoliceTimerValue < float.PositiveInfinity) {
            bool timerRemovedByRep = policeReputationSettings.DoesRankRemoveTimer(currentSceneData.locationType, playerProfile.policeReputationRank);
            if (!timerRemovedByRep) {
                isTimerRunning = true; Debug.Log("Police timer resumed.");
            }
        }
    }

    private void HandlePoliceArrival() { 
        Debug.Log("Police have arrived! Investigation over for this location."); 
        if(investigationUI != null) investigationUI.ShowPoliceArrivedMessage(); 
        isTimerRunning = false; 
        // TODO: Transition player out / disable further interactions in UI
    }

    public void ExitInvestigation() { 
        if (!enabled) return;
        Debug.Log("Player chose to exit investigation."); 
        if (investigationUI != null) investigationUI.HideInvestigationScreen();
        currentSceneData = null; 
        isTimerRunning = false;
        // TODO: Transition to another game state (e.g., Map) via GameStateManager
    }

    // Placeholder for PlayerThoughtBubble GameObject if needed for StartInvestigation logic
    // This is usually controlled by InvestigationUI itself.
    private GameObject playerThoughtBubble => (investigationUI != null) ? investigationUI.playerThoughtBubble : null;

}
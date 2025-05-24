// Filename: DEBUG_StartInvestigation.cs
using UnityEngine;

public class DEBUG_StartInvestigation : MonoBehaviour
{
    [Header("Investigation to Trigger")]
    [Tooltip("Assign the InvestigationSceneSO asset you want to load for this test.")]
    public InvestigationSceneSO sceneToLoad;

    [Tooltip("Press this key to start the investigation.")]
    public KeyCode activationKey = KeyCode.I;

    private InvestigationManager investigationManager;
    private bool logAlreadyActiveMessage = true; 

    void Start()
    {
        investigationManager = InvestigationManager.Instance;
        if (investigationManager == null)
        {
            Debug.LogError("DEBUG_StartInvestigation: InvestigationManager.Instance not found in scene! Test script will be disabled.", this);
            enabled = false; 
            return;
        }

        if (sceneToLoad == null)
        {
            Debug.LogWarning("DEBUG_StartInvestigation: 'Scene To Load' (InvestigationSceneSO) is not assigned in the Inspector. Test will not work if key is pressed.", this);
        }
    }

    void Update()
    {
        if (investigationManager == null || !investigationManager.enabled) 
        {
            return;
        }

        if (Input.GetKeyDown(activationKey))
        {
            if (!investigationManager.IsUIScreenActive)
            {
                if (sceneToLoad != null)
                {
                    Debug.Log($"DEBUG_StartInvestigation: Key '{activationKey}' pressed. Attempting to start investigation '{sceneToLoad.sceneName}'.");
                    investigationManager.StartInvestigation(sceneToLoad);
                    logAlreadyActiveMessage = false; 
                }
                else
                {
                    Debug.LogError("DEBUG_StartInvestigation: 'Scene To Load' (InvestigationSceneSO) is not assigned. Cannot start investigation.", this);
                }
            }
            else if (logAlreadyActiveMessage) 
            {
                 Debug.Log("DEBUG_StartInvestigation: Investigation screen appears to be already active. Not re-triggering with this key press.");
                 logAlreadyActiveMessage = false; 
            }
        }
        
        if (!logAlreadyActiveMessage && !investigationManager.IsUIScreenActive)
        {
            logAlreadyActiveMessage = true;
        }
    }
}
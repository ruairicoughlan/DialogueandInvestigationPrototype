// Filename: PoliceReputationSettingsSO.cs
using UnityEngine;
using System.Collections.Generic;
using System; // Required for Serializable attribute

[System.Serializable] // Make it show up in Inspector when used in a List
public class ReputationTimerModifier
{
    [Tooltip("The specific LocationTypeSO this modifier applies to.")]
    public LocationTypeSO locationType;

    // As per your design doc:
    // Negative Rank 1: Reduces Base Police Timer by 25% (multiplier 0.75)
    // Negative Rank 2: Reduces Base Police Timer by 50% (multiplier 0.50)
    // Negative Rank 3: Reduces Base Police Timer by 75% (multiplier 0.25)
    // Neutral: No effect (multiplier 1.0)
    // Positive Rank 1: Adds 25% to the Base Police Timer (multiplier 1.25)
    // Positive Rank 2: Adds 50% to the Base Police Timer (multiplier 1.50)
    // Positive Rank 3: Removes the Police Timer (special case, maybe a very large multiplier or a flag)

    [Tooltip("Multiplier for Negative Rank 3 Police Reputation (-75% timer). Use 0 for instant fail or specific handling.")]
    public float negRank3Multiplier = 0.25f;
    [Tooltip("Multiplier for Negative Rank 2 Police Reputation (-50% timer).")]
    public float negRank2Multiplier = 0.50f;
    [Tooltip("Multiplier for Negative Rank 1 Police Reputation (-25% timer).")]
    public float negRank1Multiplier = 0.75f;
    [Tooltip("Multiplier for Neutral Police Reputation (No effect).")]
    public float neutralMultiplier = 1.0f;
    [Tooltip("Multiplier for Positive Rank 1 Police Reputation (+25% timer).")]
    public float posRank1Multiplier = 1.25f;
    [Tooltip("Multiplier for Positive Rank 2 Police Reputation (+50% timer).")]
    public float posRank2Multiplier = 1.50f;
    [Tooltip("Does Positive Rank 3 Police Reputation remove the timer entirely for this location type?")]
    public bool posRank3RemovesTimer = true; // If true, timer is effectively infinite or disabled
}

[CreateAssetMenu(fileName = "PoliceReputationSettings", menuName = "Project Dublin/Core/Police Reputation Settings")]
public class PoliceReputationSettingsSO : ScriptableObject
{
    [Tooltip("List of timer modifiers for different location types based on police reputation.")]
    public List<ReputationTimerModifier> timerModifiers;

    // Dictionary for quick lookup at runtime
    private Dictionary<LocationTypeSO, ReputationTimerModifier> _modifierLookup;

    public void InitializeLookup()
    {
        if (_modifierLookup == null)
        {
            _modifierLookup = new Dictionary<LocationTypeSO, ReputationTimerModifier>();
            if (timerModifiers != null)
            {
                foreach (var modifier in timerModifiers)
                {
                    if (modifier.locationType != null && !_modifierLookup.ContainsKey(modifier.locationType))
                    {
                        _modifierLookup.Add(modifier.locationType, modifier);
                    }
                    else if (modifier.locationType != null)
                    {
                        Debug.LogWarning($"PoliceReputationSettingsSO: Duplicate LocationType '{modifier.locationType.name}' found in timerModifiers. Only the first will be used.", this);
                    }
                }
            }
        }
    }

    public float GetTimerMultiplier(LocationTypeSO locationType, int policeReputationRank)
    {
        if (_modifierLookup == null) InitializeLookup();

        if (locationType != null && _modifierLookup.TryGetValue(locationType, out ReputationTimerModifier mods))
        {
            switch (policeReputationRank)
            {
                case -3: return mods.negRank3Multiplier;
                case -2: return mods.negRank2Multiplier;
                case -1: return mods.negRank1Multiplier;
                case 0:  return mods.neutralMultiplier;
                case 1:  return mods.posRank1Multiplier;
                case 2:  return mods.posRank2Multiplier;
                case 3:  return mods.posRank3RemovesTimer ? float.PositiveInfinity : 2.0f; // Example: float.PositiveInfinity or a very large number
                default:
                    Debug.LogWarning($"GetTimerMultiplier: Unknown policeReputationRank '{policeReputationRank}'. Defaulting to neutral.", this);
                    return mods.neutralMultiplier;
            }
        }
        
        Debug.LogWarning($"GetTimerMultiplier: No specific modifier found for LocationType '{locationType?.name}'. Returning default multiplier 1.0.", this);
        return 1.0f; // Default if location type not found
    }

    public bool DoesRankRemoveTimer(LocationTypeSO locationType, int policeReputationRank)
    {
        if (_modifierLookup == null) InitializeLookup();

        if (policeReputationRank == 3 && locationType != null && _modifierLookup.TryGetValue(locationType, out ReputationTimerModifier mods))
        {
            return mods.posRank3RemovesTimer;
        }
        return false;
    }

    void OnEnable()
    {
        // Could also initialize here, but explicit call before first use is also fine
        // InitializeLookup();
    }
}
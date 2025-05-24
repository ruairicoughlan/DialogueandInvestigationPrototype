// Filename: PlayerProfileSO.cs
using UnityEngine;
using System.Collections.Generic; // For List and Dictionary

// SkillValue helper class (should already exist)
[System.Serializable]
public class SkillValue
{
    public SkillSO skill;
    [Range(0, 100)] public int value; // Skills 0-100
}

// <<< NEW HELPER CLASS FOR ATTRIBUTES >>>
[System.Serializable]
public class AttributeValue
{
    public AttributeSO attribute;
    [Range(1, 10)] public int value = 1; // Attributes 1-10, default to 1
}

[CreateAssetMenu(fileName = "PlayerProfile", menuName = "Project Dublin/Core/Player Profile")]
public class PlayerProfileSO : ScriptableObject
{
    [Header("Player Identity")]
    public CharacterSO chosenCharacterAvatar;
    public PlayerBackgroundSO playerBackground;

    [Header("Player Skills (0-100)")]
    public List<SkillValue> skills;

    [Header("Player Attributes (1-10)")] // <<< NEW SECTION
    public List<AttributeValue> attributes; // <<< NEW FIELD

    [Header("Reputation & Flags")]
    [Range(-3, 3)] public int policeReputationRank = 0;
    public List<string> initialGlobalFlags;

    // Runtime dictionaries for fast lookups
    private Dictionary<SkillSO, int> _skillMap;
    private Dictionary<AttributeSO, int> _attributeMap; // <<< NEW MAP
    private HashSet<string> _activeGlobalFlags;

    public Dictionary<SkillSO, int> SkillMap
    {
        get
        {
            if (_skillMap == null) RefreshStatsMap(); // Ensure initialized
            return _skillMap;
        }
    }

    public Dictionary<AttributeSO, int> AttributeMap // <<< NEW PROPERTY
    {
        get
        {
            if (_attributeMap == null) RefreshStatsMap(); // Ensure initialized
            return _attributeMap;
        }
    }

    public HashSet<string> ActiveGlobalFlags
    {
        get
        {
            if (_activeGlobalFlags == null) RefreshStatsMap(); // Ensure initialized
            return _activeGlobalFlags;
        }
    }

    public int GetSkillValue(SkillSO skill)
    {
        if (skill == null) return 0;
        SkillMap.TryGetValue(skill, out int value);
        return value;
    }

    public int GetAttributeValue(AttributeSO attribute) // <<< NEW METHOD
    {
        if (attribute == null) return 1; // Default to 1 if attribute not found or null
        AttributeMap.TryGetValue(attribute, out int value);
        return value > 0 ? value : 1; // Ensure attribute value is at least 1
    }

    public bool HasGlobalFlag(string flagID)
    {
        return ActiveGlobalFlags.Contains(flagID);
    }

    public void SetGlobalFlag(string flagID, bool state)
    {
        // Ensure ActiveGlobalFlags is initialized
        if (_activeGlobalFlags == null) RefreshStatsMap();

        if (state)
        {
            _activeGlobalFlags.Add(flagID);
            // Sync with editor list (optional, can be lossy if editor not updated)
            if (!initialGlobalFlags.Contains(flagID)) initialGlobalFlags.Add(flagID);
        }
        else
        {
            _activeGlobalFlags.Remove(flagID);
            initialGlobalFlags.Remove(flagID);
        }
    }

    public void RefreshStatsMap() // <<< RENAMED & UPDATED
    {
        // Skills
        _skillMap = new Dictionary<SkillSO, int>();
        if (skills != null)
        {
            foreach (var skillValue in skills)
            {
                if (skillValue.skill != null && !_skillMap.ContainsKey(skillValue.skill))
                {
                    _skillMap.Add(skillValue.skill, skillValue.value);
                }
            }
        }

        // Attributes
        _attributeMap = new Dictionary<AttributeSO, int>(); // <<< INITIALIZE ATTRIBUTE MAP
        if (attributes != null)
        {
            foreach (var attributeValue in attributes)
            {
                if (attributeValue.attribute != null && !_attributeMap.ContainsKey(attributeValue.attribute))
                {
                    _attributeMap.Add(attributeValue.attribute, Mathf.Clamp(attributeValue.value, 1, 10)); // Clamp 1-10
                }
            }
        }

        // Global Flags
        _activeGlobalFlags = new HashSet<string>(initialGlobalFlags ?? new List<string>());
    }

    void OnEnable()
    {
        RefreshStatsMap(); // Initialize all maps when SO is loaded/enabled
    }
}
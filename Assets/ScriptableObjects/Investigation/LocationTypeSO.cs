// Filename: LocationTypeSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewLocationType", menuName = "Project Dublin/Investigation/Location Type")]
public class LocationTypeSO : ScriptableObject
{
    [Tooltip("The unique ID or name for this location type, e.g., DOCKS, UPTOWN_DISTRICT, INDUSTRIAL_ZONE")]
    public string typeId = "DefaultLocationType"; // Can be used for lookups

    [Tooltip("Player-facing display name if needed, or just for designer reference.")]
    public string displayName = "Default Location Type";

    [TextArea]
    public string description = "General description of this type of location.";

    // You could add other location-type specific default parameters here later if needed
    // e.g., default NPC density, typical ambient sound profile, etc.
}
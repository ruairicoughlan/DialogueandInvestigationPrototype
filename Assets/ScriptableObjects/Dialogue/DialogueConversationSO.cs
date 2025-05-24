// Filename: DialogueConversationSO.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq queries if needed

[CreateAssetMenu(fileName = "NewConversation", menuName = "Project Dublin/Dialogue/Dialogue Conversation")]
public class DialogueConversationSO : ScriptableObject
{
    [Tooltip("An optional ID for this conversation, for easier reference if needed elsewhere.")]
    public string conversationID;

    [Tooltip("The Node ID of the very first dialogue node in this conversation.")]
    public string startNodeID; // This node should exist in the list below

    [Tooltip("All dialogue nodes that are part of this conversation.")]
    public List<DialogueNodeSO> allNodesInConversation;

    // Runtime lookup for faster node access
    private Dictionary<string, DialogueNodeSO> _nodeLookup;

    public DialogueNodeSO GetNodeByID(string nodeID)
    {
        if (_nodeLookup == null || _nodeLookup.Count == 0)
        {
            InitializeLookup();
        }

        if (_nodeLookup.TryGetValue(nodeID, out DialogueNodeSO node))
        {
            return node;
        }
        Debug.LogWarning($"Node with ID '{nodeID}' not found in conversation '{name}'.");
        return null;
    }

    public void InitializeLookup()
    {
        _nodeLookup = new Dictionary<string, DialogueNodeSO>();
        if (allNodesInConversation == null)
        {
            allNodesInConversation = new List<DialogueNodeSO>();
            Debug.LogWarning($"Conversation '{name}' has a null list of nodes. Initializing to empty list.", this);
            return;
        }

        foreach (var node in allNodesInConversation)
        {
            if (node != null && !string.IsNullOrEmpty(node.nodeID))
            {
                if (!_nodeLookup.ContainsKey(node.nodeID))
                {
                    _nodeLookup.Add(node.nodeID, node);
                }
                else
                {
                    Debug.LogWarning($"Duplicate Node ID '{node.nodeID}' found in conversation '{name}'. Only the first instance will be used.", this);
                }
            }
            else if (node != null && string.IsNullOrEmpty(node.nodeID))
            {
                 Debug.LogError($"DialogueNodeSO '{node.name}' within conversation '{name}' has an empty or null Node ID. It will be ignored.", node);
            }
        }
    }

    void OnEnable()
    {
        // Ensure lookup is initialized when the asset is loaded.
        // We might also call InitializeLookup() explicitly before starting a conversation
        // if nodes could be added/removed from the list at runtime (less common for SOs).
        InitializeLookup();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (allNodesInConversation != null && allNodesInConversation.Count > 0)
        {
            if (string.IsNullOrEmpty(startNodeID))
            {
                Debug.LogWarning($"Conversation '{name}' has nodes but no Start Node ID defined.", this);
            }
            else
            {
                bool startNodeFound = false;
                foreach (var node in allNodesInConversation)
                {
                    if (node != null && node.nodeID == startNodeID)
                    {
                        startNodeFound = true;
                        break;
                    }
                }
                if (!startNodeFound)
                {
                    Debug.LogWarning($"Conversation '{name}': Start Node ID '{startNodeID}' does not match any Node ID in 'All Nodes In Conversation'.", this);
                }
            }

            // Check for duplicate node IDs within this conversation
            var idCounts = allNodesInConversation
                .Where(n => n != null && !string.IsNullOrEmpty(n.nodeID))
                .GroupBy(n => n.nodeID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateId in idCounts)
            {
                Debug.LogError($"Conversation '{name}' contains duplicate Node ID: '{duplicateId}'. Node IDs must be unique within a conversation.", this);
            }
        }
         // Re-initialize lookup if validated in editor, in case list changed.
        if (Application.isPlaying) // Only re-init lookup if playing; otherwise, OnEnable handles it.
        {
            InitializeLookup();
        }
    }
#endif
}
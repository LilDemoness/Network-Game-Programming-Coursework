using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using Gameplay.GameplayObjects.Character.Customisation.Data;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager LocalClientInstance { get; private set; }

    [SerializeField] private FrameGFX[] m_playerFrames;
    private PlayerGFXWrapper[] _playerGFXWrappers;
    private Dictionary<SlotIndex, SlotGFXSection[]> _slotIndexToActiveGFXDict = new Dictionary<SlotIndex, SlotGFXSection[]>();


    /// <summary>
    ///     Called when we've updated this player's build.
    /// </summary>
    public event System.Action OnThisPlayerBuildUpdated;
    /// <summary>
    ///     Called when we've updated the local player's build.
    /// </summary>
    public static event System.Action<BuildData> OnLocalPlayerBuildUpdated;


    private void Awake()
    {
        // Setup our PlayerGFX Wrappers for simpler retrieving later.
        _playerGFXWrappers = new PlayerGFXWrapper[m_playerFrames.Length];
        for(int i = 0; i < m_playerFrames.Length; ++i)
        {
            _playerGFXWrappers[i] = new PlayerGFXWrapper(m_playerFrames[i]);
        }
        Debug.Log("Player Wrapper Count: " + _playerGFXWrappers.Length);
    }
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            LocalClientInstance = this;
            Debug.Log("Local Player", this);
        }
    }


    /// <summary>
    ///     Update the player's build and subsequent cached values.
    /// </summary>
    public void SetBuild(ulong clientID, BuildData buildData)
    {
        if (clientID != this.OwnerClientId)
            return;

        bool hasFoundActiveFrame = false;
        for (int i = 0; i < _playerGFXWrappers.Length; ++i)
        {
            if (!hasFoundActiveFrame)
            {
                // We haven't yet found our active frame. Perform a full check toggle (Also sets our ActiveGFX Dict if we find our active frame).
                if (_playerGFXWrappers[i].Toggle(buildData, ref _slotIndexToActiveGFXDict))
                {
                    // This is our active frame.
                    Debug.Log("Found Build" + _slotIndexToActiveGFXDict.Count);
                    hasFoundActiveFrame = true;
                }
            }
            else
            {
                // We will only ever have 1 active frame, and we have already found it. Disable all other frames.
                _playerGFXWrappers[i].Disable();
            }
        }

        OnThisPlayerBuildUpdated?.Invoke();
        if (IsLocalPlayer)
            OnLocalPlayerBuildUpdated?.Invoke(buildData);
    }


    // Doesn't account for multiple GFXSlotSections for a single SlotIndex.
    public SlotGFXSection[] GetActivationSlots()
    {
        SlotGFXSection[] slotGFXSections = new SlotGFXSection[_slotIndexToActiveGFXDict.Count];
        foreach (var kvp in _slotIndexToActiveGFXDict)
            slotGFXSections[kvp.Key.GetSlotInteger()] = kvp.Value[0];
        return slotGFXSections;
    }
    public SlotGFXSection[] GetSlotGFXForIndex(SlotIndex index) => _slotIndexToActiveGFXDict[index];
    public bool TryGetSlotGFXForIndex(SlotIndex index, out SlotGFXSection[] slotGFXSections) => _slotIndexToActiveGFXDict.TryGetValue(index, out slotGFXSections);
    public int GetActivationSlotCount() => _slotIndexToActiveGFXDict.Count;
}


struct PlayerGFXWrapper
{
    FrameGFX _frameGFX;
    Dictionary<SlotIndex, SlottableDataSlot> _attachmentSlots;


    public PlayerGFXWrapper(FrameGFX frameGFX)
    {
        this._frameGFX = frameGFX;
        
        this._attachmentSlots = new Dictionary<SlotIndex, SlottableDataSlot>(SlotIndexExtensions.GetMaxPossibleSlots());
        foreach (SlottableDataSlot attachmentSlot in frameGFX.GetSlottableDataSlotArray())
        {
            if (!_attachmentSlots.TryAdd(attachmentSlot.SlotIndex, attachmentSlot))
            {
                // We should only have 1 attachment slot for each SlotIndex, however reaching here means that we don't. Throw an exception so we know about this.
                throw new System.Exception($"We have multiple Attachment Slots with the same Slot Index ({attachmentSlot.SlotIndex}).\n" +
                    $"Duplicates: '{_attachmentSlots[attachmentSlot.SlotIndex].name}' & '{attachmentSlot.name}'");
            }
        }
    }


    public bool Toggle(BuildData buildData, ref Dictionary<SlotIndex, SlotGFXSection[]> slottables)
    {
        if (_frameGFX.Toggle(buildData.GetFrameData()) == false)
        {
            // This wrapper's frame isn't the correct frame for this build.
            return false;
        }

        // This wrapper's frame is the desired one.
        // Update slottables.
        slottables.Clear();
        for (int i = 0; i < buildData.ActiveSlottableIndicies.Length; ++i)
        {
            if (_attachmentSlots.TryGetValue(i.ToSlotIndex(), out SlottableDataSlot attachmentSlot) == false)
                continue;   // No AttachmentSlot for this index.

            slottables.Add(i.ToSlotIndex(), attachmentSlot.Toggle(buildData.GetSlottableData(i.ToSlotIndex())));
        }

        return true;
    }
    public void Disable() => _frameGFX.Toggle(null);
}
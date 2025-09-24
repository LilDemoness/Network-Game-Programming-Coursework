using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class PlayerCustomisation : MonoBehaviour
{
    [SerializeField] private FrameData[] _availableFrameData;
    private int m_activeFrameIndex;
    private int _activeFrameIndex
    {
        get => m_activeFrameIndex;
        set
        {
            // Set and Wrap our index.
            if (value < 0)
                m_activeFrameIndex = _availableFrameData.Length - 1;
            else if (value >= _availableFrameData.Length)
                m_activeFrameIndex = 0;
            else
                m_activeFrameIndex = value;

            // Notify Listeners.
            OnSelectedFrameChanged?.Invoke(_availableFrameData[_activeFrameIndex]);
        }
    }

    [SerializeField] private LegsData[] _availableLegData;
    private int m_activeLegIndex;
    private int _activeLegIndex
    {
        get => m_activeLegIndex;
        set
        {
            // Set and Wrap our index.
            if (value < 0)
                m_activeLegIndex = _availableLegData.Length - 1;
            else if (value >= _availableLegData.Length)
                m_activeLegIndex = 0;
            else
                m_activeLegIndex = value;

            // Notify Listeners.
            OnSelectedLegChanged?.Invoke(_availableLegData[m_activeLegIndex]);
        }
    }

    [SerializeField] private WeaponData[] _availableWeaponData;
    private int[] m_activeWeaponIndicies;
    private void ChangeActiveWeaponIndex(int weaponSlot, bool positive) => SetActiveWeaponIndex(weaponSlot, m_activeWeaponIndicies[weaponSlot] + (positive ? 1 : -1));
    private void SetActiveWeaponIndex(int weaponSlot, int newValue)
    {
        // Set and Wrap our index.
        if (newValue < 0)
            m_activeWeaponIndicies[weaponSlot] = _availableWeaponData.Length - 1;
        else if (newValue >= _availableWeaponData.Length)
            m_activeWeaponIndicies[weaponSlot] = 0;
        else
            m_activeWeaponIndicies[weaponSlot] = newValue;

        // Notify Listeners.
        OnSelectedWeaponChanged?.Invoke(weaponSlot, _availableWeaponData[m_activeWeaponIndicies[weaponSlot]]);
    }


    [SerializeField] private AbilityData[] _availableAbilityData;
    private int m_activeAbilityIndex;
    private int _activeAbilityIndex
    {
        get => m_activeAbilityIndex;
        set
        {
            // Set and Wrap our index.
            if (value < 0)
                m_activeAbilityIndex = _availableAbilityData.Length - 1;
            else if (value >= _availableAbilityData.Length)
                m_activeAbilityIndex = 0;
            else
                m_activeAbilityIndex = value;

            // Notify Listeners.
            OnSelectedAbilityChanged?.Invoke(_availableAbilityData[m_activeAbilityIndex]);
        }
    }


    #region Events

    public event System.Action<FrameData> OnSelectedFrameChanged;
    public event System.Action<LegsData> OnSelectedLegChanged;
    public event System.Action<int, WeaponData> OnSelectedWeaponChanged;
    public event System.Action<AbilityData> OnSelectedAbilityChanged;

    public event System.Action<FrameData, LegsData, WeaponData[], AbilityData> OnFinalisedCustomisation;

    #endregion


    private void Start()
    {
        // Start with default values.
        OnSelectedFrameChanged?.Invoke(_availableFrameData[_activeFrameIndex]);

        OnSelectedLegChanged?.Invoke(_availableLegData[_activeLegIndex]);

        int largestWeaponCount = 0;
        for(int i = 0; i < _availableFrameData.Length; ++i)
            if (_availableFrameData[i].WeaponSlotCount > largestWeaponCount)
                largestWeaponCount = _availableFrameData[i].WeaponSlotCount;
        m_activeWeaponIndicies = new int[largestWeaponCount];
        for (int i = 0; i < largestWeaponCount; ++i)
            OnSelectedWeaponChanged?.Invoke(i, _availableWeaponData[m_activeWeaponIndicies[i]]);

        OnSelectedAbilityChanged?.Invoke(_availableAbilityData[_activeAbilityIndex]);
    }


    public void SelectNextFrame() => ++_activeFrameIndex;
    public void SelectPreviousFrame() => --_activeFrameIndex;

    public void SelectNextLeg() => ++_activeLegIndex;
    public void SelectPreviousLeg() => --_activeLegIndex;

    public void SelectNextWeapon(int weaponSlot) => ChangeActiveWeaponIndex(weaponSlot, positive: true);
    public void SelectPreviousWeapon(int weaponSlot) => ChangeActiveWeaponIndex(weaponSlot, positive: false);

    public void SelectNextAbility() => ++_activeAbilityIndex;
    public void SelectPreviousAbility() => --_activeAbilityIndex;


    public void FinaliseCustomisation()
    {
        // Determine our active weapons.
        WeaponData[] activeWeapons = new WeaponData[m_activeWeaponIndicies.Length];
        for(int i = 0; i < activeWeapons.Length; ++i)
            activeWeapons[i] = _availableWeaponData[m_activeWeaponIndicies[i]];

        // Finalise our customisation.
        OnFinalisedCustomisation.Invoke(_availableFrameData[_activeFrameIndex], _availableLegData[_activeLegIndex], activeWeapons, _availableAbilityData[_activeAbilityIndex]);

        // We won't need this script anymore, so delete it.
        Destroy(this);
    }
}

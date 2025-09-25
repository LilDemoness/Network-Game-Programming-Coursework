using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerCustomisationUI : MonoBehaviour
{
    private PlayerCustomisationManager _customisationManager;
    private PlayerCustomisationOptionsDatabase customisationOptionsDatabase;
    private ulong _clientID;


    [Header("Button Text References")]
    [SerializeField] private TMP_Text _activeFrameText;

    [Space(5)]
    [SerializeField] private TMP_Text _activeLegText;
    
    [Space(5)]
    [SerializeField] private TMP_Text _activeWeapon1Text;
    [SerializeField] private TMP_Text _activeWeapon2Text;
    [SerializeField] private TMP_Text _activeWeapon3Text;

    [Space(5)]
    [SerializeField] private TMP_Text _activeAbilityText;


    [Header("Weapon Button References")]
    [SerializeField] private CanvasGroup[] _weaponButtonGroups;


    /*private void Awake()
    {
        _playerCustomisation = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerCustomisation>();

        // Events.
        _playerCustomisation.OnSelectedFrameChanged     += PlayerCustomisation_OnSelectedFrameChanged;
        _playerCustomisation.OnSelectedLegChanged       += PlayerCustomisation_OnSelectedLegChanged;
        _playerCustomisation.OnSelectedWeaponChanged    += PlayerCustomisation_OnSelectedWeaponChanged;
        _playerCustomisation.OnSelectedAbilityChanged   += PlayerCustomisation_OnSelectedAbilityChanged;
    }
    private void OnDestroy()
    {
        // Events.
        _playerCustomisation.OnSelectedFrameChanged     -= PlayerCustomisation_OnSelectedFrameChanged;
        _playerCustomisation.OnSelectedLegChanged       -= PlayerCustomisation_OnSelectedLegChanged;
        _playerCustomisation.OnSelectedWeaponChanged    -= PlayerCustomisation_OnSelectedWeaponChanged;
        _playerCustomisation.OnSelectedAbilityChanged   -= PlayerCustomisation_OnSelectedAbilityChanged;
    }


    private void PlayerCustomisation_OnSelectedFrameChanged(FrameData activeFrameData)
    {
        _activeFrameText.text = activeFrameData.Name;

        for(int i = 0; i < _weaponButtonGroups.Length; ++i)
        {
            if (i < activeFrameData.WeaponSlotCount)
            {
                // Active.
                _weaponButtonGroups[i].alpha = 1.0f;
                _weaponButtonGroups[i].interactable = true;
            }
            else
            {
                // Inactive.
                _weaponButtonGroups[i].alpha = 0.5f;
                _weaponButtonGroups[i].interactable = false;
            }
        }
    }
    private void PlayerCustomisation_OnSelectedLegChanged(LegsData legData) => _activeLegText.text = legData.Name;
    private void PlayerCustomisation_OnSelectedWeaponChanged(int weaponSlot, WeaponData weaponData)
    {
        switch(weaponSlot)
        {
            case 0: _activeWeapon1Text.text = weaponData.Name; break;
            case 1: _activeWeapon2Text.text = weaponData.Name; break;
            case 2: _activeWeapon3Text.text = weaponData.Name; break;
        }
    }
    private void PlayerCustomisation_OnSelectedAbilityChanged(AbilityData abilityData) => _activeAbilityText.text = abilityData.Name;



    public void SelectNextFrame() => _playerCustomisation.SelectNextFrame();
    public void SelectPreviousFrame() => _playerCustomisation.SelectPreviousFrame();

    public void SelectNextLeg() => _playerCustomisation.SelectNextLeg();
    public void SelectPreviousLeg() => _playerCustomisation.SelectPreviousLeg();

    public void SelectNextWeapon(int weaponSlot) => _playerCustomisation.SelectNextWeapon(weaponSlot);
    public void SelectPreviousWeapon(int weaponSlot) => _playerCustomisation.SelectPreviousWeapon(weaponSlot);

    public void SelectNextAbility() => _playerCustomisation.SelectNextAbility();
    public void SelectPreviousAbility() => _playerCustomisation.SelectPreviousAbility();


    public void FinaliseCustomisation()
    {
        _playerCustomisation.FinaliseCustomisation();
        Destroy(this.gameObject);
    }*/


    private void Awake()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
    }
    private void OnDestroy()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
    }
    public void Setup(PlayerCustomisationManager localCustomisationManager, ulong clientID, PlayerCustomisationOptionsDatabase customisationOptionsDatabase)
    {
        this._customisationManager = localCustomisationManager;
        this._clientID = clientID;
        this.customisationOptionsDatabase = customisationOptionsDatabase;
    }


    private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, PlayerCustomisationState customisationState)
    {
        if (this._clientID != clientID)
            return;

        UpdateUIText(ref customisationState);
    }
    private void UpdateUIText(ref PlayerCustomisationState customisationState)
    {
        _activeFrameText.text = customisationOptionsDatabase.FrameDatas[customisationState.FrameIndex].Name;

        _activeLegText.text = customisationOptionsDatabase.LegDatas[customisationState.LegIndex].Name;

        int activeWeaponSlots = customisationOptionsDatabase.FrameDatas[customisationState.FrameIndex].WeaponSlotCount;
        for (int i = 0; i < _weaponButtonGroups.Length; ++i)
        {
            if (i < activeWeaponSlots)
            {
                // Active.
                _weaponButtonGroups[i].alpha = 1.0f;
                _weaponButtonGroups[i].interactable = true;
            }
            else
            {
                // Inactive.
                _weaponButtonGroups[i].alpha = 0.5f;
                _weaponButtonGroups[i].interactable = false;
            }
        }
        
        _activeWeapon1Text.text = customisationOptionsDatabase.WeaponDatas[customisationState.PrimaryWeaponIndex].Name;
        _activeWeapon2Text.text = customisationOptionsDatabase.WeaponDatas[customisationState.SecondaryWeaponIndex].Name;
        _activeWeapon3Text.text = customisationOptionsDatabase.WeaponDatas[customisationState.TertiaryWeaponIndex].Name;

        _activeAbilityText.text = customisationOptionsDatabase.AbilityDatas[customisationState.AbilityIndex].Name;
    }


    public void SelectNextFrame() => _customisationManager.SelectNextFrame();
    public void SelectPreviousFrame() => _customisationManager.SelectPreviousFrame();

    public void SelectNextLeg() => _customisationManager.SelectNextLeg();
    public void SelectPreviousLeg() => _customisationManager.SelectPreviousLeg();

    public void SelectNextWeapon(int weaponSlot)
    {
        switch (weaponSlot)
        {
            case 0: _customisationManager.SelectNextPrimaryWeapon(); break;
            case 1: _customisationManager.SelectNextSecondaryWeapon(); break;
            case 2: _customisationManager.SelectNextTertiaryWeapon(); break;
        }
    }
    public void SelectPreviousWeapon(int weaponSlot)
    {
        switch (weaponSlot)
        {
            case 0: _customisationManager.SelectPreviousPrimaryWeapon(); break;
            case 1: _customisationManager.SelectPreviousSecondaryWeapon(); break;
            case 2: _customisationManager.SelectPreviousTertiaryWeapon(); break;
        }
    }

    public void SelectNextAbility() => _customisationManager.SelectNextAbility();
    public void SelectPreviousAbility() => _customisationManager.SelectPreviousAbility();
}

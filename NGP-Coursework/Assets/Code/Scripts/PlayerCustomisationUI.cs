using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCustomisationUI : MonoBehaviour
{
    [SerializeField] private PlayerCustomisation _playerCustomisation;


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


    private void Awake()
    {
        _playerCustomisation.OnSelectedFrameChanged     += PlayerCustomisation_OnSelectedFrameChanged;
        _playerCustomisation.OnSelectedLegChanged       += PlayerCustomisation_OnSelectedLegChanged;
        _playerCustomisation.OnSelectedWeaponChanged    += PlayerCustomisation_OnSelectedWeaponChanged;
        _playerCustomisation.OnSelectedAbilityChanged   += PlayerCustomisation_OnSelectedAbilityChanged;
    }
    private void OnDestroy()
    {
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
    }
}

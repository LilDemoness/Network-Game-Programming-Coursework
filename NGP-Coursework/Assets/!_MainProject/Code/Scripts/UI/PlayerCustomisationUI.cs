using UnityEngine;
using TMPro;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects;

namespace UI.Customisation
{
    public class PlayerCustomisationUI : MonoBehaviour
    {
        private PlayerCustomisationManager _customisationManager;
        private CustomisationOptionsDatabase customisationOptionsDatabase;
        private ulong _clientID;


        [Header("Button Text References")]
        [SerializeField] private TMP_Text _activeFrameText;

        [Space(5)]
        [SerializeField] private TMP_Text _activeLegText;

        [Space(5)]
        [SerializeField] private TMP_Text _activeAbilityText;


        [Header("Weapon Button References")]
        [SerializeField] private CanvasGroup _allOptionsGroup;
        [SerializeField] private PlayerCustomisationButtonUI[] _weaponCustomisationButtons;


        [Header("Ready Button References")]
        [SerializeField] private GameObject _readyButtonCheckmark;


        private void Awake()
        {
            PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
            PlayerCustomisationManager.OnPlayerCustomisationFinalised += PlayerCustomisationManager_OnPlayerCustomisationFinalised;
        }
        private void OnDestroy()
        {
            PlayerCustomisationManager.OnPlayerCustomisationStateChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
            PlayerCustomisationManager.OnPlayerCustomisationFinalised -= PlayerCustomisationManager_OnPlayerCustomisationFinalised;
        }
        public void Setup(PlayerCustomisationManager localCustomisationManager, ulong clientID, CustomisationOptionsDatabase customisationOptionsDatabase)
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
            UpdateReadyButton(customisationState.IsReady);
            SetSelectionLock(customisationState.IsReady);
        }
        private void UpdateUIText(ref PlayerCustomisationState customisationState)
        {
            _activeFrameText.text = customisationOptionsDatabase.FrameDatas[customisationState.FrameIndex].Name;

            _activeLegText.text = customisationOptionsDatabase.LegDatas[customisationState.LegIndex].Name;

            int activeWeaponSlots = customisationOptionsDatabase.FrameDatas[customisationState.FrameIndex].WeaponSlotCount;
            foreach(PlayerCustomisationButtonUI buttonUI in _weaponCustomisationButtons)
            {
                int index = (int)buttonUI.WeaponSlotIndex - 1;
                buttonUI.SetEnabledState(index < activeWeaponSlots);
                buttonUI.SetSelectedOptionText(customisationOptionsDatabase.WeaponDatas[customisationState.WeaponIndicies[index]].Name);
            }

            _activeAbilityText.text = customisationOptionsDatabase.AbilityDatas[customisationState.AbilityIndex].Name;
        }

        private void SetSelectionLock(bool isLocked)
        {
            if (isLocked)
            {
                // Locked.
                _allOptionsGroup.alpha = 0.5f;
                _allOptionsGroup.interactable = false;
            }
            else
            {
                // Unlocked.
                _allOptionsGroup.alpha = 1.0f;
                _allOptionsGroup.interactable = transform;
            }
        }

        private void UpdateReadyButton(bool isReady)
        {
            _readyButtonCheckmark.SetActive(isReady);
        }


        private void PlayerCustomisationManager_OnPlayerCustomisationFinalised(ulong clientID, PlayerCustomisationState customisationState)
        {
            if (this._clientID != clientID)
                return;

            // We no longer need our UI, so destroy it.
            Destroy(this.gameObject);
        }


        #region Button Called Functions

        public void SelectNextFrame() => _customisationManager.SelectNextFrame();
        public void SelectPreviousFrame() => _customisationManager.SelectPreviousFrame();

        public void SelectNextLeg() => _customisationManager.SelectNextLeg();
        public void SelectPreviousLeg() => _customisationManager.SelectPreviousLeg();

        public void SelectNextWeapon(WeaponSlotIndex weaponSlotIndex) => _customisationManager.SelectNextWeapon(weaponSlotIndex);
        public void SelectPreviousWeapon(WeaponSlotIndex weaponSlotIndex) => _customisationManager.SelectPreviousWeapon(weaponSlotIndex);

        public void SelectNextAbility() => _customisationManager.SelectNextAbility();
        public void SelectPreviousAbility() => _customisationManager.SelectPreviousAbility();


        public void ReadyButtonPressed() => _customisationManager.ToggleReady();

        #endregion
    }
}
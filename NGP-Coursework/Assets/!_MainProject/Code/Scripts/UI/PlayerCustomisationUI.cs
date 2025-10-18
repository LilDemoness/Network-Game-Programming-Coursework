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
        [SerializeField] private TMP_Text _activeWeapon1Text;
        [SerializeField] private TMP_Text _activeWeapon2Text;
        [SerializeField] private TMP_Text _activeWeapon3Text;
        [SerializeField] private TMP_Text _activeWeapon4Text;

        [Space(5)]
        [SerializeField] private TMP_Text _activeAbilityText;


        [Header("Weapon Button References")]
        [SerializeField] private CanvasGroup _allOptionsGroup;
        [SerializeField] private CanvasGroup[] _weaponButtonGroups;


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
        
            _activeWeapon1Text.text = customisationOptionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot(SlotIndex.PrimaryWeapon)).Name;
            _activeWeapon2Text.text = customisationOptionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot(SlotIndex.SecondaryWeapon)).Name;
            _activeWeapon3Text.text = customisationOptionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot(SlotIndex.TertiaryWeapon)).Name;
            _activeWeapon4Text.text = customisationOptionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot(SlotIndex.QuaternaryWeapon)).Name;

            _activeAbilityText.text = customisationOptionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot(SlotIndex.Ability)).Name;
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

        public void SelectNextSlottableData(SlotIndex slotIndex) => _customisationManager.SelectNextSlottableData(slotIndex);
        public void SelectPreviousSlottableData(SlotIndex slotIndex) => _customisationManager.SelectPreviousSlottableData(slotIndex);


        public void ReadyButtonPressed() => _customisationManager.ToggleReady();

        #endregion
    }
}
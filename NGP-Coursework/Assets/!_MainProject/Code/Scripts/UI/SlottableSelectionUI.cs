using System.Collections.Generic;
using System.Linq;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation;
using Unity.Netcode;
using UserInput;

namespace UI.Customisation
{
    public class SlottableSelectionUI : MonoBehaviour
    {
        [SerializeField] private PlayerCustomisationManager _playerCustomisationManager;
        [SerializeField] private FrameSelectionUI _frameSelectionUI;
        private FrameData _selectedFrameData;
        private SlotIndex _activeTab;


        [Header("Tabs")]
        [SerializeField] private SlottableSelectionUITab _tabButtonPrefab;
        [SerializeField] private Transform _tabButtonContainer;
        private SlottableSelectionUITab[] _tabButtons;


        [Header("Buttons")]
        [SerializeField] private SlottableSelectionUIButton _selectionButtonPrefab;
        [SerializeField] private Transform _selectionButtonContainer;
        private SlottableSelectionUIButton[] _selectionButtons;
        private int _currentPreviewSlottableIndex;


        // Events.
        public static event System.Action<int> OnSlottablePreviewSelectionChanged;



        private void Awake()
        {
            GenerateButtons();
            CleanupTabs();

            ClientInput.OnNextTabPerformed += ClientInput_OnNextTabPerformed;
            ClientInput.OnPreviousTabPerformed += ClientInput_OnPreviousTabPerformed;
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged; ;
        }
        private void OnDestroy()
        {
            ClientInput.OnNextTabPerformed -= ClientInput_OnNextTabPerformed;
            ClientInput.OnPreviousTabPerformed -= ClientInput_OnPreviousTabPerformed;
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        }

        private void ClientInput_OnNextTabPerformed()
        {
            if (!_frameSelectionUI.IsFrameSelectionScreenActive)
                SelectNextTab();
        }
        private void ClientInput_OnPreviousTabPerformed()
        {
            if (!_frameSelectionUI.IsFrameSelectionScreenActive)
                SelectPreviousTab();
        }

        private void GenerateButtons()
        {
            // Cleanup existing button instances.
            for(int i = _selectionButtonContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_selectionButtonContainer.GetChild(i).gameObject);
            }

            // Add in our buttons so that we'll always have enough.
            int maxOptionsCount = CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas.Length;
            _selectionButtons = new SlottableSelectionUIButton[maxOptionsCount];
            for (int i = 0; i < maxOptionsCount; ++i)
            {
                SlottableSelectionUIButton button = Instantiate(_selectionButtonPrefab, _selectionButtonContainer);
                button.OnPressed += SlottableSelectionButton_OnPressed;

                // Setup the button.
                button.SetupButton(CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas[i]);

                // Add to an array for future referencing (Disabling & Enabling for different Attachment Points).
                _selectionButtons[i] = button;

                // Start the button as hidden
                button.Hide();
            }
        }
        private void CleanupTabs()
        {
            // Cleanup existing button instances.
            for (int i = _tabButtonContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_tabButtonContainer.GetChild(i).gameObject);
            }
            _tabButtons = new SlottableSelectionUITab[0];   // Probably unneeded.
        }
        private void SetupTabs()
        {
            int currentTabCount = _tabButtons.Length;
            int desiredTabCount = _selectedFrameData.AttachmentPoints.Length;
            if (currentTabCount >= desiredTabCount)
                return;

            // We don't have enough tab buttons.
            // Resize our array to facilitate the addition of the new tabs.
            System.Array.Resize(ref _tabButtons, desiredTabCount);

            // Ensure we have enough
            for(int i = currentTabCount; i < desiredTabCount; ++i)
            {
                // We don't have enough tab buttons. Create a new one.
                SlottableSelectionUITab slottableSelectionUITab = Instantiate<SlottableSelectionUITab>(_tabButtonPrefab, _tabButtonContainer);
                slottableSelectionUITab.SetSlotIndex((SlotIndex)(i + 1));


                // Setup the tab.
                slottableSelectionUITab.OnPressed += SelectTab;

                // Cache a reference to our tab.
                _tabButtons[i] = slottableSelectionUITab;
            }
        }


        [ContextMenu("Set Test Frame")]
        private void SetTestFrame()
        {
            _playerCustomisationManager.SelectFrame(2);
        }

        private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, BuildData buildData)
        {
            if (clientID != NetworkManager.Singleton.LocalClientId)
                return; // Not the client.

            // Check if our selected frame has changed, and if it has update our cached data.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(buildData.ActiveFrameIndex);
            if (frameData != _selectedFrameData)
            {
                // Set our selected frame.
                SetSelectedFrame(frameData);
            }
        }


        public void SetSelectedFrame(FrameData frameData)
        {
            this._selectedFrameData = frameData;
            SetupTabs();
            SelectTab(SlotIndex.PrimaryWeapon);
        }

        [ContextMenu("Select Next")]
        public void SelectNextTab() => SelectTab(MathUtils.Loop(_activeTab.GetSlotInteger() + 1, 0, _selectedFrameData.AttachmentPoints.Length).ToSlotIndex());
        [ContextMenu("Select Prev")]
        public void SelectPreviousTab() => SelectTab(MathUtils.Loop(_activeTab.GetSlotInteger() - 1, 0, _selectedFrameData.AttachmentPoints.Length).ToSlotIndex());
        
        public void SelectTab(SlotIndex slotIndex)
        {
            if (slotIndex.GetSlotInteger() >= _selectedFrameData.AttachmentPoints.Length)
                throw new System.ArgumentException($"Active Frame '{_selectedFrameData.name}' has no Attachment Point for SlotIndex {slotIndex}");

            // Set the active tab.
            this._activeTab = slotIndex;

            // Mark the corresponding tab button as selected.
            for(int i = 0; i < _tabButtons.Length; ++i)
            {
                _tabButtons[i].SetSelectedState(i == _activeTab.GetSlotInteger());
            }

            // Disable all buttons.
            // Can we compress this & enabling into a single loop so as to not disable neccessary buttons?
            DisableAllButtons();

            // Enable the required buttons.
            foreach(SlottableData slottableData in _selectedFrameData.AttachmentPoints[_activeTab.GetSlotInteger()].ValidSlottableDatas)
            {
                int slottableIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(slottableData);
                _selectionButtons[slottableIndex].Show();
            }

            // Select the corresponding slot for the currently selected slottable.
            int selectedIndex = _playerCustomisationManager.GetClientSelectedSlottableIndex(_activeTab);
            OnSlottablePreviewSelectionChanged?.Invoke(selectedIndex);
            _selectionButtons[selectedIndex].MarkAsSelected();
        }

        private void DisableAllButtons()
        {
            for(int i = 0; i < _selectionButtons.Length; ++i)
            {
                _selectionButtons[i].Hide();
            }
        }


        private void SlottableSelectionButton_OnPressed(int slottableDataIndex)
        {
            // Ensure the selected index is valid for the active slot.
            // Change to a check made by the PlayerCustomisationManager?
            if (!_selectedFrameData.AttachmentPoints[_activeTab.GetSlotInteger()].ValidSlottableDatas.Any(t => CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(t) == slottableDataIndex))
                throw new System.ArgumentException($"You are trying to select an invalid slottable index ({slottableDataIndex}) for slot {_activeTab}");

            OnSlottablePreviewSelectionChanged?.Invoke(slottableDataIndex);

            _currentPreviewSlottableIndex = slottableDataIndex;
            EquipSelectedSlottable();
        }
        private void EquipSelectedSlottable()
        {
            Debug.Log($"Slottable {_currentPreviewSlottableIndex} Selected (Name: {CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(_currentPreviewSlottableIndex).Name})");
            _playerCustomisationManager.SelectSlottableData(_activeTab, _currentPreviewSlottableIndex);
        }
    }
}
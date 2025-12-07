using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UserInput;
using Gameplay.GameplayObjects.Players;
using System.Collections;

namespace UI.Customisation.SlottableSelection
{
    /// <summary>
    ///     Handles the display & processing of input for the Equipped and Previewed Slottables.
    /// </summary>
    public class SlottableSelectionUI : MonoBehaviour
    {
        private FrameData _selectedFrameData;
        private AttachmentSlotIndex _activeTab;


        [Header("Tabs")]
        [SerializeField] private SlottableSelectionUITab _tabButtonPrefab;
        [SerializeField] private Transform _tabButtonContainer;
        private SlottableSelectionUITab[] _tabButtons;


        [Header("Buttons")]
        [SerializeField] private SlottableSelectionUIButton _selectionButtonPrefab;
        [SerializeField] private ScrollRect _buttonsScrollRect;
        private RectTransform _selectionButtonContainer => _buttonsScrollRect.content;
        private SlottableSelectionUIButton[] _selectionButtons;
        private int _currentPreviewSlottableIndex;


        // Events.
        public static event System.Action<int> OnSlottablePreviewSelectionChanged;



        private void Awake()
        {
            GenerateButtons();
            CleanupTabs();

            PersistentPlayer.OnLocalPlayerBuildChanged += PersistentPlayer_OnLocalPlayerBuildChanged;
        }

        private void OnEnable()
        {
            ClientInput.OnNavigatePerformed += ClientInput_OnNavigatePerformed;
            ClientInput.OnNextTabPerformed += ClientInput_OnNextTabPerformed;
            ClientInput.OnPreviousTabPerformed += ClientInput_OnPreviousTabPerformed;
        }
        private IEnumerator Start()
        {
            yield return null;
            PersistentPlayer_OnLocalPlayerBuildChanged(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.BuildDataReference);    // Temp - Ensure build data is loaded initially.
        }
        private void OnDisable()
        {
            ClientInput.OnNavigatePerformed -= ClientInput_OnNavigatePerformed;
            ClientInput.OnNextTabPerformed -= ClientInput_OnNextTabPerformed;
            ClientInput.OnPreviousTabPerformed -= ClientInput_OnPreviousTabPerformed;
        }
        private void OnDestroy()
        {
            PersistentPlayer.OnLocalPlayerBuildChanged -= PersistentPlayer_OnLocalPlayerBuildChanged;
        }

        private void ClientInput_OnNavigatePerformed(Vector2 value)
        {
            if (!OverlayMenu.IsWithinActiveMenu(this.transform))
                return; // We aren't within the active menu, and so our input shouldn't be counted.

            if (value.x > 0.0f)
                SelectNextTab();
            else if (value.x < 0.0f)
                SelectPreviousTab();
        }
        private void ClientInput_OnNextTabPerformed()
        {
            if (!OverlayMenu.IsWithinActiveMenu(this.transform))
                return; // We aren't within the active menu, and so our input shouldn't be counted.
            SelectNextTab();
        }
        private void ClientInput_OnPreviousTabPerformed()
        {
            if (!OverlayMenu.IsWithinActiveMenu(this.transform))
                return; // We aren't within the active menu, and so our input shouldn't be counted.
            SelectPreviousTab();
        }

        /// <summary>
        ///     Generate <see cref="SlottableSelectionUIButton"/> for the maximum possible number of valid elements for the slottable datas.
        /// </summary>
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
                button.OnPressed += EquipSlottableDataByIndex;

                // Setup the button.
                button.SetupButton(CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas[i]);

                // Add to an array for future referencing (Disabling & Enabling for different Attachment Points).
                _selectionButtons[i] = button;

                // Start the button as hidden
                button.Hide();
            }
        }


        /// <summary>
        ///     Remove all existing Attachment Point tab groups.
        /// </summary>
        private void CleanupTabs()
        {
            // Cleanup existing button instances.
            for (int i = _tabButtonContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_tabButtonContainer.GetChild(i).gameObject);
            }
            _tabButtons = new SlottableSelectionUITab[0];   // Probably unneeded.
        }
        /// <summary>
        ///     Setup all the Attachment Point tab groups.
        /// </summary>
        private void SetupTabs()
        {
            int currentTabCount = _tabButtons.Length;
            int desiredTabCount = _selectedFrameData.AttachmentPoints.Length;


            if (currentTabCount < desiredTabCount)
            {
                // We don't have enough tab buttons.
                // Resize our array to facilitate the addition of the new tabs.
                System.Array.Resize(ref _tabButtons, desiredTabCount);

                // Ensure we have enough tab buttons.
                for(int i = currentTabCount; i < desiredTabCount; ++i)
                {
                    // We don't have enough tab buttons. Create a new one.
                    SlottableSelectionUITab slottableSelectionUITab = Instantiate<SlottableSelectionUITab>(_tabButtonPrefab, _tabButtonContainer);
                    slottableSelectionUITab.SetAttachmentSlotIndex(i.ToSlotIndex());


                    // Setup the tab.
                    slottableSelectionUITab.OnPressed += SelectTab;

                    // Cache a reference to our tab.
                    _tabButtons[i] = slottableSelectionUITab;
                }
            }
            
            // Enable all the required tab buttons.
            for(int i = 0; i < _tabButtons.Length; ++i)
            {
                if (i < desiredTabCount)
                    _tabButtons[i].Show();
                else
                    _tabButtons[i].Hide();
            }
        }


        private void PersistentPlayer_OnLocalPlayerBuildChanged(BuildData buildData)
        {
            // Check if our selected frame has changed, and if it has update our cached data.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(buildData.ActiveFrameIndex);
            if (frameData != _selectedFrameData)
            {
                // Set our selected tab.
                SetSelectedFrame(frameData);
            }
        }


        /// <summary>
        ///     Update the selected frame to the passed one and ready our tabs groups to accomodate it.
        /// </summary>
        public void SetSelectedFrame(FrameData frameData)
        {
            this._selectedFrameData = frameData;
            SetupTabs();
            Debug.Log("Select Tab");
            SelectTab(AttachmentSlotIndex.Primary);
        }

        /// <summary>
        ///     Select the next tab group.
        /// </summary>
        public void SelectNextTab() => SelectTab(MathUtils.Loop(_activeTab.GetSlotInteger() + 1, 0, _selectedFrameData.AttachmentPoints.Length).ToSlotIndex());
        /// <summary>
        ///     Select the previous tab group.
        /// </summary>
        public void SelectPreviousTab() => SelectTab(MathUtils.Loop(_activeTab.GetSlotInteger() - 1, 0, _selectedFrameData.AttachmentPoints.Length).ToSlotIndex());

        /// <summary>
        ///     Select the tab with the given AttachmentSlotIndex.
        /// </summary>
        public void SelectTab(AttachmentSlotIndex slotIndex)
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
            DisableAllSlottableButtons();

            // Enable the required buttons.
            foreach (SlottableData slottableData in _selectedFrameData.AttachmentPoints[_activeTab.GetSlotInteger()].ValidSlottableDatas)
            {
                int slottableIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(slottableData);
                _selectionButtons[slottableIndex].Show();
            }

            // Select the corresponding slot for the currently selected slottable.
            int selectedIndex = PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.GetSlottableIndex(_activeTab);
            OnSlottablePreviewSelectionChanged?.Invoke(selectedIndex);
            _selectionButtons[selectedIndex].MarkAsSelected();
        }
        /// <summary>
        ///     Disable all our <see cref="SlottableSelectionUIButton"/> instances.
        /// </summary>
        private void DisableAllSlottableButtons()
        {
            for (int i = 0; i < _selectionButtons.Length; ++i)
            {
                _selectionButtons[i].Hide();
            }
        }


        /// <summary>
        ///     Equip the Slottable Data with the corresponding index.
        /// </summary>
        private void EquipSlottableDataByIndex(int slottableDataIndex)
        {
            // Ensure the selected index is valid for the active slot.
            // Change to a check made by the PlayerCustomisationManager?
            if (!_selectedFrameData.AttachmentPoints[_activeTab.GetSlotInteger()].ValidSlottableDatas.Any(t => CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(t) == slottableDataIndex))
                throw new System.ArgumentException($"You are trying to select an invalid slottable index ({slottableDataIndex}) for slot {_activeTab}");

            OnSlottablePreviewSelectionChanged?.Invoke(slottableDataIndex);
            // Ensure that the select element is within the visible area of the Scroll Rect.
            _buttonsScrollRect.BringChildIntoView(_selectionButtons[slottableDataIndex].transform as RectTransform);

            _currentPreviewSlottableIndex = slottableDataIndex;
            EquipSelectedSlottable();
        }
        /// <summary>
        ///     Equip our selected slottable.
        /// </summary>
        private void EquipSelectedSlottable()
        {
            PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.SetSlottableServerRpc(_activeTab, _currentPreviewSlottableIndex);
        }
    }
}
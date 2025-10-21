using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UserInput;
using UI.Tables;

namespace UI.Customisation
{
    public class FrameSelectionUI : MonoBehaviour
    {
        [SerializeField] private PlayerCustomisationManager _playerCustomisationManager;
        private int _frameDataCount;


        [Header("Frame Selection")]
        [SerializeField] private GameObject _frameSelectionRoot;
        public bool IsFrameSelectionScreenActive => _frameSelectionRoot.activeSelf;

        [Space(10)]
        [SerializeField] private Transform _frameOptionsContainer;
        [SerializeField] private float _frameOptionSpacing = 200.0f;
        [SerializeField] private float _frameVerticalOffset = -35.0f;
        private int _selectedFrameIndex;
        private int _currentPreviewedFrameIndex;

        [Space(5)]
        [SerializeField] private FrameSelectionOption _frameSelectionOptionPrefab;
        private FrameSelectionOption[] _frameSelectionOptions;



        private void Awake()
        {
            SetupFrameSelectionOptions();
            HideSelectionOptions();

            SubscribeToInput();
            PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        }
        private void OnDestroy()
        {
            UnsubscribeFromInput();
            PlayerCustomisationManager.OnPlayerCustomisationStateChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;   
        }
        /// <summary>
        ///     Create FrameSelectionOption instances for each possible Frame in the game.
        /// </summary>
        private void SetupFrameSelectionOptions()
        {
            // Ensure that no instances exist from the inspector or similar.
            CleanupFrameSelectionOptions();

            // Create & Setup the FrameSelectionOption instances.
            _frameDataCount = CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas.Length;
            _frameSelectionOptions = new FrameSelectionOption[_frameDataCount];
            for (int i = 0; i < _frameDataCount; ++i)
            {
                FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(i);

                // Create & Setup the FrameSelectionOption instance.
                FrameSelectionOption frameSelectionOption = Instantiate<FrameSelectionOption>(_frameSelectionOptionPrefab, _frameOptionsContainer);
                frameSelectionOption.Setup(frameData);

                // Set the instance's name (For easier debugging in the inspector).
                frameSelectionOption.gameObject.name = "FrameSelectionOption_" + ReplaceWhitespace(frameData.Name, "-");

                // Add the option to our array for toggling
                _frameSelectionOptions[i] = frameSelectionOption;
            }
        }
        private static readonly System.Text.RegularExpressions.Regex sWhitespace = new System.Text.RegularExpressions.Regex(@"\s+");
        private static string ReplaceWhitespace(string input, string replacement) => sWhitespace.Replace(input, replacement);
        private void CleanupFrameSelectionOptions()
        {
            for(int i = _frameOptionsContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_frameOptionsContainer.GetChild(i).gameObject);
            }
        }
        private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, PlayerCustomisationState customisationState)
        {
            if (clientID != NetworkManager.Singleton.LocalClientId)
                return; // Not the client.

            _selectedFrameIndex = customisationState.FrameIndex;
            MarkActiveFrameOption();
        }


        private void SubscribeToInput()
        {
            ClientInput.OnOpenFrameSelectionPerformed += ToggleSelectionOptions;
            ClientInput.OnConfirmPerformed += ClientInput_OnConfirmPerformed;

            ClientInput.OnNextTabPerformed += SelectNextFrameOption;
            ClientInput.OnPreviousTabPerformed += SelectPreviousFrameOption;
        }
        private void UnsubscribeFromInput()
        {
            ClientInput.OnOpenFrameSelectionPerformed -= ToggleSelectionOptions;
            ClientInput.OnConfirmPerformed -= ClientInput_OnConfirmPerformed;

            ClientInput.OnNextTabPerformed -= SelectNextFrameOption;
            ClientInput.OnPreviousTabPerformed -= SelectPreviousFrameOption;
        }

        private void ClientInput_OnConfirmPerformed()
        {
            if (IsFrameSelectionScreenActive)
                SelectCurrentFrameOption();
        }



        public void SelectNextFramePressed() => throw new System.NotImplementedException();
        public void SelectPreviousFramePressed() => throw new System.NotImplementedException();


        


        public void ToggleSelectionOptions()
        {
            if (_frameSelectionRoot.activeSelf)
                HideSelectionOptions();
            else
                ShowSelectionOptions();
        }
        [ContextMenu("Show")]
        public void ShowSelectionOptions()
        {
            // Enable the selection root.
            _frameSelectionRoot.SetActive(true);

            // Start with previewing the selected frame.
            _currentPreviewedFrameIndex = _selectedFrameIndex;
            ScrollFrameOptionsToSelected(isInstant: true);      
        }
        [ContextMenu("Hide")]
        public void HideSelectionOptions() => _frameSelectionRoot.SetActive(false);


        public void SelectNextFrameOption()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex + 1, _frameDataCount);
            ScrollFrameOptionsToSelected(false);
        }
        public void SelectPreviousFrameOption()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex - 1, _frameDataCount);
            ScrollFrameOptionsToSelected(false);
        }
        /// <summary>
        ///     Scroll the Frame Options so that the selected option is in the centre of the screen.
        /// </summary>
        /// <param name="isInstant"> Should the transition take time or be instant?</param>
        private void ScrollFrameOptionsToSelected(bool isInstant)
        {
            // To-do: Implement non-instant transitions.

            // Position our frame so that the selected option is at the centre of the screen.
            float spacingBetweenOptions = (_frameSelectionOptionPrefab.transform as RectTransform).sizeDelta.x + _frameOptionSpacing;
            for(int i = 0; i < _frameSelectionOptions.Length; ++i)
            {
                // Calculate our desired position.
                int difference = i - _currentPreviewedFrameIndex;
                float horizontalSpacing = difference * spacingBetweenOptions;
                float verticalSpacing = Mathf.Abs(difference) * _frameVerticalOffset;

                // Move our frame to the desired position.
                _frameSelectionOptions[i].transform.localPosition = new Vector3(horizontalSpacing, verticalSpacing);


                // Set our option's selected state (Only the previewed frame should be selected).
                _frameSelectionOptions[i].SetSelectionState(i == _currentPreviewedFrameIndex);
            }
        }


        /// <summary>
        ///     Toggle the 'IsEquipped' sprite of the FrameSelectionOption Instances so that only the equipped one is visible.
        /// </summary>
        public void MarkActiveFrameOption()
        {
            for(int i = 0; i < _frameSelectionOptions.Length; ++i)
            {
                _frameSelectionOptions[i].SetIsActiveFrame(i == _selectedFrameIndex);
            }
        }

        /// <summary>
        ///     Set the currently previewed frame as our selected frame.
        /// </summary>
        /// <remarks> Hides the Frame Selection Options UI when performed.</remarks>
        public void SelectCurrentFrameOption()
        {
            _playerCustomisationManager.SelectFrame(_currentPreviewedFrameIndex);
            HideSelectionOptions();
        }
    }
}
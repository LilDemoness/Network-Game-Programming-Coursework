using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UserInput;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;

namespace UI.Customisation.FrameSelection
{
    /// <summary>
    ///     The UI that handles the player selecting their desired frame.
    /// </summary>
    public class FrameSelectionUI : OverlayMenu
    {
        private int _frameDataCount;


        [Header("Frame Selection")]
        [SerializeField] private GameObject _frameSelectionRoot;
        protected override GameObject RootObject => _frameSelectionRoot;


        [Space(10)]
        [SerializeField] private Transform _frameOptionsContainer;
        [SerializeField] private float _frameOptionSpacing = 200.0f;
        [SerializeField] private float _frameVerticalOffset = -35.0f;
        private int _selectedFrameIndex;
        private int _currentPreviewedFrameIndex;

        [Space(5)]
        [SerializeField] private FrameSelectionOption _frameSelectionOptionPrefab;
        private FrameSelectionOption[] _frameSelectionOptions;


        [Header("Selection Navigation")]
        [SerializeField] private Button _selectedFrameConfirmationButton;
        protected override GameObject FirstSelectedItem => _selectedFrameConfirmationButton.gameObject;


        private void Awake()
        {
            SetupFrameSelectionOptions();
            Close(false);

            SubscribeToInput();
            PersistentPlayer.OnLocalPlayerBuildChanged += PersistentPlayer_OnLocalPlayerBuildChanged;
        }
        private void Start() => PersistentPlayer_OnLocalPlayerBuildChanged(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.BuildDataReference);    // Temp - Ensure build data is loaded initially.
        private void OnDestroy()
        {
            UnsubscribeFromInput();
            PersistentPlayer.OnLocalPlayerBuildChanged -= PersistentPlayer_OnLocalPlayerBuildChanged;   
        }


        #region Frame Options Setup

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
        /// <summary>
        ///     Replace the whitespace in a given string with the desired replacement text.
        /// </summary>
        private static string ReplaceWhitespace(string input, string replacement) => sWhitespace.Replace(input, replacement);

        /// <summary>
        ///     Remove all existing frame Option Elements.
        /// </summary>
        private void CleanupFrameSelectionOptions()
        {
            for(int i = _frameOptionsContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_frameOptionsContainer.GetChild(i).gameObject);
            }

            _frameDataCount = 0;
            _frameSelectionOptions = new FrameSelectionOption[0];
        }

        #endregion


        private void PersistentPlayer_OnLocalPlayerBuildChanged(BuildData buildData)
        {
            // Update the selected frame option.
            _selectedFrameIndex = buildData.ActiveFrameIndex;
            UpdateActiveFrameIdentifier();
        }

        private void SubscribeToInput()
        {
            ClientInput.OnOpenFrameSelectionPerformed += ToggleSelectionOptions;
            ClientInput.OnConfirmPerformed += ClientInput_OnConfirmPerformed;
        }
        private void UnsubscribeFromInput()
        {
            ClientInput.OnOpenFrameSelectionPerformed -= ToggleSelectionOptions;
            ClientInput.OnConfirmPerformed -= ClientInput_OnConfirmPerformed;
        }

        private void ClientInput_OnConfirmPerformed()
        {
            if (this.IsActiveMenu)
                EquipPreviewedFrameOption();
        }



        /// <summary>
        ///     Increment our active frame (With looping).
        /// </summary>
        public void SetActiveFrameAsNext()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex + 1, _frameDataCount);
            EquipPreviewedFrameOption();
        }
        /// <summary>
        ///     Decrement our active frame (With looping).
        /// </summary>
        public void SetActiveFrameAsPrevious()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex - 1, _frameDataCount);
            EquipPreviewedFrameOption();
        }



        /// <summary>
        ///     Toggle the active state of this menu.
        /// </summary>
        public void ToggleSelectionOptions()
        {
            if (this.IsOpen)
                Close();
            else
                Open();
        }
        /// <inheritdoc/>
        public override void Open(GameObject initialSelectedObject)
        {
            base.Open(initialSelectedObject);

            // Start with previewing the selected frame.
            _currentPreviewedFrameIndex = _selectedFrameIndex;
            ScrollFrameOptionsToPreviewed(isInstant: true);      
        }
        /// <inheritdoc/>
        public override void Close(bool selectPreviousSelectable = true) => base.Close(selectPreviousSelectable);


        /// <summary>
        ///     Mark the next frame as our preview.
        /// </summary>
        public void PreviewNextFrame()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex + 1, _frameDataCount);
            ScrollFrameOptionsToPreviewed(false);
        }
        /// <summary>
        ///     Mark the previous frame as our preview.
        /// </summary>
        public void PreviewPreviousFrame()
        {
            _currentPreviewedFrameIndex = MathUtils.Loop(_currentPreviewedFrameIndex - 1, _frameDataCount);
            ScrollFrameOptionsToPreviewed(false);
        }
        /// <summary>
        ///     Scroll the Frame Options so that the currently previewed option is in the centre of the screen.
        /// </summary>
        /// <param name="isInstant"> Should the transition take time or be instant?</param>
        private void ScrollFrameOptionsToPreviewed(bool isInstant)
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
                _frameSelectionOptions[i].SetIsPreviewedFrame(i == _currentPreviewedFrameIndex);
            }
        }


        /// <summary>
        ///     Toggle the 'IsEquipped' sprite of the FrameSelectionOption Instances so that only the equipped one is visible.
        /// </summary>
        public void UpdateActiveFrameIdentifier()
        {
            for(int i = 0; i < _frameSelectionOptions.Length; ++i)
            {
                _frameSelectionOptions[i].SetIsActiveFrame(i == _selectedFrameIndex);
            }
        }

        /// <summary>
        ///     Set the currently previewed frame as our selected frame.
        /// </summary>
        /// <remarks> Hides this menu once performed.</remarks>
        public void EquipPreviewedFrameOption()
        {
            PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.SelectFrame(_currentPreviewedFrameIndex);
            Close();
        }
    }
}
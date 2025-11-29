using UnityEngine;
using Unity.Netcode;
using TMPro;
using UI.Tables;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Players;

namespace UI.Customisation.FrameSelection
{
    /// <summary>
    ///     Displays the relevant information for the currently equipped Frame.
    /// </summary>
    public class FrameInformationDisplayUI : MonoBehaviour
    {
        [Header("Selected Frame Display")]
        [SerializeField] private TMP_Text _selectedFrameName;


        [Header("Frame Stats Display")]
        [SerializeField] private InformationTableRow _sizeCategoryRow;
        [SerializeField] private InformationTableRow _healthRow;
        [SerializeField] private InformationTableRow _speedRow;
        [SerializeField] private InformationTableRow _heatCapRow;



        private void Awake() => PersistentPlayer.OnLocalPlayerBuildChanged += PersistentPlayer_OnLocalPlayerBuildChanged;
        private void Start() => PersistentPlayer_OnLocalPlayerBuildChanged(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.BuildDataReference);    // Temp - Ensure build data is loaded initially.

        private void OnDestroy() => PersistentPlayer.OnLocalPlayerBuildChanged -= PersistentPlayer_OnLocalPlayerBuildChanged;
        private void PersistentPlayer_OnLocalPlayerBuildChanged(BuildData buildData)
        {
            // Update our displayed frame information.
            FrameData frameData = buildData.GetFrameData();
            SetSelectedFrameText(frameData.Name);
            UpdateFrameStatsDisplay(frameData);
        }


        /// <summary>
        ///     Set the contents of the Selected Frame Text Element.
        /// </summary>
        public void SetSelectedFrameText(string frameName) => _selectedFrameName.text = frameName;
        /// <summary>
        ///     Update the displayed information with the parameters of the passed FrameData.
        /// </summary>
        public void UpdateFrameStatsDisplay(FrameData frameData)
        {
            _sizeCategoryRow.SetText("Size Category:", frameData.FrameSize.ToString());
            _healthRow.SetText("health:", frameData.MaxHealth.ToString());
            _speedRow.SetText("Speed:", frameData.MovementSpeed.ToString() + Units.SPEED_UNITS);
            _heatCapRow.SetText("Heat Cap:", frameData.HeatCapacity.ToString());
        }
    }
}
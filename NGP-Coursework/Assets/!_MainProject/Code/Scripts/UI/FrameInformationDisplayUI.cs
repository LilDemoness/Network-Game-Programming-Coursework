using UnityEngine;
using TMPro;
using UI.Tables;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation;
using Unity.Netcode;

namespace UI.Customisation
{
    public class FrameInformationDisplayUI : MonoBehaviour
    {
        [Header("Selected Frame Display")]
        [SerializeField] private TMP_Text _selectedFrameName;


        [Header("Frame Stats Display")]
        [SerializeField] private InformationTableRow _sizeCategoryRow;
        [SerializeField] private InformationTableRow _healthRow;
        [SerializeField] private InformationTableRow _speedRow;
        [SerializeField] private InformationTableRow _heatCapRow;



        private void Awake() => PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        
        private void OnDestroy() => PlayerCustomisationManager.OnPlayerCustomisationStateChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, PlayerCustomisationState customisationState)
        {
            if (clientID != NetworkManager.Singleton.LocalClientId)
                return; // Not the client.

            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(customisationState.FrameIndex);
            SetSelectedFrameText(frameData.Name);
            UpdateFrameStatsDisplay(frameData);
        }


        public void SetSelectedFrameText(string frameName) => _selectedFrameName.text = frameName;
        public void UpdateFrameStatsDisplay(FrameData frameData)
        {
            _sizeCategoryRow.SetText("Size Category:", frameData.FrameSize.ToString());
            _healthRow.SetText("health:", frameData.MaxHealth.ToString());
            _speedRow.SetText("Speed:", frameData.MovementSpeed.ToString() + Units.SPEED_UNITS);
            _heatCapRow.SetText("Heat Cap:", frameData.HeatCapacity.ToString());
        }
    }
}
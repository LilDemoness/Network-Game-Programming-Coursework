using UnityEngine;
using TMPro;
using UI.Tables;
using Gameplay.Actions.Definitions;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace UI.Customisation.SlottableSelection
{
    /// <summary>
    ///     Displays the relevant information for the currently previewed Slottable.
    /// </summary>
    public class SlottableInformationDisplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        [Space(5)]
        [SerializeField] private InformationTableRow[] _informationTable = new InformationTableRow[INFORMATION_TABLE_ROWS];
        private const int INFORMATION_TABLE_ROWS = 7;


        private void Awake() => SlottableSelectionUI.OnSlottablePreviewSelectionChanged += SlottableSelectionUI_OnSlottablePreviewSelectionChanged;
        private void OnDestroy() => SlottableSelectionUI.OnSlottablePreviewSelectionChanged -= SlottableSelectionUI_OnSlottablePreviewSelectionChanged;



        private void SlottableSelectionUI_OnSlottablePreviewSelectionChanged(int slottableDataIndex)
        {
            UpdateDisplayInformation(CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(slottableDataIndex));
        }
        /// <summary>
        ///     Update the displayed information for the given SlottableData.
        /// </summary>
        /// <param name="slottableData"></param>
        private void UpdateDisplayInformation(SlottableData slottableData)
        {
            // Basic Data.
            _nameText.text = slottableData.Name;
            _descriptionText.text = slottableData.Description;

            if (slottableData.AssociatedAction == null)
            {
                Debug.LogWarning("No Associated Action Set");
                SetTableForInvalidData();
                return;
            }

            // Table Data.
            SetHeatRow(0, slottableData.AssociatedAction);
            SetRangeRow(1, slottableData.AssociatedAction);
            SetUseTimeRow(2, slottableData.AssociatedAction);
            SetUseRateRow(3, slottableData.AssociatedAction);
            SetChargeTimeRow(4, slottableData.AssociatedAction);
            SetCooldownRow(5, slottableData.AssociatedAction);
            SetBurstCountRow(6, slottableData.AssociatedAction);
        }


        #region Row Update Methods

        private void SetHeatRow(int rowIndex, ActionDefinition action) => _informationTable[rowIndex].SetText("Activation Heat:", action.ImmediateHeat.ToString());
        private void SetRangeRow(int rowIndex, ActionDefinition action)
        {
            string rangeText = action switch
            {
                RangedRaycastAction => (action as RangedRaycastAction).MaxRange + Units.DISTANCE_UNITS,
                RangedProjectileAction => (action as RangedProjectileAction).MaxRange + Units.DISTANCE_UNITS,
                AoETargetingAction => (action as AoETargetingAction).RangeString,
                SelfTargetingAction => "Self",
                _ => throw new System.NotImplementedException(),
            };

            _informationTable[rowIndex].SetText("Range:", rangeText);
        }
        private void SetUseTimeRow(int rowIndex, ActionDefinition action) => _informationTable[rowIndex].SetText("Use Time: ", (action.ExecutionDelay > 0.0f ? action.ExecutionDelay + Units.TIME_UNITS : "Instant"));
        private void SetUseRateRow(int rowIndex, ActionDefinition action)
        {
            string retriggerText = action.TriggerType switch
            {
                Gameplay.Actions.ActionTriggerType.Single => "Single",
                Gameplay.Actions.ActionTriggerType.Burst => "Single",
                Gameplay.Actions.ActionTriggerType.Repeated => (1.0f / action.RetriggerDelay) + ("/" + Units.TIME_UNITS),
                Gameplay.Actions.ActionTriggerType.RepeatedBurst => (action.RetriggerDelay + Units.TIME_UNITS),
                _ => throw new System.NotImplementedException(),
            };

            _informationTable[rowIndex].SetText("Use Rate: ", retriggerText);
        }
        private void SetChargeTimeRow(int rowIndex, ActionDefinition action) => _informationTable[rowIndex].SetText("Charge Time: ", (action.CanCharge ? action.MaxChargeTime : 0.0f).ToString() + Units.TIME_UNITS);
        private void SetCooldownRow(int rowIndex, ActionDefinition action) => _informationTable[rowIndex].SetText("Cooldown: ", (action.HasCooldown ? action.ActionCooldown : 0.0f).ToString() + Units.TIME_UNITS);
        private void SetBurstCountRow(int rowIndex, ActionDefinition action)
        {
            string burstsText = action.Bursts > 0 ? (action.Bursts.ToString() + " in " + (action.Bursts * action.BurstDelay) + Units.TIME_UNITS) : "N/A";
            _informationTable[rowIndex].SetText("Burst Count: ", burstsText);
        }


        /// <summary>
        ///     Update the table with null values.
        /// </summary>
        private void SetTableForInvalidData()
        {
            _informationTable[0].SetText("Heat: ", "null");
            _informationTable[1].SetText("Range: ", "null");
            _informationTable[2].SetText("Use Time: ", "null");
            _informationTable[3].SetText("Use Rate: ", "null");
            _informationTable[4].SetText("Charge Time: ", "null");
            _informationTable[5].SetText("Cooldown: ", "null");
            _informationTable[6].SetText("Burst Count: ", "null");
        }

        #endregion
    }
}
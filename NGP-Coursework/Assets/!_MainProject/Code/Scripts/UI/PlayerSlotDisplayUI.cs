using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;
using TMPro;
using UI.Icons;
using UnityEngine;
using UserInput;

public class PlayerSlotDisplayUI : MonoBehaviour
{
    [SerializeField] private AttachmentSlotIndex _slotIndex = AttachmentSlotIndex.Unset;
    [SerializeField] private TextMeshProUGUI _slotNameLabel;
    [SerializeField] private TextMeshProUGUI _activationIconText;


    private void Awake()
    {
        if (_slotIndex == AttachmentSlotIndex.Unset)
        {
            Debug.LogError($"Error: {this.name} has an unset Slot Index", this);
            return;
        }

        Player.OnLocalPlayerBuildUpdated += Player_OnLocalPlayerBuildUpdated;

        InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateSpriteIdentifiers;
        InputIconManager.OnSpriteAssetChanged += UpdateSpriteIdentifiers;
        UpdateSpriteIdentifiers();
    }
    private void OnDestroy()
    {
        Player.OnLocalPlayerBuildUpdated -= Player_OnLocalPlayerBuildUpdated;

        InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateSpriteIdentifiers;
        InputIconManager.OnSpriteAssetChanged -= UpdateSpriteIdentifiers;
    }


    private void Player_OnLocalPlayerBuildUpdated(BuildData buildData)
    {
        if (buildData.GetFrameData().AttachmentPoints.Length < (int)_slotIndex)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(true);
            SetName(buildData.GetSlottableData(_slotIndex).Name);
        }
    }
    private void SetName(string name) => _slotNameLabel.text = name;

    private void UpdateSpriteIdentifiers()
    {
        _activationIconText.spriteAsset = InputIconManager.GetSpriteAsset();
        _activationIconText.text = InputIconManager.GetIconIdentifierForAction(ClientInput.GetSlotActivationAction(_slotIndex));
    }
}

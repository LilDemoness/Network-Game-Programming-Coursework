using Gameplay.GameplayObjects.Character.Customisation.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Customisation
{
    public class FrameSelectionOption : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _frameNameText;
        [SerializeField] private Image _frameSpriteImage;

        [Space(5)]
        [SerializeField] private TMP_Text _sizeCategoryText;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _speedText;
        [SerializeField] private TMP_Text _heatCapText;

        [Space(5)]
        [SerializeField] private GameObject _deselectedOverlayGO;
        [SerializeField] private GameObject _isActiveFrameGO;


        public void Setup(FrameData frameData)
        {
            this._frameNameText.text = frameData.Name;
            //this._frameSpriteImage.sprite = frameData.Sprite;

            _sizeCategoryText.text = frameData.FrameSize.ToString();
            _healthText.text = frameData.MaxHealth.ToString();
            _speedText.text = frameData.MovementSpeed.ToString() + Units.SPEED_UNITS;
            _heatCapText.text = frameData.HeatCapacity.ToString();
        }
        public void SetSelectionState(bool isSelected) => _deselectedOverlayGO.SetActive(!isSelected);
        public void SetIsActiveFrame(bool isActiveFrame) => _isActiveFrameGO.SetActive(isActiveFrame);
    }
}
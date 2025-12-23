using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace UI.Customisation.FrameSelection
{
    /// <summary>
    ///     The data visuals for a given frame within the <see cref="FrameSelectionUI"/>.
    /// </summary>
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


        /// <summary>
        ///     Setup this FrameSelectionOption with the parameters of the passed FrameData.
        /// </summary>
        public void Setup(FrameData frameData)
        {
            this._frameNameText.text = frameData.Name;
            this._frameSpriteImage.sprite = frameData.Sprite;

            _sizeCategoryText.text = frameData.FrameSize.ToString();
            _healthText.text = frameData.MaxHealth.ToString();
            _speedText.text = frameData.MovementSpeed.ToString() + Units.SPEED_UNITS;
            _heatCapText.text = frameData.HeatCapacity.ToString();
        }

        /// <summary>
        ///     Set whether this option currently previewed or not.
        /// </summary>
        public void SetIsPreviewedFrame(bool isPreviewedFrame) => _deselectedOverlayGO.SetActive(!isPreviewedFrame);
        /// <summary>
        ///     Set whether this element is the active frame or not.
        /// </summary>
        public void SetIsActiveFrame(bool isActiveFrame) => _isActiveFrameGO.SetActive(isActiveFrame);
    }
}
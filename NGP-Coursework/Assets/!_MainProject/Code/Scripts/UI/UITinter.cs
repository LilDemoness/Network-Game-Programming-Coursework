using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    /// <summary>
    ///     Applies a tint to a UI Image.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UITinter : MonoBehaviour
    {
        [SerializeField] private Color[] _tintColours;
        private Image _image;

        private void Awake() => _image = GetComponent<Image>();

        public void SetToColour(int colourIndex)
        {
            if (colourIndex >= _tintColours.Length)
                return; // Index out of range.

            _image.color = _tintColours[colourIndex];
        }
    }
}
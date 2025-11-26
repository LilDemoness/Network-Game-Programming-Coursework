using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    ///     A script to play a basic animation by rotating the object.
    /// </summary>
    public class ConnectionAnimation : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = -50.0f;

        private void Update()
        {
            transform.Rotate(new Vector3(0.0f, 0.0f, _rotationSpeed * Mathf.PI * Time.deltaTime));
        }
    }
}
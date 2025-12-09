using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     Utility class to mark an object as 'DontDestroyOnLoad'
    /// </summary>
    public class DontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}
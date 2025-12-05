using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ClientLoadingScreen : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;


        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
        private void Start()
        {
            SetCanvasVisibility(false);
        }
        private void OnDestroy()
        {
            
        }
        private void Update()
        {
            
        }


        public void StartLoadingScreen(string sceneName)
        {
            SetCanvasVisibility(true);
        }
        public void UpdateLoadingScreen(string sceneName)
        {

        }
        public void StopLoadingScreen()
        {
            SetCanvasVisibility(false);
        }


        private void SetCanvasVisibility(bool visibility)
        {
            _canvas.enabled = visibility;
        }
    }
}
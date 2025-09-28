using UnityEngine;
using Gameplay.Actions;

namespace Gameplay.GameplayObjects
{
    public class GameDataSource : MonoBehaviour
    {
        public static GameDataSource Instance { get; private set; }


        [SerializeField] private Action m_generalChaseActionPrototype;
        [SerializeField] private Action m_generalTargetActionPrototype;
        [SerializeField] private Action m_stunnedActionPrototype;


        public Action GeneralChaseActionPrototype => m_generalChaseActionPrototype;
        public Action GeneralTargetActionPrototype => m_generalTargetActionPrototype;
        public Action StunnedActionPrototype => m_stunnedActionPrototype;


        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined");
            }

            DontDestroyOnLoad(this.gameObject);
            Instance = this;
        }
    }
}
using System.Collections.Generic;
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


        [Tooltip("All Action Prototype Scriptable Objects")]
        [SerializeField] private Action[] _actionPrototypes;


        public Action GeneralChaseActionPrototype => m_generalChaseActionPrototype;
        public Action GeneralTargetActionPrototype => m_generalTargetActionPrototype;
        public Action StunnedActionPrototype => m_stunnedActionPrototype;

        private List<Action> _allActions;


        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined");
            }

            BuildActionIDs();

            DontDestroyOnLoad(this.gameObject);
            Instance = this;
        }
        private void BuildActionIDs()
        {
            HashSet<Action> uniqueActions = new HashSet<Action>(_actionPrototypes);

            // Add our General Action Prototypes.
            uniqueActions.Add(GeneralChaseActionPrototype);
            uniqueActions.Add(GeneralTargetActionPrototype);
            uniqueActions.Add(StunnedActionPrototype);

            _allActions = new List<Action>(uniqueActions.Count);


            // Add all our unique actions to '_allActions' and set their IDs to match.
            int i = 0;
            foreach(Action uniqueAction in uniqueActions)
            {
                uniqueAction.ActionID = new ActionID { ID = i };
                _allActions.Add(uniqueAction);
                ++i;
            }
        }


        public Action GetActionPrototypeByID(ActionID index)
        {
            return _allActions[index.ID];
        }
        public bool TryGetActionPrototypeById(ActionID index, out Action action)
        {
            for(int i = 0; i < _allActions.Count; ++i)
            {
                if (_allActions[i].ActionID == index)
                {
                    action = _allActions[i];
                    return true;
                }
            }

            action = null;
            return false;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Actions;

namespace Gameplay.GameplayObjects
{
    public class GameDataSource : MonoBehaviour
    {
        public static GameDataSource Instance { get; private set; }


        [SerializeField] private Action m_generalChaseActionDefinition;
        [SerializeField] private Action m_generalTargetActionDefinition;
        [SerializeField] private Action m_stunnedActionDefinition;


        [Tooltip("All Action Prototype Scriptable Objects")]
        [SerializeField] private Action[] _actionDefinitions;


        public Action GeneralChaseActionDefinition => m_generalChaseActionDefinition;
        public Action GeneralTargetActionDefinition => m_generalTargetActionDefinition;
        public Action StunnedActionDefinition => m_stunnedActionDefinition;

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
            HashSet<Action> uniqueActions = new HashSet<Action>(_actionDefinitions);

            // Add our General Action Prototypes.
            uniqueActions.Add(m_generalChaseActionDefinition);
            uniqueActions.Add(m_generalTargetActionDefinition);
            uniqueActions.Add(m_stunnedActionDefinition);

            _allActions = new List<Action>(uniqueActions.Count);


            // Add all our unique actions to '_allActions' and set their IDs to match.
            int i = 0;
            foreach(Action uniqueAction in uniqueActions)
            {
                uniqueAction.ActionID = new ActionID() { ID = i };
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
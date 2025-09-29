using System.Collections.Generic;
using UnityEngine;
using Gameplay.Actions;

namespace Gameplay.GameplayObjects
{
    public class GameDataSource : MonoBehaviour
    {
        public static GameDataSource Instance { get; private set; }


        [SerializeField] private ActionDefinition m_generalChaseActionDefinition;
        [SerializeField] private ActionDefinition m_generalTargetActionDefinition;
        [SerializeField] private ActionDefinition m_stunnedActionDefinition;


        [Tooltip("All Action Prototype Scriptable Objects")]
        [SerializeField] private ActionDefinition[] _actionDefinitions;


        public ActionDefinition GeneralChaseActionDefinition => m_generalChaseActionDefinition;
        public ActionDefinition GeneralTargetActionDefinition => m_generalTargetActionDefinition;
        public ActionDefinition StunnedActionDefinition => m_stunnedActionDefinition;

        private List<ActionDefinition> _allActions;


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
            HashSet<ActionDefinition> uniqueActions = new HashSet<ActionDefinition>(_actionDefinitions);

            // Add our General Action Prototypes.
            uniqueActions.Add(m_generalChaseActionDefinition);
            uniqueActions.Add(m_generalTargetActionDefinition);
            uniqueActions.Add(m_stunnedActionDefinition);

            _allActions = new List<ActionDefinition>(uniqueActions.Count);


            // Add all our unique actions to '_allActions' and set their IDs to match.
            int i = 0;
            foreach(ActionDefinition uniqueAction in uniqueActions)
            {
                uniqueAction.SetActionID(i);
                _allActions.Add(uniqueAction);
                ++i;
            }
        }


        public ActionDefinition GetActionDefinitionByID(ActionID index)
        {
            return _allActions[index.ID];
        }
        public bool TryGetActionDefinitionById(ActionID index, out ActionDefinition action)
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
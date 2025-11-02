using System.Collections.Generic;
using UnityEngine.Pool;
using Gameplay.GameplayObjects;

namespace Gameplay.Actions
{
    /// <summary>
    ///     Creates and manages Actions and their Object Pools.
    /// </summary>
    public static class ActionFactory
    {
        private static Dictionary<ActionID, ObjectPool<Action>> s_actionPools = new Dictionary<ActionID, ObjectPool<Action>>();

        /// <summary>
        ///     Retrieve the Object Pool for the passed ActionID. 
        /// </summary>
        /// <remarks> Creates a new ObjectPool if one doesn't exist.</remarks>
        private static ObjectPool<Action> GetActionPool(ActionID actionID)
        {
            if (!s_actionPools.TryGetValue(actionID, out var actionPool))
            {
                // We don't yet have a pool for this action type. Create one.
                actionPool = new ObjectPool<Action>(
                    createFunc: () => new Action(definition: GameDataSource.Instance.GetActionDefinitionByID(actionID)),
                    actionOnRelease: action => action.ReturnToPool());

                s_actionPools.Add(actionID, actionPool);
            }

            return actionPool;
        }


        /// <summary>
        ///     Factory method that creates Actions from their request data.
        /// </summary>
        /// <param name="data"> The Data to instantiate this action from.</param>
        /// <returns> The newly created action.</returns>
        public static Action CreateActionFromData(ref ActionRequestData data)
        {
            var returnAction = GetActionPool(data.ActionID).Get();
            returnAction.Initialise(ref data);
            return returnAction;
        }


        /// <summary>
        ///     Return the given action to its corresponding object pool.
        /// </summary>
        public static void ReturnAction(Action action) => GetActionPool(action.ActionID).Release(action);
        /// <summary>
        ///     Delete all pooled action instances.
        /// </summary>
        public static void PurgePooledActions()
        {
            foreach(var actionPool in s_actionPools.Values)
            {
                actionPool.Clear();
            }
        }
    }
}
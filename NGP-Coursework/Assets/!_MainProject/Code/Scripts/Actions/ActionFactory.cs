using System.Collections.Generic;
using UnityEngine.Pool;
using Gameplay.GameplayObjects;
using Object = UnityEngine.Object;

namespace Gameplay.Actions
{
    public static class ActionFactory
    {
        private static Dictionary<ActionID, ObjectPool<Action>> s_actionPools = new Dictionary<ActionID, ObjectPool<Action>>();

        private static ObjectPool<Action> GetActionPool(ActionID actionID)
        {
            if (!s_actionPools.TryGetValue(actionID, out var actionPool))
            {
                // We don't yet have a pool for this action type. Create one.
                actionPool = new ObjectPool<Action>(
                    createFunc: () => Object.Instantiate(GameDataSource.Instance.GetActionPrototypeByID(actionID)),
                    actionOnRelease: action => action.Reset(),
                    actionOnDestroy: Object.Destroy);

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


        public static void ReturnAction(Action action) => GetActionPool(action.ActionID).Release(action);
        public static void PurgePooledActions()
        {
            foreach(var actionPool in s_actionPools.Values)
            {
                actionPool.Clear();
            }
        }
    }
}
using Gameplay.GameplayObjects.Players;
using Infrastructure;
using UnityEngine;

namespace Gameplay.GameplayObjects
{
    /// <summary>
    ///     A runtime list of <see cref="PersistentPlayer"/> objects that is populated on both clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class PersistentPlayerRuntimeCollection : RuntimeCollection<PersistentPlayer>
    {
        public bool TryGetPlayer(ulong clientID, out PersistentPlayer persistentPlayer)
        {
            for(int i = 0; i < Items.Count; ++i)
            {
                if (Items[i].OwnerClientId == clientID)
                {
                    // Found the matching player.
                    persistentPlayer = Items[i];
                    return true;
                }
            }

            // No PersistentPlayers with the requested ClientId.
            persistentPlayer = null;
            return false;
        }
    }
}
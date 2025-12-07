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
        public PersistentPlayerRuntimeCollection()
        {
            ItemAdded += PersistentPlayerRuntimeCollection_ItemAdded;
        }
        ~PersistentPlayerRuntimeCollection()
        {
            ItemAdded -= PersistentPlayerRuntimeCollection_ItemAdded;
        }

        private void PersistentPlayerRuntimeCollection_ItemAdded(PersistentPlayer obj)
        {
            Debug.Log("Added: " + obj.OwnerClientId);
        }

        public bool TryGetPlayer(ulong clientID, out PersistentPlayer persistentPlayer)
        {
            for(int i = 0; i < Items.Count; ++i)
            {
                Debug.Log("Searching: " + Items[i].OwnerClientId);
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
        public bool TryGetPlayer(int playerIndex, out PersistentPlayer persistentPlayer)
        {
            if (playerIndex != -1)  // Player Index '-1' is an unset value.
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    if (Items[i].PlayerNumber.Value == playerIndex)
                    {
                        // Found the matching player.
                        persistentPlayer = Items[i];
                        return true;
                    }
                }
            }

            // No PersistentPlayers with the requested Player Index.
            persistentPlayer = null;
            return false;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    public abstract class NetworkGameplayState : NetworkBehaviour
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerCollection;


        // Player Respawning.
        protected const bool USE_GROUPED_RESPAWNS = true;  // If true, respawns are grouped together at the time which is the nearest multiple of '_respawnDelay'.
        protected const float RESPAWN_DELAY = 10.0f;
        protected const float MIN_RESPAWN_DELAY = 2.0f;
        protected Dictionary<ServerCharacter, (float Time, bool ShouldAutoRespawn)> CharacterToRespawnInfoDictionary = new();

        public static event System.Action<float> OnLocalPlayerRespawnStarted;


        public static event System.Action OnLocalPlayerInitialCustomisationRequested;
        private static Dictionary<ulong, System.Action<ulong>> s_initialCustomisationPrompts = new();



        public abstract void Initialise(ServerCharacter[] playerCharacters, ServerCharacter[] npcCharacters);
        public abstract void AddPlayer(ServerCharacter playerCharacter);
        public abstract void AddNPC(ServerCharacter npcCharacter);


        public abstract void OnPlayerLeft(ulong clientId);
        public void OnPlayerReconnected(ulong clientId, ServerCharacter newServerCharacter) => OnPlayerReconnected(GetPlayerIndex(clientId), newServerCharacter);
        public abstract void OnPlayerReconnected(int playerIndex, ServerCharacter newServerCharacter);


        protected int GetPlayerIndex(ulong clientId)
        {
            if (!_persistentPlayerCollection.TryGetPlayer(clientId, out PersistentPlayer persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for Client {clientId}");

            return persistentPlayer.PlayerNumber.Value;
        }
        protected int GetTeamIndex(ulong clientId)
        {
            if (!_persistentPlayerCollection.TryGetPlayer(clientId, out PersistentPlayer persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for Client {clientId}");

            return persistentPlayer.TeamIndex.Value;
        }



        protected virtual void Update()
        {
            // Likely lots of Garbage generated here. Find a better way.
            foreach (ServerCharacter characterToRespawn in CharacterToRespawnInfoDictionary.Keys.ToArray())
            {
                if (!CharacterToRespawnInfoDictionary[characterToRespawn].ShouldAutoRespawn)
                    continue;

                if (Time.time > CharacterToRespawnInfoDictionary[characterToRespawn].Time)
                {
                    PerformRespawn(characterToRespawn);
                }
            }
        }



        /// <summary>
        ///     Create a respawn request for a ServerCharacter.<br/>
        ///     If the ServerCharacter is a Player, then notify the owning client with the respawn delay.
        /// </summary>
        public void StartRespawn(ServerCharacter serverCharacter)
        {
            float respawnTime = GetRespawnTime();
            CharacterToRespawnInfoDictionary.Add(serverCharacter, (respawnTime, true));

            if (serverCharacter.GetComponent<Player>() != null)
                NotifyOwnerOfRespawnAttemptRpc(respawnTime - Time.time, RpcTarget.Group(new[] { serverCharacter.OwnerClientId }, RpcTargetUse.Temp));
        }
        /// <summary>
        ///     Perform a Respawn for the given ServerCharacter.
        /// </summary>
        protected void PerformRespawn(ServerCharacter serverCharacter)
        {
            CharacterToRespawnInfoDictionary.Remove(serverCharacter);

            EntitySpawnPoint spawnPoint = EntitySpawnPoint.GetRandomSpawnPoint(EntitySpawnPoint.EntityTypes.Player, -1);
            serverCharacter.RespawnCharacter(spawnPoint.transform.position, spawnPoint.transform.rotation);
            spawnPoint.SpawnAtPoint();
        }
        /// <summary>
        ///     Prevent a respawning ServerCharacter from actually respawning, though their respawn time remaining still decreases.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void PreventRespawnServerRpc(ulong serverCharacterNetworkObject) => PreventRespawn(NetworkManager.SpawnManager.SpawnedObjects[serverCharacterNetworkObject].GetComponent<ServerCharacter>());

        /// <summary>
        ///     Prevent a respawning ServerCharacter from actually respawning, though their respawn time remaining still decreases.<br/>
        ///     Server-only.
        /// </summary>
        protected void PreventRespawn(ServerCharacter serverCharacter)
        {
            if (!CharacterToRespawnInfoDictionary.ContainsKey(serverCharacter))
                throw new System.Exception("We are trying to pause the respawn of a non-respawning player");

            CharacterToRespawnInfoDictionary[serverCharacter] = (CharacterToRespawnInfoDictionary[serverCharacter].Time, false);
        }

        /// <summary>
        ///     Allow a respawning ServerCharacter that previously had its respawning prevented to respawn.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void AllowRespawnServerRpc(ulong serverCharacterNetworkObject) => AllowRespawn(NetworkManager.SpawnManager.SpawnedObjects[serverCharacterNetworkObject].GetComponent<ServerCharacter>());
        /// <summary>
        ///     Allow a respawning ServerCharacter that previously had its respawning prevented to respawn.<br/>
        ///     Server-only.
        /// </summary>
        protected void AllowRespawn(ServerCharacter serverCharacter)
        {
            if (!CharacterToRespawnInfoDictionary.ContainsKey(serverCharacter))
                throw new System.Exception("We are trying to resume the respawn of a non-respawning player");

            CharacterToRespawnInfoDictionary[serverCharacter] = (CharacterToRespawnInfoDictionary[serverCharacter].Time, true);
        }
        /// <summary>
        ///     Notify a client that their local player is respawning, passing them the respawnDelay for things like Respawn Screens.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifyOwnerOfRespawnAttemptRpc(float respawnDelay, RpcParams rpcParams = default) => OnLocalPlayerRespawnStarted?.Invoke(respawnDelay);
        

        /// <summary>
        ///     Get the time when a ServerCharacter should respawn.
        /// </summary>
        protected virtual float GetRespawnTime()
        {
            if (USE_GROUPED_RESPAWNS)
            {
                //float serverTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
                float time = Time.time;
                float respawnTime = Mathf.Ceil(time / RESPAWN_DELAY) * RESPAWN_DELAY;
                float respawnDelay = respawnTime - time;
                return respawnDelay >= MIN_RESPAWN_DELAY ? respawnTime : respawnTime + RESPAWN_DELAY; // Prevent being under our minimum respawn time by moving to the next multiple if we are below.
            }
            //else
            //    return RESPAWN_DELAY;
        }


        public void PromptInitialCustomisation(ulong clientId, System.Action<ulong> onCompleteCallback)
        {
            PromptInitialCustomisationRpc(RpcTarget.Group(new[] { clientId }, RpcTargetUse.Temp));
            s_initialCustomisationPrompts.Add(clientId, onCompleteCallback);
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void PromptInitialCustomisationRpc(RpcParams rpcParams = default) => OnLocalPlayerInitialCustomisationRequested?.Invoke();
        [Rpc(SendTo.Server)]
        public void InitialCustomisationPromptCompletedServerRpc(RpcParams rpcParams = default)
        {
            Debug.Log("Initial Customisation Completed For Client: " + rpcParams.Receive.SenderClientId);
            if (!s_initialCustomisationPrompts.TryGetValue(rpcParams.Receive.SenderClientId, out System.Action<ulong> onCompleteCallback))
                throw new System.Exception();

            onCompleteCallback?.Invoke(rpcParams.Receive.SenderClientId);
        }



        /// <summary>
        ///     Increment the score of the passed ServerCharacter, if possible.
        /// </summary>
        public abstract void IncrementScore(ServerCharacter serverCharacter);


        /// <summary>
        ///     Server-only function to save this NetworkGameplayState's data to the Persistent State.
        /// </summary>
        public abstract void SavePersistentData(ref PersistentGameState persistentGameState);
    }
}
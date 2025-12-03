using System;
using System.Collections.Generic;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the Gameplay states that include Teams.
    /// </summary>
    public class NetworkTeamGameplayState : NetworkGameplayState
    {
        public NetworkList<TeamGameData> TeamData;  // In no specific order.
        private Dictionary<int, TeamGameData> _teamIndexToDataDict = new Dictionary<int, TeamGameData>();


        public override void SavePersistentData(ref PersistentGameState persistentGameState)
        {
            // Create the PostGameData.
            PostGameData[] postGameData = null;

            // Set the PostGameData.
            persistentGameState.GameData = postGameData;
            persistentGameState.UseTeams = true;
        }


        public override void OnNetworkSpawn()
        {
            TeamData.OnListChanged += OnTeamDataChanged;
        }
        public override void OnNetworkDespawn()
        {
            if (TeamData != null)
                TeamData.OnListChanged -= OnTeamDataChanged;
        }


        public override void Initialise(ulong[] clientIds)
        {
            // Get all unique TeamIDs.
            HashSet<int> teamIndicies = new HashSet<int>(); // All elements within a HashSet are unique.
            for(int i = 0; i < clientIds.Length; ++i)
                teamIndicies.Add(GetTeamIndex(clientIds[i]));
            
            // Populate our TeamData Network Variable.
            foreach (int teamIndex in teamIndicies)
            {
                TeamData.Add(new TeamGameData(teamIndex));
                // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.
            }
        }
        public override void AddPlayer(ulong clientId)
        {
            int teamIndex = GetTeamIndex(clientId);
            if (teamIndex == -1)
                return; // No Team, and we don't have logic for this.

            // Create a new team if the player should be in a new team.
            if (!_teamIndexToDataDict.ContainsKey(teamIndex))
            {
                TeamData.Add(new TeamGameData(teamIndex)); // New team.
                // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.
            }
        }


        private void OnTeamDataChanged(NetworkListEvent<TeamGameData> changeEvent)
        {
            // Update our cached value.
            switch (changeEvent.Type)
            {
                // New Entry.
                case NetworkListEvent<TeamGameData>.EventType.Add:
                case NetworkListEvent<TeamGameData>.EventType.Insert:
                    {
                        TeamGameData teamData = changeEvent.Value;
                        teamData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).
                        _teamIndexToDataDict.Add(changeEvent.Value.TeamIndex, teamData);
                        break;
                    }

                // Entry Changed.
                case NetworkListEvent<TeamGameData>.EventType.Value:
                    {
                        TeamGameData teamData = changeEvent.Value;
                        teamData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).
                        _teamIndexToDataDict[changeEvent.Value.TeamIndex] = teamData;
                        break;
                    }

                // Removal.
                case NetworkListEvent<TeamGameData>.EventType.Remove:
                case NetworkListEvent<TeamGameData>.EventType.RemoveAt:
                    {
                        _teamIndexToDataDict.Remove(changeEvent.Value.TeamIndex);
                        return;
                    }

                // Other.    
                case NetworkListEvent<TeamGameData>.EventType.Clear:
                    {
                        _teamIndexToDataDict.Clear();
                        return;
                    }
                case NetworkListEvent<TeamGameData>.EventType.Full:
                    {
                        for (int i = 0; i < TeamData.Count; ++i)
                        {
                            TeamGameData teamData = TeamData[i];
                            teamData.ListIndex = i;  // Allows for easier retrieval when editing (Mainly on the Server).

                            if (!_teamIndexToDataDict.TryAdd(teamData.TeamIndex, teamData))
                            {
                                _teamIndexToDataDict[teamData.TeamIndex] = teamData;
                            }
                        }
                        break;
                    }
            }
        }


        public override void IncrementScore(ServerCharacter serverCharacter)
        {
            // Retrieve the character's TeamID.
            int teamIndex = serverCharacter.TeamID.Value;

            // Retrieve the Team Data for editing.
            int index = GetListIndexForTeamIndex(teamIndex);
            TeamGameData teamData = TeamData[index];

            // Increment Score.
            teamData.Score += 1;

            // Apply our changes.
            TeamData[index] = teamData;
            Debug.Log($"Team {TeamData[index].TeamIndex} - New Score: {TeamData[index].Score} - Gained By '{serverCharacter.CharacterName}'");
        }
        private int GetListIndexForTeamIndex(int teamIndex)
        {
            if (_teamIndexToDataDict.ContainsKey(teamIndex))
            {
                int index = _teamIndexToDataDict[teamIndex].ListIndex;

                if (index == -1)
                    throw new System.Exception($"The Cached Team with Index {teamIndex} has an invalid ListPosition value");

                return index;
            }

            throw new System.Exception($"No Team Cached with Index {teamIndex}");
        }


        public struct TeamGameData : INetworkSerializable, IEquatable<TeamGameData>
        {
            public int TeamIndex;   // Can be used to retrieve corresponding players.
            public int Score;

            [System.NonSerialized] public int ListIndex; // A non-serialized, non-synced int representing this team's position in the TeamData array. Used on the server for easier retrieval of data.


            public TeamGameData(int teamIndex)
            {
                this.TeamIndex = teamIndex;
                this.Score = 0;
                this.ListIndex = -1;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref TeamIndex);
                serializer.SerializeValue(ref Score);
            }
            public bool Equals(TeamGameData other) => (this.TeamIndex, this.Score) == (other.TeamIndex, other.Score);
        }
    }
}
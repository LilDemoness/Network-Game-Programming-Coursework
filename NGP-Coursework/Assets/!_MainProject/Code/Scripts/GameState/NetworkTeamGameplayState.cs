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
    /*public class NetworkTeamGameplayState : NetworkGameplayState
    {


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
    }*/
}
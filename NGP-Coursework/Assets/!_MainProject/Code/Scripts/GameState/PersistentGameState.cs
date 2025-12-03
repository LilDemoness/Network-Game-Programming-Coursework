using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     A class containing some data that needs to be passed between states to represent the session's win state.<br/>
    ///     We will be changing this once we start work on victory conditions, but for now are using the same as the Boss Room sample to get it working.
    /// </summary>
    public class PersistentGameState
    {
        public bool UseTeams { get; set; }
        public PostGameData[] GameData { get; set; }


        public void Reset()
        {
            GameData = new PostGameData[0]; // ???
        }
    }



    public struct PostGameData : INetworkSerializable, IEquatable<PostGameData>
    {
        /// <summary>
        ///     NoTeams: PlayerIndex;
        ///     Teams: TeamIndex (Used to retrieve players).
        /// </summary>
        public int Index;
        public int Score;


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Index);
            serializer.SerializeValue(ref Score);
        }
        public bool Equals(PostGameData other) => (this.Index, this.Score) == (other.Index, other.Score);
    }
}
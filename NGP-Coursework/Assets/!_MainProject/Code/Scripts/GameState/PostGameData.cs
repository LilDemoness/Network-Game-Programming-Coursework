using Gameplay.GameplayObjects.Character.Customisation.Data;
using System;
using Unity.Netcode;
using Utils;

namespace Gameplay.GameState
{
    public struct FFAPostGameData : INetworkSerializable, IEquatable<FFAPostGameData>
    {
        public static bool CountDeathsAsNegativePoints { get; set; } = false;
        public int Score => CountDeathsAsNegativePoints ? Kills - Deaths : Kills;


        public int PlayerIndex;         // Used to determine which player won, and to show each client which position on the leaderboard represents them.
        public int Kills;               // Used for determining the winners, and for displaying this character's score on the leaderboard.
        public int Deaths;

        public FixedPlayerName Name;    // Used for displaying this character's name on the Leaderboard.

        public int FrameIndex;          // Used for displaying this character's model on the podium.
        public int[] SlottableIndicies; // Used for displaying this character's model on the podium.


        public FFAPostGameData(int playerIndex, int kills, int deaths, FixedPlayerName name, int frameIndex, int[] slottableIndicies)
        {
            this.PlayerIndex = playerIndex;
            this.Kills = kills;
            this.Deaths = deaths;

            this.Name = name;

            this.FrameIndex = frameIndex;
            this.SlottableIndicies = slottableIndicies;
        }


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerIndex);
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref Deaths);

            serializer.SerializeValue(ref Name);

            serializer.SerializeValue(ref FrameIndex);
            serializer.SerializeValue(ref SlottableIndicies);
        }
        public bool Equals(FFAPostGameData othr)
            => (this.PlayerIndex, this.Kills, this.Deaths, this.Name, this.FrameIndex, this.SlottableIndicies)
            == (othr.PlayerIndex, othr.Kills, othr.Deaths, othr.Name, othr.FrameIndex, othr.SlottableIndicies);
    }
    public struct TDMPostGameData : INetworkSerializable, IEquatable<TDMPostGameData>
    {
        public int TeamIndex;
        public int Score;


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TeamIndex);
            serializer.SerializeValue(ref Score);
        }
        public bool Equals(TDMPostGameData other) => (this.TeamIndex, this.Score) == (other.TeamIndex, other.Score);
    }
}
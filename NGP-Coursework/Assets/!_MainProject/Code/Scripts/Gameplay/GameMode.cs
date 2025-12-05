namespace Gameplay
{
    /// <summary>
    ///     Enum of the different types of GameMode within the game.
    /// </summary>
    [System.Serializable]
    public enum GameMode
    {
        Invalid = -1,

        // Game Modes (Ensure their indicies are contiguous).
        FreeForAll = 0,
        TeamDeathmatch,
        KingOfTheHill
    }

    /// <summary>
    ///     Extension functions for the <see cref="GameMode"/> enum.
    /// </summary>
    public static class GameModeExtensions
    {
        public static GameMode[] GetAllGameModes(this GameMode gameType) => new GameMode[]
        {
            GameMode.FreeForAll,
            GameMode.TeamDeathmatch,
            GameMode.KingOfTheHill,
        };
    }
}
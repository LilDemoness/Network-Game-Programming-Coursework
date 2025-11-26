using UnityEngine;

namespace Gameplay.GameState
{
    public enum WinState
    {
        Invalid,
        Win,
        Loss,
    }

    /// <summary>
    ///     A class containing some data that needs to be passed between states to represent the session's win state.<br/>
    ///     We will be changing this once we start work on victory conditions, but for now are using the same as the Boss Room sample to get it working.
    /// </summary>
    public class PersistentGameState
    {
        public WinState WinState { get; private set; } = WinState.Invalid;

        public void SetWinState(WinState winState) => WinState = winState;
        public void Reset() => WinState = WinState.Invalid;
    }
}
namespace Gameplay.Actions
{
    [System.Serializable]
    public enum BlockingModeType
    {
        EntireDuration,             // The Action blocks for it's entire duration.
        OnlyDuringExecutionTime,    // The Action blocks only during its initial execution.
        Never,                      // Once started, the action never blocks (Will still be in the ActionQueue until started).
    }
}
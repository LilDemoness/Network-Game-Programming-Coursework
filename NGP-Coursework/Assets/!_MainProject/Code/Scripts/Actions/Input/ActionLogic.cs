namespace Gameplay.Actions
{
    /// <summary>
    ///     Enum containing all the Types of Actions.
    ///     There is a many-to-one mapping of Actions to ActionLogic.
    /// </summary>
    public enum ActionLogic
    {
        Chase,
        Cancelling,

        Melee,
        LaunchProjectile,
        RangedTargeted,
        TangedFXTargeted,
        AoE,
        ChargedLaunchProjectile,

        StealthMode,
    }
}
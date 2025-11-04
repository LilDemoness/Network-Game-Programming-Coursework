namespace Gameplay.StatusEffects
{
    /// <summary>
    ///     How a StatusEffect should handle being applied to the same character more than once.
    /// </summary>
    public enum StackingType
    {
        /// <summary> Trigger the StatusEffect agsin (Calls 'OnStart()').</summary>
        Retrigger,
        /// <summary> Reset the remaining duration of the StatusEffect.</summary>
        ResetDuration,
        /// <summary> Add the lifetime of the StatusEffect to the active instance.</summary>
        AddDuration,
        /// <summary> Play the status effect in parallel with the active instance.</summary>
        InParallel,
        /// <summary> Cancel the active instance and don't apply.</summary>
        Toggle
    }
}
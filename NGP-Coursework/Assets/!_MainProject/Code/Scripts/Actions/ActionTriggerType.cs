namespace Gameplay.Actions
{
    public enum ActionTriggerType
    {
        Single,         // Action triggered Once then Concludes.
        Burst,          // Action performed a fixed number of times then Concludes.
        Repeated,       // Action triggered until cancelled/released.
        RepeatedBurst,  // Action triggered until cancelled/released, performing multiple times when updated.
    }
}
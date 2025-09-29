namespace Gameplay.Actions.Targeting
{
    [System.Flags]
    public enum TargetableTypes
    {
        None = 0,

        Self = 1 << 0,
        Friendlies = 1 << 1,
        Enemies = 1 << 2,
        Props = 1 << 3,

        All = ~0,
        AllOthers = All & ~Self,
    }
}
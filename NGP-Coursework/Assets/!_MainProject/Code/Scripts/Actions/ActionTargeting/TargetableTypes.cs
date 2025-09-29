using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

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

    public static class TargetableTypesExtensions
    {
        public static bool IsValidTarget(this TargetableTypes targetableTypes, ServerCharacter owner, NetworkObject targetObject)
        {
            if (targetObject.TryGetComponent<ServerCharacter>(out ServerCharacter targetCharacter))
            {
                // The target is a character (Not a Prop).
                // Check if we are targeting our owner.
                if (targetCharacter == owner)
                    return targetableTypes.HasFlag(TargetableTypes.Self);

                // Check if we are targeting a friendly or enemy character.
                bool isFriendly = owner.TeamID == targetCharacter.TeamID;
                return (isFriendly && targetableTypes.HasFlag(TargetableTypes.Friendlies))
                    || (!isFriendly && targetableTypes.HasFlag(TargetableTypes.Enemies));
            }
            else
            {
                // Not a Character (Therefore likely a prop).
                return targetableTypes.HasFlag(TargetableTypes.Props);
            }
        }
    }
}
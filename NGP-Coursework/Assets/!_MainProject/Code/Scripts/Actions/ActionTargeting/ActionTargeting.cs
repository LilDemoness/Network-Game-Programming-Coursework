using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    // Targeting Types:
    // - Self
    // - AoE
    // - Melee
    // - Ranged Projectile
    // - Ranged Raycast
    [System.Serializable]
    public abstract class ActionTargeting
    {
        // Can apply from an origin position rather than a character so that we can have things like AoEs spawn from a projectile's hit position.
        public abstract void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ulong[]> onCompleteCallback);
    }
}
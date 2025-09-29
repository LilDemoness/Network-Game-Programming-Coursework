using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    [System.Serializable]
    public class SelfTargeting : ActionTargeting
    {
        public override ulong[] GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction) => new ulong[1] { owner.NetworkObjectId };
    }
}
using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    [System.Serializable]
    public class SelfTargeting : ActionTargeting
    {
        public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInfo[]> onCompleteCallback)
            => onCompleteCallback?.Invoke(owner, new ActionHitInfo[1] { new ActionHitInfo(owner.transform) });


        public override bool CanTriggerOnClient() => false;
    }
}
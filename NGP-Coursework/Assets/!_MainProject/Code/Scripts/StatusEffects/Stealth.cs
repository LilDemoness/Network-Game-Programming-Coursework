using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.StatusEffects
{
    [CreateAssetMenu(menuName = "Status Effect/New Stealth Status Effect")]
    public class Stealth : StatusEffectDefinition
    {
        public override void OnStart(ServerCharacter serverCharacter) => serverCharacter.IsInStealth.Value = true;
        public override void OnEnd(ServerCharacter serverCharacter) => EndStealth(serverCharacter);
        public override void OnCancel(ServerCharacter serverCharacter) => EndStealth(serverCharacter);

        private void EndStealth(ServerCharacter serverCharacter)
        {
            serverCharacter.IsInStealth.Value = false;
        }


        public override void OnStartClient(ClientCharacter clientCharacter) { }
    }
}
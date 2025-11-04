using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.StatusEffects.Definitions
{
    [CreateAssetMenu(menuName = "Status Effect/New Stealth Status Effect")]
    public class Stealth : StatusEffectDefinition
    {
        public override void OnStart(ServerCharacter serverCharacter) => serverCharacter.IsInStealth.Value = true;
        public override void OnEnd(ServerCharacter serverCharacter) => EndStealth(serverCharacter);
        public override void OnCancel(ServerCharacter serverCharacter) => EndStealth(serverCharacter);


        /// <summary>
        ///     End the Stealth effect on the passed character.
        /// </summary>
        private void EndStealth(ServerCharacter serverCharacter)
        {
            serverCharacter.IsInStealth.Value = false;
        }


        public override void OnStartClient(ClientCharacter clientCharacter) { }
    }
}
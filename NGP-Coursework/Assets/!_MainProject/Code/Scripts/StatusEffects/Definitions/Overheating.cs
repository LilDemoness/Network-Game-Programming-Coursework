using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.StatusEffects.Definitions
{
    [CreateAssetMenu(menuName = "Status Effect/New Overheating Status Effect")]
    public class Overheating : StatusEffectDefinition
    {
        [SerializeField] private float _damagePerTick = 1.0f;
        [SerializeField] private float _heatDecreasePerTick = 1.0f;

        public override void OnTick(ServerCharacter serverCharacter)
        {
            serverCharacter.NetworkHealthComponent.ReceiveHealthChange_Server(serverCharacter, -_damagePerTick);
            //serverCharacter.ReceiveHeatChange(serverCharacter, -_heatChangeOnTick);
        }
    }
}
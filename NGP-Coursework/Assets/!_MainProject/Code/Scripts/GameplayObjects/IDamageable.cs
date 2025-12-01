using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        void ReceiveHealthChange_Server(ServerCharacter influencer, float hitPointsChange);

        float GetMissingHealth();

        bool CanHaveHealthChanged();
        bool CanTakeDamage();
        bool CanReceiveHealing();
    }
}
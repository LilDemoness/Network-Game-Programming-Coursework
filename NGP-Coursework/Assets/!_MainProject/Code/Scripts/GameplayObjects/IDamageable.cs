using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        void ReceiveHealthChange(ServerCharacter influencer, float hitPointsChange);

        float GetMissingHealth();

        bool IsDamageable();
    }
}
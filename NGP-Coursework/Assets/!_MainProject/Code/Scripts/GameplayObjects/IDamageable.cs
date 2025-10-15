using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        void ReceiveHealthChange(ServerCharacter influencer, int hitPointsChange);

        int GetMissingHealth();

        bool IsDamageable();
    }
}
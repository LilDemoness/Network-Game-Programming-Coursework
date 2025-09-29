using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        void ReceiveHitPoints(ServerCharacter influencer, int hitPointsChange);

        int GetTotalDamage();

        ulong NetworkObjectID { get; }

        Transform transform { get; }

        bool IsDamageable();
    }
}
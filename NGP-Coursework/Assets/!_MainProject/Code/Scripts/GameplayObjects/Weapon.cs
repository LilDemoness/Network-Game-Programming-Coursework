using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData m_weaponData;
        public WeaponData WeaponData => m_weaponData;

        [SerializeField] private Transform _firingOrigin;


        public Transform GetAttackOriginTransform() => _firingOrigin;
        public Vector3 GetAttackLocalOffset() => Vector3.zero;
        public Vector3 GetAttackLocalDirection() => Vector3.forward;
    }
}
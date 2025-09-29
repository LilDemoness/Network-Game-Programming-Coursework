using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData m_weaponData;
        public WeaponData WeaponData => m_weaponData;

        [SerializeField] private Transform _firingOrigin;


        public Vector3 GetAttackOrigin() => _firingOrigin.position;
        public Vector3 GetAttackDirection() => _firingOrigin.forward;
    }
}
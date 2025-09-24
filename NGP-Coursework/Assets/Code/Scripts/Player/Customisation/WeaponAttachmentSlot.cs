using UnityEngine;

public class WeaponAttachmentSlot : MonoBehaviour
{
    [SerializeField] private WeaponsGFXSection[] _weaponGFXs;


    public void Toggle(WeaponData activeData)
    {
        for (int i = 0; i < _weaponGFXs.Length; ++i)
            _weaponGFXs[i].Toggle(activeData);
    }
    public void Finalise(WeaponData activeData)
    {
        for (int i = 0; i < _weaponGFXs.Length; ++i)
            _weaponGFXs[i].Finalise(activeData);
    }
}

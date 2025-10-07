using UnityEngine;
using Gameplay.Actions.Effects;

public class SpawnObjectEffectTester : MonoBehaviour
{
    [SerializeField] private SpawnObjectEffect _spawnObjectEffect;


    private void OnDrawGizmosSelected()
    {
        ActionHitInformation hitInfo = new ActionHitInformation();
        _spawnObjectEffect.ApplyEffect(null, hitInfo);
    }
}

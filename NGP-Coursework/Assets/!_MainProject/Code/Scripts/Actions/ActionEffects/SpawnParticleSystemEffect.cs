using UnityEngine;
using Gameplay.Actions.Targeting;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Visuals
{
    /// <summary>
    ///     ActionEffect that plays a Particle System for either a fixed duration or the lifetime of the particle system.
    /// </summary>
    [System.Serializable]
    public class SpawnParticleSystemEffect : ActionClientVisual
    {
        [SerializeField] private ParticleSystem _particleSystemPrefab;
        [SerializeField] [Min(0.0f)] private float _lifetime = 0.0f;

        protected override void ApplyToTarget(ClientCharacter client, Action actionReference, ref ActionHitInfo actionHitInfo)
        {
            ParticleSystem particleSystem = GameObject.Instantiate<ParticleSystem>(_particleSystemPrefab, actionHitInfo.HitPosition, Quaternion.LookRotation(actionHitInfo.HitNormal));

            

            if (_lifetime == 0.0f)
                particleSystem.Play();
            else
            {

            }
        }
    }
}
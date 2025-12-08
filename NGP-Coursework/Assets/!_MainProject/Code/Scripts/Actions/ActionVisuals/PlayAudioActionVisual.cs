using Gameplay.Audio;
using Gameplay.GameplayObjects.Character;
using UnityEngine;
using VisualEffects;

namespace Gameplay.Actions.Visuals
{
    /// <summary>
    ///     Plays a AudioClip at the origin position when triggered.
    /// </summary>
    [System.Serializable]
    public class PlayAudioActionVisual : ActionVisual
    {
        [SerializeField] private AudioClip _clip;


        protected override void Trigger(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction) => PlayAudioClip(origin);



        private void PlayAudioClip(in Vector3 origin)
        {
            if (_clip == null)
                return;

            AudioManager.Instance.PlayOneShot(_clip, origin);
        }
    }
}

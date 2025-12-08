using UnityEngine;

namespace Gameplay.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource _audioSource;


        public void PlayOneShot(AudioClip clip, Vector3 position)
        {
            _audioSource.transform.position = position;
            _audioSource.PlayOneShot(clip);
        }
    }
}
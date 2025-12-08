using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisualEffects
{
    public class SpecialFXGraphic : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> _particleSystems;
        [SerializeField] private float _autoShutdownTime = -1.0f;
        [SerializeField] private bool _shutdownAutomaticallyOnAllParticlesComplete = true;
        [SerializeField] private float _postShutdownDelay = -1.0f;

        public event System.Action<SpecialFXGraphic> OnShutdownComplete;

        private bool _isInShutdown = false;
        public int SpecialFXListIndex = -1;


        public void Play()
        {
            _isInShutdown = false;

            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                particleSystem.Play();
            }

            if (_autoShutdownTime >= 0.0f)
                StartCoroutine(ShutdownAfterDuration());
            if (_shutdownAutomaticallyOnAllParticlesComplete)
                StartCoroutine(ShutdownAfterParticlesComplete());
        }
        private IEnumerator ShutdownAfterDuration()
        {
            yield return new WaitForSeconds(_autoShutdownTime);
            Shutdown();
        }
        private IEnumerator ShutdownAfterParticlesComplete()
        {
            bool foundAliveParticles;
            do
            {
                yield return new WaitForEndOfFrame();
                foundAliveParticles = false;
                foreach (ParticleSystem particleSystem in _particleSystems)
                {
                    if (particleSystem.IsAlive())
                    {
                        foundAliveParticles = true;
                    }
                }
            } while (foundAliveParticles);

            Shutdown();
        }


        public void Shutdown()
        {
            if (_isInShutdown)
                return;

            _isInShutdown = true;

            foreach(ParticleSystem particleSystem in _particleSystems)
            {
                particleSystem.Stop();
            }

            if (_postShutdownDelay >= 0.0f)
            {
                StartCoroutine(NotifyShutdownAfterFixedTime());
            }
            else
            {
                // Non-Fixed Shutdown Time. Shutdown once our particles have ended.
                StartCoroutine(WaitForParticlesToEnd());
            }
        }

        private IEnumerator NotifyShutdownAfterFixedTime()
        {
            yield return new WaitForSeconds(_postShutdownDelay);
            OnShutdownComplete?.Invoke(this);
        }
        private IEnumerator WaitForParticlesToEnd()
        {
            bool foundAliveParticles;
            do
            {
                yield return new WaitForEndOfFrame();
                foundAliveParticles = false;
                foreach (ParticleSystem particleSystem in _particleSystems)
                {
                    if (particleSystem.IsAlive())
                    {
                        foundAliveParticles = true;
                    }
                }
            } while (foundAliveParticles);

            OnShutdownComplete?.Invoke(this);
        }


#if UNITY_EDITOR

        [ContextMenu("Setup/Find All Child Particle Systems")]
        private void Setup_FindAllChildParticleSystems()
        {
            _particleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>());
        }

#endif
        }
    }
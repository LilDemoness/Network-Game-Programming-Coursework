using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.StatusEffects.Definitions;

namespace Gameplay.StatusEffects
{
    public class StatusEffect
    {
        private StatusEffectDefinition _definition;
        public StatusEffectDefinition Definition => _definition;


# region Effect Timing

        /// <summary> The 'NetworkManager.ServerTime' time when this StatusEffect was started. </summary>
        public float TimeStarted { get; set; }
        /// <summary> The time (In Seconds) that this StatusEffect has been running. </summary>
        public float TimeRunning => NetworkManager.Singleton.ServerTime.TimeAsFloat - TimeStarted;
        /// <summary> The time when this StatusEffect should elapse.</summary>
        public float EffectElapsedTime { get; set; }

        private float _nextTickTime;
        private bool _hasPerformedFinalTick;

#endregion


        public StatusEffect(StatusEffectDefinition definition)
        {
            this._definition = definition;
        }


        /// <summary>
        ///     Reset the container for returning to an object pool.
        /// </summary>
        public virtual void ReturnToPool()
        {
            this.TimeStarted = 0.0f;
            this._nextTickTime = 0.0f;
            this._hasPerformedFinalTick = false;
        }

        #region Server-side


        /// <inheritdoc cref="StatusEffectDefinition.OnStart(ServerCharacter)"/>
        public void OnStart(ServerCharacter serverCharacter)
        {
            // Determine timing requirements for this effect (Next Update and Elapsed).
            EffectElapsedTime = _definition.Lifetime > 0.0f ? TimeStarted + _definition.Lifetime : -1.0f;
            _nextTickTime = 0.0f;

            // Perform any required initialisation logic.
            _definition.OnStart(serverCharacter);
        }
        /// <summary>
        ///     Called every frame and determines if the status effect should update.
        /// </summary>
        public void OnUpdate(ServerCharacter serverCharacter)
        {
            if (_hasPerformedFinalTick)
                return; // We aren't wanting to trigger this effect again.
            
            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextTickTime)
                return; // We aren't yet at the time to trigger this effect.

            // We are wanting to trigger this effect.
            _definition.OnTick(serverCharacter);


            if (_definition.RetriggerDelay > 0.0f)
            {
                // We are wanting to tick this effect again. Calculate the next tick time.
                _nextTickTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
            }
            else
                _hasPerformedFinalTick = true;  // We aren't wanting to tick the effect again.
        }
        /// <inheritdoc cref="StatusEffectDefinition.OnEnd(ServerCharacter)"/>
        public void OnEnd(ServerCharacter serverCharacter) => _definition.OnEnd(serverCharacter);
        /// <inheritdoc cref="StatusEffectDefinition.OnCancel(ServerCharacter)"/>
        public void OnCancel(ServerCharacter serverCharacter) => _definition.OnCancel(serverCharacter);

        #endregion


        #region Client-side

        /// <inheritdoc cref="StatusEffectDefinition.OnStartClient(ClientCharacter)"/>
        /// <param name="serverTimeStarted"> The time on the server when this StatusEffect was applied.</param>
        public void OnStartClient(ClientCharacter clientCharacter, float serverTimeStarted)
        {
            // Calculate timing requirements.
            TimeStarted = serverTimeStarted;
            EffectElapsedTime = _definition.Lifetime > 0.0f ? TimeStarted + _definition.Lifetime : -1.0f;
            _nextTickTime = 0.0f;

            // Perform any required initialisation logic.
            _definition.OnStartClient(clientCharacter);
        }
        /// <summary>
        ///     Called on the client every frame and determines if the status effect should update.
        /// </summary>
        public void OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (_hasPerformedFinalTick)
                return; // We aren't wanting to trigger this effect again.

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextTickTime)
                return; // We aren't yet at the time to trigger this effect.

            // We are wanting to trigger this effect.
            _definition.OnTickClient(clientCharacter);

            if (_definition.RetriggerDelay > 0.0f)
            {
                // We are wanting to tick this effect again. Calculate the next tick time.
                _nextTickTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
            }
            else
                _hasPerformedFinalTick = true;  // We aren't wanting to trigger this effect again.
        }
        /// <inheritdoc cref="StatusEffectDefinition.OnEndClient(ClientCharacter)"/>
        public void OnEndClient(ClientCharacter clientCharacter) => _definition.OnEndClient(clientCharacter);
        /// <inheritdoc cref="StatusEffectDefinition.OnCancelClient(ClientCharacter)"/>
        public void OnCancelClient(ClientCharacter clientCharacter) => _definition.OnCancelClient(clientCharacter);

        #endregion
    }
}
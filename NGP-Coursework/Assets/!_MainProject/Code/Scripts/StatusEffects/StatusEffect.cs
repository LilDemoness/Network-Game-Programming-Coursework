using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.StatusEffects
{
    public class StatusEffect
    {
        private StatusEffectDefinition _definition;
        public StatusEffectDefinition Definition => _definition;


        // Effect Timing.
        public float TimeStarted { get; set; }
        public float TimeRunning => NetworkManager.Singleton.ServerTime.TimeAsFloat - TimeStarted;
        public float EffectElapsedTime { get; set; }

        private float _nextTickTime;
        private bool _hasPerformedFinalTick;



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

        public void OnStart(ServerCharacter serverCharacter)
        {
            EffectElapsedTime = _definition.Lifetime > 0.0f ? TimeStarted + _definition.Lifetime : -1.0f;
            _nextTickTime = 0.0f;
            _definition.OnStart(serverCharacter);
        }
        public void OnUpdate(ServerCharacter serverCharacter)
        {
            // Here or in the Action Definition?
            if (_definition.HeatGeneratedPerSecond != 0.0f)
            {
                serverCharacter.ReceiveHeatChange(_definition.HeatGeneratedPerSecond * UnityEngine.Time.deltaTime);
            }
            
            if (_hasPerformedFinalTick)
                return;

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextTickTime)
                return;

            _definition.OnTick(serverCharacter);


            if (_definition.RetriggerDelay > 0.0f)
                _nextTickTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
            else
                _hasPerformedFinalTick = true;
        }
        public void OnEnd(ServerCharacter serverCharacter) => _definition.OnEnd(serverCharacter);
        public void OnCancel(ServerCharacter serverCharacter) => _definition.OnCancel(serverCharacter);

        #endregion


        #region Client-side

        public void OnStartClient(ClientCharacter clientCharacter, float serverTimeStarted)
        {
            TimeStarted = serverTimeStarted;
            EffectElapsedTime = _definition.Lifetime > 0.0f ? TimeStarted + _definition.Lifetime : -1.0f;
            _nextTickTime = 0.0f;

            _definition.OnStartClient(clientCharacter);
        }
        public void OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (_hasPerformedFinalTick)
                return;

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextTickTime)
                return;

            _definition.OnTickClient(clientCharacter);

            if (_definition.RetriggerDelay > 0.0f)
                _nextTickTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
        }
        public void OnEndClient(ClientCharacter clientCharacter) => _definition.OnEndClient(clientCharacter);
        public void OnCancelClient(ClientCharacter clientCharacter) => _definition.OnCancelClient(clientCharacter);

        #endregion
    }
}
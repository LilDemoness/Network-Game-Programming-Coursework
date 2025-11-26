using UnityEngine;

namespace UnityServices
{
    public class RateLimitCooldown
    {
        private readonly float _cooldownTimeLength;
        private float _cooldownFinishedTime;

        public float CooldownTimeLength => _cooldownTimeLength;

        public RateLimitCooldown(float cooldownTimeLength)
        {
            this._cooldownTimeLength = cooldownTimeLength;
            this._cooldownFinishedTime = -1.0f;
        }

        public bool CanCall => Time.unscaledTime > _cooldownFinishedTime;
        public void PutOnCooldown() => _cooldownFinishedTime = Time.unscaledTime + _cooldownTimeLength;
    }
}
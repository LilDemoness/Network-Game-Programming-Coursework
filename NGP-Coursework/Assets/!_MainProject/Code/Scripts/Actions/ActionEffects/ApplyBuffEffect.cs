using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ApplyBuffEffect : ActionEffect
    {
        [SerializeField] private Action.BuffableValue _buffableValueType;
        [SerializeField] private float _newValue;
        [SerializeField] private float _buffLifetime;

        [Space(5)]
        [SerializeField] private bool _scaleValueWithCharge = false;
        [SerializeField] private bool _scaleDurationWithCharge = true;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            float valueChange = _newValue - Action.GetUnbuffedValue(_buffableValueType);
            float buffValue = _scaleDurationWithCharge ? (Action.GetUnbuffedValue(_buffableValueType) + valueChange * chargePercentage) : _newValue;
            float buffLifetime = _scaleDurationWithCharge ? _buffLifetime * chargePercentage : _buffLifetime;

            Debug.Log($"Applying Buff {_buffableValueType} (Value: {buffValue}) to '{hitInfo.Target.name}' for {buffLifetime} seconds");
        }

        // We don't need to perform any cleanup as the buff is automatically removed by the character when its duration elapses.
        public override void Cleanup() { }
    }
}
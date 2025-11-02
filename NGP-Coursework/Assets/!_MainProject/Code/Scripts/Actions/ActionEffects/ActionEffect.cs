using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Base class for all game-state affecting effects created by an action.
    /// </summary>
    [System.Serializable]
    public abstract class ActionEffect
    {
        public void ApplyEffect(ServerCharacter owner, in ActionHitInformation[] hitInfoArray, float chargePercentage)
        {
            for(int i = 0; i < hitInfoArray.Length; ++i)
            {
                ApplyEffect(owner, hitInfoArray[i], chargePercentage);
            }
        }
        public abstract void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage);


        /// <summary>
        ///     Cleanup the ActionEffect to be used the next time the Action is created.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner) { }
    }
}
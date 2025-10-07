using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public abstract class ActionEffect
    {
        public void ApplyEffect(ServerCharacter owner, in ActionHitInformation[] hitInfoArray)
        {
            for(int i = 0; i < hitInfoArray.Length; ++i)
            {
                ApplyEffect(owner, hitInfoArray[i]);
            }
        }
        public abstract void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo);


        /// <summary>
        ///     Cleanup the ActionEffect to be used the next time the Action is created.
        /// </summary>
        public abstract void Cleanup();
    }
}
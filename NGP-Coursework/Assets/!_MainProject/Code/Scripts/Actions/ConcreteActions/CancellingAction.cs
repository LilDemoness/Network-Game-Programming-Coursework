using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An Action that is used only to cancel other actions.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Cancelling Action")]
    public class CancellingAction : Action
    {
        public override void Reset()
        {
            base.Reset();

            Config ??= new ActionConfig();
            Config.Logic = ActionLogic.Cancelling;
        }

        public override bool OnStart(ServerCharacter serverCharacter) => false;
        public override bool OnUpdate(ServerCharacter serverCharacter) => false;
    }
}
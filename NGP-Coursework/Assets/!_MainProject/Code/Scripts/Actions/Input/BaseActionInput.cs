using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    public abstract class BaseActionInput : MonoBehaviour
    {
        protected ServerCharacter PlayerOwner;
        protected Vector3 Origin;
        protected ActionID ActionPrototypeID;
        protected System.Action<ActionRequestData> SendInput;
        System.Action _onFinished;


        public void Initialise(ServerCharacter playerOwner, Vector3 origin, ActionID actionPrototypeID, System.Action<ActionRequestData> onSendInput, System.Action onFinished)
        {
            this.PlayerOwner = playerOwner;
            this.Origin = origin;
            this.ActionPrototypeID = actionPrototypeID;
            this.SendInput = onSendInput;
            this._onFinished = onFinished;
        }

        public void OnDestroy() => _onFinished?.Invoke();
        
        public virtual void OnReleaseKey() { }
    }
}
using UnityEngine;
using Gameplay.GameplayObjects.Character;
using VisualEffects;

namespace Gameplay.Actions.Visuals
{
    /// <summary>
    ///     Spawns and deletes a SpecialFXGraphic.
    /// </summary>
    [System.Serializable]
    public class SpecialFXActionVisual : ActionVisual
    {
        [SerializeField] private SpecialFXGraphic _specialFXGraphic;


        protected override void Trigger(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction) => ActivateSpecialFXGraphic(origin, direction);
        


        private void ActivateSpecialFXGraphic(in Vector3 origin, in Vector3 direction)
        {
            if (_specialFXGraphic == null)
                return;

            SpecialFXGraphic specialFXGraphic = SpecialFXPoolManager.GetFromPrefab(_specialFXGraphic);
            specialFXGraphic.transform.position = origin;
            specialFXGraphic.transform.rotation = Quaternion.LookRotation(direction);

            specialFXGraphic.OnShutdownComplete += SpecialFXGraphic_OnShutdownComplete;

            specialFXGraphic.Play();
        }
        private void SpecialFXGraphic_OnShutdownComplete(SpecialFXGraphic graphicInstance)
        {
            graphicInstance.OnShutdownComplete -= SpecialFXGraphic_OnShutdownComplete;
            SpecialFXPoolManager.ReturnFromPrefab(_specialFXGraphic, graphicInstance);
        }
    }
}
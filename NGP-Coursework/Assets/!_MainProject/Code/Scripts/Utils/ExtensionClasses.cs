using UnityEngine;


public static class ComponentExtensions
{
    public static bool TryGetComponentThroughParents<T>(this Component activeComponent, out T component)
    {
        if (activeComponent.TryGetComponent(out component))
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.TryGetComponentThroughParents<T>(out component);
    }
}
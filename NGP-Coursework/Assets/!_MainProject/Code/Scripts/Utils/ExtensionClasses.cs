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


    public static bool HasParent(this Component activeComponent, Transform parentToCheck)
    {
        if (activeComponent.transform == parentToCheck)
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.HasParent(parentToCheck);
    }
}
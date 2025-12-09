using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class ComponentExtensions
{
    /// <summary>
    ///     Attempt to retrieve the first component of the desired type,
    ///     searching this component's parent's until there are none left or it finds an instance of the desired component type.
    /// </summary>
    public static bool TryGetComponentThroughParents<T>(this Component activeComponent, out T component)
    {
        if (activeComponent.TryGetComponent(out component))
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.TryGetComponentThroughParents<T>(out component);
    }

    /// <summary>
    ///     Returns true if this component is a child of (Or on the same object of) the object with the passed transform.
    /// </summary>
    public static bool HasParent(this Component activeComponent, Transform parentToCheck)
    {
        if (activeComponent.transform == parentToCheck)
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.HasParent(parentToCheck);
    }
}


public static class SelectableExtensions
{
    /// <summary>
    ///     Setup the Navigation parameter of this Selectable with the given inputs.
    /// </summary>
    public static void SetNavigation(this Selectable selectable, Selectable onLeft = null, Selectable onRight = null, Selectable onUp = null, Selectable onDown = null)
    {
        // Create and setup the new navigation.
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft     = onLeft;
        navigation.selectOnRight    = onRight;
        navigation.selectOnUp       = onUp;
        navigation.selectOnDown     = onDown;

        // Set our selectable's navigation.
        selectable.navigation = navigation;
    }
}


public static class ScrollRectExtensions
{
    /// <summary>
    ///     Ensure that the child element of this ScrollRect is visible within the contents.
    /// </summary>
    // Source: 'https://stackoverflow.com/a/62005575'.
    public static void BringChildIntoView(this ScrollRect instance, RectTransform child)
    {
        // Ensure that our RectTransforms have been updated.
        instance.content.ForceUpdateRectTransforms();
        instance.viewport.ForceUpdateRectTransforms();

        // Take scaling into account.
        Vector2 viewportLocalPosition = instance.viewport.localPosition;
        Vector2 childLocalPosition = child.localPosition;
        Vector2 newContentPosition = new Vector2(
            0 - ((viewportLocalPosition.x * instance.viewport.localScale.x) + (childLocalPosition.x * instance.content.localScale.x)),
            0 - ((viewportLocalPosition.y * instance.viewport.localScale.y) + (childLocalPosition.y * instance.content.localScale.y))
        );


        // Clamp Positions.
        instance.content.localPosition = newContentPosition;
        Rect contentRectInViewport = TransformRectFromTo(instance.content.transform, instance.viewport);
        float deltaXMin = contentRectInViewport.xMin - instance.viewport.rect.xMin;
        // Clamp to <= 0.
        if (deltaXMin > 0)
        {
            newContentPosition.x -= deltaXMin;
        }
        // Clamp to >= 0.
        float deltaXMax = contentRectInViewport.xMax - instance.viewport.rect.xMax;
        if (deltaXMax < 0)
        {
            newContentPosition.x -= deltaXMax;
        }
        // Clamp to <= 0.
        float deltaYMin = contentRectInViewport.yMin - instance.viewport.rect.yMin;
        if (deltaYMin > 0)
        {
            newContentPosition.y -= deltaYMin;
        }
        // Clamp to >= 0.
        float deltaYMax = contentRectInViewport.yMax - instance.viewport.rect.yMax;
        if (deltaYMax < 0)
        {
            newContentPosition.y -= deltaYMax;
        }


        // Apply final position.
        instance.content.localPosition = newContentPosition;
        instance.content.ForceUpdateRectTransforms();
    }

    /// <summary>
    ///     Converts a Rect from one RectTransfrom to another RectTransfrom.
    /// </summary>
    /// <remarks> Use the root Canvas Transform as "to" to get the reference pixel positions.</remarks>
    public static Rect TransformRectFromTo(Transform from, Transform to)
    {
        RectTransform fromRectTrans = from.GetComponent<RectTransform>();
        RectTransform toRectTrans = to.GetComponent<RectTransform>();

        if (fromRectTrans == null || toRectTrans == null)
            return default(Rect);   // One of our entered transforms wasn't a RectTransform and therefore was invalid.


        Vector3[] fromWorldCorners = new Vector3[4];
        Vector3[] toLocalCorners = new Vector3[4];
        Matrix4x4 toLocal = to.worldToLocalMatrix;
        fromRectTrans.GetWorldCorners(fromWorldCorners);
        for (int i = 0; i < 4; i++)
        {
            toLocalCorners[i] = toLocal.MultiplyPoint3x4(fromWorldCorners[i]);
        }

        return new Rect(toLocalCorners[0].x, toLocalCorners[0].y, toLocalCorners[2].x - toLocalCorners[1].x, toLocalCorners[1].y - toLocalCorners[0].y);
    }
}



public static class DictionaryExtensions
{
    /// <summary>
    ///     Returns true if this dictionary contains a null key.
    /// </summary>
    public static bool HasNullKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) => dictionary.Keys.Any(t => t.Equals(null));

    /// <summary>
    ///     Removes all null keys from this dictionary, if it has any.
    /// </summary>
    public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        if (!dictionary.HasNullKey())
            return;

        // Remove all null keys from the dictionary.
        dictionary = dictionary
            .Where(kvp => !kvp.Key.Equals(null))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}



public static class IListExtensions
{
    /// <summary>
	///     Randomly shuffles the element order of the specified list.
	/// </summary>
    // From: 'https://discussions.unity.com/t/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code/535113/2'.
    public static void Shuffle<T>(this IList<T> listToSort)
    {
        var count = listToSort.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var randomIndex = Random.Range(i, count);
            var tmp = listToSort[i];
            listToSort[i] = listToSort[randomIndex];
            listToSort[randomIndex] = tmp;
        }
    }
}
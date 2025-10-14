using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
///     An ObjectPool that keeps links to its retrieved objects and recycles them when it runs out of space.
/// </summary>
/// <typeparam name="T"></typeparam>
// Code Adapted from the base ObjectPool<T> ('https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Runtime/Export/ObjectPool/ObjectPools.cs').
public class RecyclingPool<T> : IDisposable, IObjectPool<T> where T : class
{
    internal readonly Stack<T> m_inactiveStack;
    internal readonly Queue<T> m_activeQueue;
    readonly Func<T> m_createFunc;
    readonly Action<T> m_actionOnGet;
    readonly Action<T> m_actionOnRelease;
    readonly Action<T> m_actionOnDestroy;
    readonly int m_maxSize; // Used to prevent catastrophic memory retention.
    internal bool m_collectionCheck;

    /// <summary>
    ///     The total number of active and inactive objects.
    /// </summary>
    public int CountAll { get; private set; }

    /// <summary>
    ///     Number of objects that have been created by the pool but are currently in use and have not yet been returned.
    /// </summary>
    public int CountActive { get { return m_activeQueue.Count; } }

    /// <summary>
    ///     Number of objects that are currently available in the pool.
    /// </summary>
    public int CountInactive { get { return m_inactiveStack.Count; } }

    /// <summary>
    ///     Creates a new ObjectPool.
    /// </summary>
    /// <param name="createFunc">Use to create a new instance when the pool is empty. In most cases this will just be <code>() => new T()</code></param>
    /// <param name="actionOnGet">Called when the instance is being taken from the pool.</param>
    /// <param name="actionOnRelease">Called when the instance is being returned to the pool. This could be used to clean up or disable the instance.</param>
    /// <param name="actionOnDestroy">Called when the element can not be returned to the pool due to it being equal to the maxSize.</param>
    /// <param name="collectionCheck">Collection checks are performed when an instance is returned back to the pool. An exception will be thrown if the instance is already in the pool. Collection checks are only performed in the Editor.</param>
    /// <param name="defaultCapacity">The default capacity the stack will be created with.</param>
    /// <param name="maxSize">The maximum size of the pool. When the pool reaches the max size then any further instances returned to the pool will be ignored and can be garbage collected. This can be used to prevent the pool growing to a very large size.</param>
    public RecyclingPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10)
    {
        if (createFunc == null)
            throw new ArgumentNullException(nameof(createFunc));

        if (maxSize <= 0)
            throw new ArgumentException("Max Size must be greater than 0", nameof(maxSize));

        m_activeQueue = new Queue<T>(defaultCapacity);
        m_inactiveStack = new Stack<T>(defaultCapacity);
        m_createFunc = createFunc;
        m_maxSize = maxSize;
        m_actionOnGet = actionOnGet;
        m_actionOnRelease = actionOnRelease;
        m_actionOnDestroy = actionOnDestroy;
        m_collectionCheck = collectionCheck;
    }

    /// <summary>
    ///     Get an object from the pool.
    /// </summary>
    /// <returns>A new object from the pool.</returns>
    public T Get()
    {
        T element;
        if (m_inactiveStack.Count == 0)
        {
            if (CountAll >= m_maxSize)
            {
                element = m_activeQueue.Dequeue();

                if (m_collectionCheck && m_inactiveStack.Count > 0)
                {
                    if (m_inactiveStack.Contains(element))
                        throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
                }

                m_actionOnRelease?.Invoke(element);
            }
            else
            {
                element = m_createFunc();
                CountAll++;
            }

            Debug.Log(CountAll + " : " + m_maxSize, element as UnityEngine.Object);
        }
        else
        {
            element = m_inactiveStack.Pop();
        }
        m_actionOnGet?.Invoke(element);
        m_activeQueue.Enqueue(element);
        return element;
    }

    /// <summary>
    ///     Get a new <see cref="UnityEngine.Pool.PooledObject{T}"/> which can be used to return the instance back to the pool when the PooledObject is disposed.
    /// </summary>
    /// <param name="v">Output new typed object.</param>
    /// <returns>New PooledObject</returns>
    public PooledObject<T> Get(out T v) => new PooledObject<T>(v = Get(), this);

    /// <summary>
    /// Release an object to the pool.
    /// </summary>
    /// <param name="element">Object to release.</param>
    public void Release(T element)
    {
        Debug.Log("Release: " + (element as MonoBehaviour).name, element as MonoBehaviour);
        if (m_collectionCheck && m_inactiveStack.Count > 0)
        {
            if (m_inactiveStack.Contains(element))
                throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
        }

        m_actionOnRelease?.Invoke(element);

        if (CountInactive < m_maxSize)
        {
            m_inactiveStack.Push(element);
        }
        else
        {
            m_actionOnDestroy?.Invoke(element);
        }
    }

    /// <summary>
    /// Releases all pooled objects so they can be garbage collected.
    /// </summary>
    public void Clear()
    {
        if (m_actionOnDestroy != null)
        {
            foreach (var item in m_inactiveStack)
            {
                m_actionOnDestroy(item);
            }

            foreach (var item in m_activeQueue)
            {
                m_actionOnDestroy(item);
            }
        }

        m_inactiveStack.Clear();
        m_activeQueue.Clear();
        CountAll = 0;
    }

    public void Dispose()
    {
        // Ensure we do a clear so the destroy action can be called.
        Clear();
    }
}
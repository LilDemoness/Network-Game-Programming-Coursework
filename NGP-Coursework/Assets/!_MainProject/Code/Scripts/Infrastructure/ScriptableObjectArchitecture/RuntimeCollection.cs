using System;
using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    ///     ScriptableObject class that contains a list of a given type.<br/>
    ///     The instance of this SO can be referenced between components, without a hard reference between systems.
    /// </summary>
    /// <typeparam name="T"> The type contained within this RuntimeCollection.</typeparam>
    public class RuntimeCollection<T> : ScriptableObject
    {
        public List<T> Items = new List<T>();

        public event Action<T> ItemAdded;
        public event Action<T> ItemRemoved;


        public void Add(T item)
        {
            if (Items.Contains(item))
                return;

            Items.Add(item);
            ItemAdded?.Invoke(item);
        }
        public void Remove(T item)
        {
            if (Items.Remove(item))
            {
                // Successfully removed.
                ItemRemoved?.Invoke(item);
            }
            /*if (!Items.Contains(item))
                return;

            Items.Remove(item);
            ItemRemoved?.Invoke(item);*/
        }
    }
}
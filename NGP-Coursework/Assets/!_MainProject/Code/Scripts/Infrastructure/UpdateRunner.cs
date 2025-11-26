using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    ///     Some objects might need to be on a slower update loop than the usual MonoBehaviour update and without precise timing
    ///     (E.g. To refresh data from Services)</br>
    ///     Some might also not want to be coupled to a Unity object at all but still need an update loop.<br/>
    ///     This class facilitates these.
    /// </summary>
    public class UpdateRunner : MonoBehaviour
    {
        private class SubscriberData
        {
            public float Period;
            public float NextCallTime;
            public float LastCallTime;
        }

        private readonly Queue<System.Action> _pendingHandlers = new Queue<System.Action>();
        private readonly HashSet<System.Action<float>> _subscribers = new HashSet<System.Action<float>>();
        private readonly Dictionary<System.Action<float>, SubscriberData> _subscriberData = new Dictionary<System.Action<float>, SubscriberData>();


        public void OnDestroy()
        {
            _pendingHandlers.Clear();
            _subscribers.Clear();
            _subscriberData.Clear();
        }


        /// <summary>
        ///     Subscribe in order to have <paramref name="onUpdate"/> called approximately every <paramref name="updatePeriod"/> seconds (Or every frame if <= 0).
        ///     Don't assume that <paramref name="onUpdate"/> will be called in any particular order compared to other subscribers.
        /// </summary>
        /// <param name="onUpdate"> The update function. The float parameter is time since the last call.</param>
        /// <param name="updatePeriod"> The time between updates (In Seconds). Every frame if <= 0.</param>
        public void Subscribe(System.Action<float> onUpdate, float updatePeriod)
        {
            if (onUpdate == null)
                return;

            if (onUpdate.Target == null)    // Detect a local function that cannot be Unsubscribed from as it could go out of scope.
            {
                Debug.LogError("Can't subscribe to a local function that can go out of scope and can't be unsubscribed from");
                return;
            }

            if (onUpdate.Method.ToString().Contains('<'))   // Detect anonymous functions by checking for a character that can't exist in a declared method name.
            {
                Debug.LogError("Can't subscribe with an anonymous function that cannot be Unsubscribed from");
                return;
            }


            if (_subscribers.Contains(onUpdate))
                return;

            _pendingHandlers.Enqueue(() =>
            {
                if (_subscribers.Add(onUpdate))
                {
                    _subscriberData.Add(onUpdate, new SubscriberData() { Period = updatePeriod, NextCallTime = 0, LastCallTime = Time.time });
                }
            });
        }


        /// <summary>
        ///     Unsubscribe from the update loop.<br/>
        ///     Safe to call even if <paramref name="onUpdate"/> was not previously Subscribed.
        /// </summary>
        /// <param name="onUpdate"></param>
        public void Unsubscribe(System.Action<float> onUpdate)
        {
            _pendingHandlers.Enqueue(() =>
            {
                _subscribers.Remove(onUpdate);
                _subscriberData.Remove(onUpdate);
            });
        }


        /// <summary>
        ///     Each frome, advance all subscribers. Any that have hit their period should act, though if they take to long to do so they can be removed.
        /// </summary>
        private void Update()
        {
            // Add all pending subscriptions.
            while (_pendingHandlers.Count > 0)
            {
                _pendingHandlers.Dequeue()?.Invoke();
            }

            // Check each subscriber for if they are ready to be called.
            foreach(System.Action<float> subscriber in _subscribers)
            {
                SubscriberData subscriberData = _subscriberData[subscriber];

                if (Time.time >= subscriberData.NextCallTime)
                {
                    subscriber.Invoke(Time.time - subscriberData.LastCallTime);
                    subscriberData.LastCallTime = Time.time;
                    subscriberData.NextCallTime = Time.time + subscriberData.Period;
                }
            }
        }
    }
}
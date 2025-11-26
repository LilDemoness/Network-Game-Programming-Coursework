using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Infrastructure
{
    public class MessageChannel<T> : IMessageChannel<T>
    {
        private readonly List<System.Action<T>> _messageHandlers = new List<System.Action<T>>();


        /// <summary>
        ///     This dictionary of handlers to be either added or removed is used to prevent problems from immediate modification of the list of subscribers.<br/>
        ///     It could happen if one decides to unsubscribe in a message handler, etc.<br/>
        ///     A true value maens this handler should be added, and a false one means it should be removed.
        /// </summary>
        private readonly Dictionary<System.Action<T>, bool> _pendingHandlers = new Dictionary<System.Action<T>, bool>();


        public bool IsDisposed { get; private set; } = false;


        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _messageHandlers.Clear();
                _pendingHandlers.Clear();
            }
        }


        public virtual void Publish(T message)
        {
            // Handle pending handlers (Addition and Removal).
            foreach(System.Action<T> handler in _pendingHandlers.Keys)
            {
                if (_pendingHandlers[handler])
                {
                    _messageHandlers.Add(handler);
                }
                else
                {
                    _messageHandlers.Remove(handler);
                }
            }
            _pendingHandlers.Clear();


            // Handle message handlers.
            foreach(System.Action<T> messageHandler in _messageHandlers)
            {
                messageHandler?.Invoke(message);
            }
        }

        public virtual IDisposable Subscribe(System.Action<T> handler)
        {
            Assert.IsTrue(!IsSubscribed(handler), "Attempting to subscribe with the same handler more than once");

            if (_pendingHandlers.ContainsKey(handler))
            {
                if (_pendingHandlers[handler] == false)
                {
                    // We're wanting to both add and remove the handler, so the requests cancel eachother out.
                    _pendingHandlers.Remove(handler);
                }
            }
            else
            {
                // Mark the handler as to be added.
                _pendingHandlers[handler] = true;
            }
            
            DisposableSubscription<T> subscription = new DisposableSubscription<T>(this, handler);
            return subscription;
        }

        public void Unsubscribe(System.Action<T> handler)
        {
            if (!IsSubscribed(handler))
                return;

            if (_pendingHandlers.ContainsKey(handler))
            {
                if (_pendingHandlers[handler])
                {
                    // We're wanting to both add and remove the handler, so the requests cancel eachother out.
                    _pendingHandlers.Remove(handler);
                }
            }
            else
            {
                // Mark the handler as to be removed.
                _pendingHandlers[handler] = false;
            }
        }

        /// <summary>
        ///     Returns true if the handler is subscribed to this message and isn't going to be removed, or is wishing to be subscribed.
        /// </summary>
        private bool IsSubscribed(System.Action<T> handler)
        {
            bool isPendingRemoval = _pendingHandlers.ContainsKey(handler) && !_pendingHandlers[handler];
            bool isPendingAddition = _pendingHandlers.ContainsKey(handler) && _pendingHandlers[handler];
            return (_messageHandlers.Contains(handler) && !isPendingRemoval) || isPendingAddition;
        }
    }
}
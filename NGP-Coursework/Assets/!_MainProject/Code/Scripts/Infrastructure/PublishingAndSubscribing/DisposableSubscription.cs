using System;

namespace Infrastructure
{
    /// <summary>
    ///     This class is a handle to an active <see cref="MessageChannel{T}"/> subscription and when disposed it unsubscribes from said channel.
    /// </summary>
    public class DisposableSubscription<T> : IDisposable
    {
        private System.Action<T> _handler;
        private bool _isDisposed;
        private IMessageChannel<T> _messageChannel;

        public DisposableSubscription(IMessageChannel<T> messageChannel, System.Action<T> handler)
        {
            this._messageChannel = messageChannel;
            this._handler = handler;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (!_messageChannel.IsDisposed)
            {
                _messageChannel.Unsubscribe(_handler);
            }

            _handler = null;
            _messageChannel = null;
        }
    }
}
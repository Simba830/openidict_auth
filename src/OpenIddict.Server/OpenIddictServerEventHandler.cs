﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace OpenIddict.Server
{
    /// <summary>
    /// Represents a handler able to process <typeparamref name="TEvent"/> events.
    /// </summary>
    /// <typeparam name="TEvent">The type of the events handled by this instance.</typeparam>
    public class OpenIddictServerEventHandler<TEvent> : IOpenIddictServerEventHandler<TEvent>
        where TEvent : class, IOpenIddictServerEvent
    {
        private readonly Func<TEvent, CancellationToken, Task> _handler;

        /// <summary>
        /// Creates a new event using the specified handler delegate.
        /// </summary>
        /// <param name="handler">The event handler delegate</param>
        public OpenIddictServerEventHandler([NotNull] Func<TEvent, CancellationToken, Task> handler)
            => _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        /// <summary>
        /// Processes the event.
        /// </summary>
        /// <param name="notification">The event to process.</param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> that can be used to abort the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public Task HandleAsync(TEvent notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return _handler.Invoke(notification, cancellationToken);
        }
    }
}

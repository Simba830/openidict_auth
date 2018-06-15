﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace OpenIddict.Validation
{
    /// <summary>
    /// Dispatches notifications by invoking the corresponding handlers.
    /// </summary>
    public class OpenIddictValidationEventService : IOpenIddictValidationEventService
    {
        private readonly IServiceProvider _provider;

        public OpenIddictValidationEventService([NotNull] IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Publishes a new event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to publish.</typeparam>
        /// <param name="notification">The event to publish.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>A <see cref="Task"/> that can be used to monitor the asynchronous operation.</returns>
        public async Task PublishAsync<TEvent>([NotNull] TEvent notification, CancellationToken cancellationToken = default)
            where TEvent : class, IOpenIddictValidationEvent
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            foreach (var handler in _provider.GetServices<IOpenIddictValidationEventHandler<TEvent>>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                await handler.HandleAsync(notification, cancellationToken);

                // Note: the following logic determines whether next handlers should be invoked
                // depending on whether the underlying event context was substantially updated.
                switch (notification)
                {
                    case OpenIddictValidationEvents.ApplyChallenge value when value.Context.Handled: return;

                    case OpenIddictValidationEvents.CreateTicket value when value.Context.Result != null:    return;
                    case OpenIddictValidationEvents.CreateTicket value when value.Context.Principal == null: return;

                    case OpenIddictValidationEvents.DecryptToken value when value.Context.Result != null:    return;
                    case OpenIddictValidationEvents.DecryptToken value when value.Context.Principal != null: return;

                    case OpenIddictValidationEvents.RetrieveToken value when value.Context.Result != null:    return;
                    case OpenIddictValidationEvents.RetrieveToken value when value.Context.Principal != null: return;

                    case OpenIddictValidationEvents.ValidateToken value when value.Context.Result != null:    return;
                    case OpenIddictValidationEvents.ValidateToken value when value.Context.Principal == null: return;
                }
            }
        }
    }
}

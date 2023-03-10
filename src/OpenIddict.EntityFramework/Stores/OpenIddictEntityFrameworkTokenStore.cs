/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.EntityFramework.Models;
using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace OpenIddict.EntityFramework
{
    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// </summary>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    public class OpenIddictEntityFrameworkTokenStore<TContext> :
        OpenIddictEntityFrameworkTokenStore<OpenIddictEntityFrameworkToken,
                                            OpenIddictEntityFrameworkApplication,
                                            OpenIddictEntityFrameworkAuthorization, TContext, string>
        where TContext : DbContext
    {
        public OpenIddictEntityFrameworkTokenStore(
            IMemoryCache cache,
            TContext context,
            IOptionsMonitor<OpenIddictEntityFrameworkOptions> options)
            : base(cache, context, options)
        {
        }
    }

    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// </summary>
    /// <typeparam name="TToken">The type of the Token entity.</typeparam>
    /// <typeparam name="TApplication">The type of the Application entity.</typeparam>
    /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    /// <typeparam name="TKey">The type of the entity primary keys.</typeparam>
    public class OpenIddictEntityFrameworkTokenStore<TToken, TApplication, TAuthorization, TContext, TKey> : IOpenIddictTokenStore<TToken>
        where TToken : OpenIddictEntityFrameworkToken<TKey, TApplication, TAuthorization>
        where TApplication : OpenIddictEntityFrameworkApplication<TKey, TAuthorization, TToken>
        where TAuthorization : OpenIddictEntityFrameworkAuthorization<TKey, TApplication, TToken>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        public OpenIddictEntityFrameworkTokenStore(
            IMemoryCache cache,
            TContext context,
            IOptionsMonitor<OpenIddictEntityFrameworkOptions> options)
        {
            Cache = cache;
            Context = context;
            Options = options;
        }

        /// <summary>
        /// Gets the memory cache associated with the current store.
        /// </summary>
        protected IMemoryCache Cache { get; }

        /// <summary>
        /// Gets the database context associated with the current store.
        /// </summary>
        protected TContext Context { get; }

        /// <summary>
        /// Gets the options associated with the current store.
        /// </summary>
        protected IOptionsMonitor<OpenIddictEntityFrameworkOptions> Options { get; }

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TApplication"/> entity.
        /// </summary>
        private DbSet<TApplication> Applications => Context.Set<TApplication>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TAuthorization"/> entity.
        /// </summary>
        private DbSet<TAuthorization> Authorizations => Context.Set<TAuthorization>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TToken"/> entity.
        /// </summary>
        private DbSet<TToken> Tokens => Context.Set<TToken>();

        /// <inheritdoc/>
        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
            => await Tokens.LongCountAsync(cancellationToken);

        /// <inheritdoc/>
        public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return await query(Tokens).LongCountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Tokens.Add(token);

            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Tokens.Remove(token);

            try
            {
                await Context.SaveChangesAsync(cancellationToken);
            }

            catch (DbUpdateConcurrencyException exception)
            {
                // Reset the state of the entity to prevents future calls to SaveChangesAsync() from failing.
                Context.Entry(token).State = EntityState.Unchanged;

                throw new OpenIddictExceptions.ConcurrencyException(SR.GetResourceString(SR.ID1246), exception);
            }
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindAsync(string subject,
            string client, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1197), nameof(subject));
            }

            if (string.IsNullOrEmpty(client))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1123), nameof(client));
            }

            var key = ConvertIdentifierFromString(client);

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Application != null &&
                          token.Application.Id!.Equals(key) &&
                          token.Subject == subject
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindAsync(
            string subject, string client,
            string status, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1197), nameof(subject));
            }

            if (string.IsNullOrEmpty(client))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1123), nameof(client));
            }

            if (string.IsNullOrEmpty(status))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1198), nameof(status));
            }

            var key = ConvertIdentifierFromString(client);

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Application != null &&
                          token.Application.Id!.Equals(key) &&
                          token.Subject == subject &&
                          token.Status == status
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindAsync(
            string subject, string client,
            string status, string type, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1197), nameof(subject));
            }

            if (string.IsNullOrEmpty(client))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1123), nameof(client));
            }

            if (string.IsNullOrEmpty(status))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1198), nameof(status));
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1199), nameof(type));
            }

            var key = ConvertIdentifierFromString(client);

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Application != null &&
                          token.Application.Id!.Equals(key) &&
                          token.Subject == subject &&
                          token.Status == status &&
                          token.Type == type
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1194), nameof(identifier));
            }

            var key = ConvertIdentifierFromString(identifier);

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Application != null &&
                          token.Application.Id!.Equals(key)
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1194), nameof(identifier));
            }

            var key = ConvertIdentifierFromString(identifier);

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Authorization != null &&
                          token.Authorization.Id!.Equals(key)
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1194), nameof(identifier));
            }

            var key = ConvertIdentifierFromString(identifier);

            return await (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                          where token.Id!.Equals(key)
                          select token).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1194), nameof(identifier));
            }

            return await (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                          where token.ReferenceId == identifier
                          select token).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID1197), nameof(subject));
            }

            return (from token in Tokens.Include(token => token.Application).Include(token => token.Authorization)
                    where token.Subject == subject
                    select token).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            // If the application is not attached to the token, try to load it manually.
            if (token.Application == null)
            {
                var reference = Context.Entry(token).Reference(entry => entry.Application);
                if (reference.EntityEntry.State == EntityState.Detached)
                {
                    return null;
                }

                await reference.LoadAsync(cancellationToken);
            }

            if (token.Application == null)
            {
                return null;
            }

            return ConvertIdentifierToString(token.Application.Id);
        }

        /// <inheritdoc/>
        public virtual async ValueTask<TResult> GetAsync<TState, TResult>(
            Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
            TState state, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return await query(
                Tokens.Include(token => token.Application)
                      .Include(token => token.Authorization), state).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            // If the authorization is not attached to the token, try to load it manually.
            if (token.Authorization == null)
            {
                var reference = Context.Entry(token).Reference(entry => entry.Authorization);
                if (reference.EntityEntry.State == EntityState.Detached)
                {
                    return null;
                }

                await reference.LoadAsync(cancellationToken);
            }

            if (token.Authorization == null)
            {
                return null;
            }

            return ConvertIdentifierToString(token.Authorization.Id);
        }

        /// <inheritdoc/>
        public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<DateTimeOffset?>(token.CreationDate);
        }

        /// <inheritdoc/>
        public virtual ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<DateTimeOffset?>(token.ExpirationDate);
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(ConvertIdentifierToString(token.Id));
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(token.Payload);
        }

        /// <inheritdoc/>
        public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrEmpty(token.Properties))
            {
                return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary.Create<string, JsonElement>());
            }

            // Note: parsing the stringified properties is an expensive operation.
            // To mitigate that, the resulting object is stored in the memory cache.
            var key = string.Concat("d0509397-1bbf-40e7-97e1-5e6d7bc2536c", "\x1e", token.Properties);
            var properties = Cache.GetOrCreate(key, entry =>
            {
                entry.SetPriority(CacheItemPriority.High)
                     .SetSlidingExpiration(TimeSpan.FromMinutes(1));

                return JsonSerializer.Deserialize<ImmutableDictionary<string, JsonElement>>(token.Properties);
            });

            return new ValueTask<ImmutableDictionary<string, JsonElement>>(properties);
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(token.ReferenceId);
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(token.Status);
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(token.Subject);
        }

        /// <inheritdoc/>
        public virtual ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return new ValueTask<string?>(token.Type);
        }

        /// <inheritdoc/>
        public virtual ValueTask<TToken> InstantiateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return new ValueTask<TToken>(Activator.CreateInstance<TToken>());
            }

            catch (MemberAccessException exception)
            {
                return new ValueTask<TToken>(Task.FromException<TToken>(
                    new InvalidOperationException(SR.GetResourceString(SR.ID1247), exception)));
            }
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TToken> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = Tokens.Include(token => token.Application)
                              .Include(token => token.Authorization)
                              .OrderBy(token => token.Id!)
                              .AsQueryable();

            if (offset.HasValue)
            {
                query = query.Skip(offset.Value);
            }

            if (count.HasValue)
            {
                query = query.Take(count.Value);
            }

            return query.AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
            Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
            TState state, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query(
                Tokens.Include(token => token.Application)
                      .Include(token => token.Authorization), state).AsAsyncEnumerable(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async ValueTask PruneAsync(CancellationToken cancellationToken)
        {
            // Note: Entity Framework 6.x doesn't support set-based deletes, which prevents removing
            // entities in a single command without having to retrieve and materialize them first.
            // To work around this limitation, entities are manually listed and deleted using a batch logic.

            List<Exception>? exceptions = null;

            DbContextTransaction? CreateTransaction()
            {
                // Note: relational providers like Sqlite are known to lack proper support
                // for repeatable read transactions. To ensure this method can be safely used
                // with such providers, the database transaction is created in a try/catch block.
                try
                {
                    return Context.Database.BeginTransaction(IsolationLevel.RepeatableRead);
                }

                catch
                {
                    return null;
                }
            }

            // Note: to avoid sending too many queries, the maximum number of elements
            // that can be removed by a single call to PruneAsync() is limited to 50000.
            for (var offset = 0; offset < 50_000; offset += 1_000)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // To prevent concurrency exceptions from being thrown if an entry is modified
                // after it was retrieved from the database, the following logic is executed in
                // a repeatable read transaction, that will put a lock on the retrieved entries
                // and thus prevent them from being concurrently modified outside this block.
                using var transaction = CreateTransaction();

                var tokens = await (from token in Tokens
                                    where token.Status != OpenIddictConstants.Statuses.Valid ||
                                          token.ExpirationDate < DateTimeOffset.UtcNow
                                    orderby token.Id
                                    select token).Skip(offset).Take(1_000).ToListAsync(cancellationToken);

                if (tokens.Count == 0)
                {
                    break;
                }

                Tokens.RemoveRange(tokens);

                try
                {
                    await Context.SaveChangesAsync(cancellationToken);
                    transaction?.Commit();
                }

                catch (Exception exception)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>(capacity: 1);
                    }

                    exceptions.Add(exception);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(SR.GetResourceString(SR.ID1248), exceptions);
            }
        }

        /// <inheritdoc/>
        public virtual async ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var application = await Applications.FindAsync(cancellationToken, ConvertIdentifierFromString(identifier));
                if (application == null)
                {
                    throw new InvalidOperationException(SR.GetResourceString(SR.ID1249));
                }

                token.Application = application;
            }

            else
            {
                // If the application is not attached to the token, try to load it manually.
                if (token.Application == null)
                {
                    var reference = Context.Entry(token).Reference(entry => entry.Application);
                    if (reference.EntityEntry.State == EntityState.Detached)
                    {
                        return;
                    }

                    await reference.LoadAsync(cancellationToken);
                }

                token.Application = null;
            }
        }

        /// <inheritdoc/>
        public virtual async ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var authorization = await Authorizations.FindAsync(cancellationToken, ConvertIdentifierFromString(identifier));
                if (authorization == null)
                {
                    throw new InvalidOperationException(SR.GetResourceString(SR.ID1250));
                }

                token.Authorization = authorization;
            }

            else
            {
                // If the authorization is not attached to the token, try to load it manually.
                if (token.Authorization == null)
                {
                    var reference = Context.Entry(token).Reference(entry => entry.Authorization);
                    if (reference.EntityEntry.State == EntityState.Detached)
                    {
                        return;
                    }

                    await reference.LoadAsync(cancellationToken);
                }

                token.Authorization = null;
            }
        }

        /// <inheritdoc/>
        public virtual ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.CreationDate = date;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.ExpirationDate = date;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.Payload = payload;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetPropertiesAsync(TToken token,
            ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (properties == null || properties.IsEmpty)
            {
                token.Properties = null;

                return default;
            }

            token.Properties = JsonSerializer.Serialize(properties, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            });

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.ReferenceId = identifier;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.Status = status;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.Subject = subject;

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            token.Type = type;

            return default;
        }

        /// <inheritdoc/>
        public virtual async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Tokens.Attach(token);

            // Generate a new concurrency token and attach it
            // to the token before persisting the changes.
            token.ConcurrencyToken = Guid.NewGuid().ToString();

            Context.Entry(token).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync(cancellationToken);
            }

            catch (DbUpdateConcurrencyException exception)
            {
                // Reset the state of the entity to prevents future calls to SaveChangesAsync() from failing.
                Context.Entry(token).State = EntityState.Unchanged;

                throw new OpenIddictExceptions.ConcurrencyException(SR.GetResourceString(SR.ID1246), exception);
            }
        }

        /// <summary>
        /// Converts the provided identifier to a strongly typed key object.
        /// </summary>
        /// <param name="identifier">The identifier to convert.</param>
        /// <returns>An instance of <typeparamref name="TKey"/> representing the provided identifier.</returns>
        [return: MaybeNull]
        public virtual TKey ConvertIdentifierFromString(string? identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return default;
            }

            return (TKey) TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(identifier);
        }

        /// <summary>
        /// Converts the provided identifier to its string representation.
        /// </summary>
        /// <param name="identifier">The identifier to convert.</param>
        /// <returns>A <see cref="string"/> representation of the provided identifier.</returns>
        public virtual string? ConvertIdentifierToString([AllowNull] TKey identifier)
        {
            if (Equals(identifier, default(TKey)))
            {
                return null;
            }

            return TypeDescriptor.GetConverter(typeof(TKey)).ConvertToInvariantString(identifier);
        }
    }
}
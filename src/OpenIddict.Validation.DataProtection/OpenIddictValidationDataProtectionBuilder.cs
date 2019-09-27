﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using OpenIddict.Validation.DataProtection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Exposes the necessary methods required to configure the
    /// OpenIddict ASP.NET Core Data Protection integration.
    /// </summary>
    public class OpenIddictValidationDataProtectionBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OpenIddictValidationDataProtectionBuilder"/>.
        /// </summary>
        /// <param name="services">The services collection.</param>
        public OpenIddictValidationDataProtectionBuilder([NotNull] IServiceCollection services)
            => Services = services ?? throw new ArgumentNullException(nameof(services));

        /// <summary>
        /// Gets the services collection.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IServiceCollection Services { get; }

        /// <summary>
        /// Amends the default OpenIddict validation ASP.NET Core Data Protection configuration.
        /// </summary>
        /// <param name="configuration">The delegate used to configure the OpenIddict options.</param>
        /// <remarks>This extension can be safely called multiple times.</remarks>
        /// <returns>The <see cref="OpenIddictValidationDataProtectionBuilder"/>.</returns>
        public OpenIddictValidationDataProtectionBuilder Configure([NotNull] Action<OpenIddictValidationDataProtectionOptions> configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Services.Configure(configuration);

            return this;
        }

        /// <summary>
        /// Configures OpenIddict to use a specific data protection provider
        /// instead of relying on the default instance provided by the DI container.
        /// </summary>
        /// <param name="provider">The data protection provider used to create token protectors.</param>
        /// <returns>The <see cref="OpenIddictValidationDataProtectionBuilder"/>.</returns>
        public OpenIddictValidationDataProtectionBuilder UseDataProtectionProvider([NotNull] IDataProtectionProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            return Configure(options => options.DataProtectionProvider = provider);
        }

        /// <summary>
        /// Configures OpenIddict to use a specific formatter instead of relying on the default instance.
        /// </summary>
        /// <param name="formatter">The formatter used to read and write tokens.</param>
        /// <returns>The <see cref="OpenIddictValidationDataProtectionBuilder"/>.</returns>
        public OpenIddictValidationDataProtectionBuilder UseFormatter([NotNull] IOpenIddictValidationDataProtectionFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            return Configure(options => options.Formatter = formatter);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals([CanBeNull] object obj) => base.Equals(obj);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
}

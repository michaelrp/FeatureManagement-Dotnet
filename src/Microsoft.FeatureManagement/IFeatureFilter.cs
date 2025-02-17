﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A filter that can be used to determine whether some criteria is met to enable a feature flag. A feature filter is free to use any criteria available, such as process state or request content. Feature filters can be registered for a given feature flag and if any feature filter evaluates to true, that feature flag will be considered enabled.
    /// </summary>
    public interface IFeatureFilter : IFeatureFilterMetadata
    {
        /// <summary>
        /// Evaluates the feature filter to see if the filter's criteria for being enabled has been satisfied.
        /// </summary>
        /// <param name="context">A feature filter evaluation context that contains information that may be needed to evaluate the filter. This context includes configuration, if any, for this filter for the feature flag being evaluated.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the filter's criteria has been met, false otherwise.</returns>
        Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default);
    }
}

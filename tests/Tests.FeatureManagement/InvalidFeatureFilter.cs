﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    //
    // Cannot implement more than one IFeatureFilter interface
    class InvalidFeatureFilter : IContextualFeatureFilter<IAccountContext>, IContextualFeatureFilter<object>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, object appContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    //
    // Cannot implement more than one IFeatureFilter interface
    class InvalidFeatureFilter2 : IFeatureFilter, IContextualFeatureFilter<object>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, object appContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}

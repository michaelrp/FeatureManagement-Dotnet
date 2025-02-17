﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class OnDemandTargetingContextAccessor : ITargetingContextAccessor
    {
        public TargetingContext Current { get; set; }

        public ValueTask<TargetingContext> GetContextAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<TargetingContext>(Current);
        }
    }
}

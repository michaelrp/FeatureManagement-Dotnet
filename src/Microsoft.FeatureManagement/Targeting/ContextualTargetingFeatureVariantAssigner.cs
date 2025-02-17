﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Assigners
{
    /// <summary>
    /// A feature variant assigner that can be used to assign a variant based on targeted audiences.
    /// </summary>
    [AssignerAlias(Alias)]
    public class ContextualTargetingFeatureVariantAssigner : IContextualFeatureVariantAssigner<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly TargetingEvaluationOptions _options;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        public ContextualTargetingFeatureVariantAssigner(IOptions<TargetingEvaluationOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Assigns one of the variants configured for a dynamic feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAssignmentContext">Contextual information available for use during the assignment process.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, ITargetingContext targetingContext, CancellationToken cancellationToken)
        {
            if (variantAssignmentContext == null)
            {
                throw new ArgumentNullException(nameof(variantAssignmentContext));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            DynamicFeatureDefinition featureDefinition = variantAssignmentContext.FeatureDefinition;

            if (featureDefinition == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAssignmentContext)}.{nameof(variantAssignmentContext.FeatureDefinition)} cannot be null.",
                    nameof(variantAssignmentContext));
            }

            if (featureDefinition.Variants == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAssignmentContext)}.{nameof(variantAssignmentContext.FeatureDefinition)}.{nameof(featureDefinition.Variants)} cannot be null.",
                    nameof(variantAssignmentContext));
            }

            var lookup = new Dictionary<FeatureVariant, TargetingFilterSettings>();

            //
            // Check users
            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                TargetingFilterSettings targetingSettings = v.AssignmentParameters.Get<TargetingFilterSettings>();

                //
                // Put in lookup table to avoid repeatedly creating targeting settings
                lookup[v] = targetingSettings;

                if (targetingSettings == null &&
                    v.Default)
                {
                    //
                    // Valid to omit audience for default variant
                    continue;
                }

                if (!TargetingEvaluator.TryValidateSettings(targetingSettings, out string paramName, out string reason))
                {
                    throw new ArgumentException(reason, paramName);
                }

                //
                // Check if the user is being targeted directly
                if (targetingSettings.Audience.Users != null &&
                    TargetingEvaluator.IsTargeted(
                        targetingContext,
                        targetingSettings.Audience.Users,
                        _options.IgnoreCase))
                {
                    return new ValueTask<FeatureVariant>(v);
                }
            }

            var cumulativeGroups = new Dictionary<string, double>(
                _options.IgnoreCase ? StringComparer.OrdinalIgnoreCase :
                                      StringComparer.Ordinal);

            //
            // Check Groups
            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                TargetingFilterSettings targetingSettings = lookup[v];

                if (targetingSettings == null ||
                    targetingSettings.Audience.Groups == null)
                {
                    continue;
                }

                AccumulateGroups(targetingSettings.Audience.Groups, cumulativeGroups);

                if (TargetingEvaluator.IsTargeted(
                    targetingContext,
                    targetingSettings.Audience.Groups,
                    _options.IgnoreCase,
                    featureDefinition.Name))
                {
                    return new ValueTask<FeatureVariant>(v);
                }
            }

            double cumulativePercentage = 0;

            //
            // Check default rollout percentage
            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                TargetingFilterSettings targetingSettings = lookup[v];

                if (targetingSettings == null)
                {
                    continue;
                }

                AccumulateDefaultRollout(targetingSettings.Audience, ref cumulativePercentage);

                if (TargetingEvaluator.IsTargeted(
                    targetingContext,
                    targetingSettings.Audience.DefaultRolloutPercentage,
                    _options.IgnoreCase,
                    featureDefinition.Name))
                {
                    return new ValueTask<FeatureVariant>(v);
                }
            }

            return new ValueTask<FeatureVariant>((FeatureVariant)null);
        }

        /// <summary>
        /// Accumulates percentages for groups of an audience.
        /// </summary>
        /// <param name="groups">The groups that will have their percentages updated based on currently accumulated percentages</param>
        /// <param name="cumulativeGroups">The current cumulative rollout percentage for each group</param>
        private static void AccumulateGroups(IEnumerable<GroupRollout> groups, Dictionary<string, double> cumulativeGroups)
        {
            foreach (GroupRollout gr in groups)
            {
                double percentage = gr.RolloutPercentage;

                if (cumulativeGroups.TryGetValue(gr.Name, out double p))
                {
                    percentage += p;
                }

                cumulativeGroups[gr.Name] = percentage;

                gr.RolloutPercentage = percentage;
            }
        }

        /// <summary>
        /// Accumulates percentages for the  default rollout of an audience.
        /// </summary>
        /// <param name="audience">The audience that will have its percentages updated based on currently accumulated percentages</param>
        /// <param name="cumulativeDefaultPercentage">The current cumulative default rollout percentage</param>
        private static void AccumulateDefaultRollout(Audience audience, ref double cumulativeDefaultPercentage)
        {
            cumulativeDefaultPercentage = cumulativeDefaultPercentage + audience.DefaultRolloutPercentage;

            audience.DefaultRolloutPercentage = cumulativeDefaultPercentage;
        }
    }
}

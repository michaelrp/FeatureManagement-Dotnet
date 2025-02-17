﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService.FeatureManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Consoto.Banking.AccountService
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //
            // Setup configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //
            // Setup application services + feature management
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(configuration)
                    .AddFeatureManagement()
                    .AddFeatureFilter<PercentageFilter>()
                    .AddFeatureFilter<AccountIdFilter>();

            //
            // Get the feature manager from application services
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

                var accounts = new List<string>()
                {
                    "abc",
                    "adef",
                    "abcdefghijklmnopqrstuvwxyz"
                };

                //
                // Mimic work items in a task-driven console application
                foreach (var account in accounts)
                {
                    const string FeatureName = "Beta";

                    //
                    // Check if feature enabled
                    //
                    var accountServiceContext = new AccountServiceContext
                    {
                        AccountId = account
                    };

                    bool enabled = await featureManager.IsEnabledAsync(FeatureName, accountServiceContext, CancellationToken.None);

                    //
                    // Output results
                    Console.WriteLine($"The {FeatureName} feature is {(enabled ? "enabled" : "disabled")} for the '{account}' account.");
                }
            }
        }
    }
}

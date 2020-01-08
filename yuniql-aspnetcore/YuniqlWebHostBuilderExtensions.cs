﻿using Microsoft.AspNetCore.Hosting;
using Yuniql.Core;
using Yuniql.Extensibility;

namespace Yuniql.AspNetCore
{
    public static class YuniqlWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseYuniql(
            this IWebHostBuilder builder,
            YuniqlConfiguration configuration
        )
        {
            var traceService = new FileTraceService { IsDebugEnabled = configuration.DebugTraceMode };
            return builder.UseYuniql(traceService, configuration);
        }

        public static IWebHostBuilder UseYuniql(
            this IWebHostBuilder builder,
            ITraceService traceService,
            YuniqlConfiguration configuration
        )
        {
            var migrationServiceFactory = new MigrationServiceFactory(traceService);
            var migrationService = migrationServiceFactory.Create(configuration.Platform);
            migrationService.Initialize(configuration.ConnectionString);
            migrationService.Run(
                configuration.WorkspacePath,
                configuration.TargetVersion,
                configuration.AutoCreateDatabase,
                configuration.Tokens,
                configuration.VerifyOnly,
                configuration.Delimiter);

            return builder;
        }
    }
}
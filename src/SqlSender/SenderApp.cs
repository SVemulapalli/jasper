﻿using Baseline.Dates;
using Jasper;
using Jasper.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SqlSender
{
    public class SenderApp : JasperRegistry
    {
        public SenderApp()
        {
            Hosting.ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json").AddEnvironmentVariables();
            });

            Hosting.UseUrls("http://*:5060").UseKestrel();

            Hosting.ConfigureLogging(x =>
            {
                x.SetMinimumLevel(LogLevel.Error);
                x.AddConsole();
            });

            Settings.Alter<JasperOptions>(x => x.Retries.NodeReassignmentPollingTime = 5.Seconds());

            Settings.PersistMessagesWithSqlServer((context, settings) =>
            {
                settings.ConnectionString = context.Configuration["mssql"];
                settings.SchemaName = "sender";
            });


            Settings.Configure(c =>
            {
                Transports.ListenForMessagesFrom(c.Configuration["listener"]);
                Publish.AllMessagesTo(c.Configuration["receiver"]);
            });
        }
    }
}

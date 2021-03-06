﻿using System;
using Jasper.Configuration;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports;
using Jasper.Persistence.Marten.Persistence;
using Jasper.Persistence.Marten.Persistence.DbObjects;
using Jasper.Persistence.Marten.Persistence.Sagas;
using Jasper.Persistence.Marten.Resiliency;
using LamarCompiler.Model;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jasper.Persistence.Marten
{
    /// <summary>
    ///     Opts into using Marten as the backing message store
    /// </summary>
    public class MartenBackedPersistence : IJasperExtension
    {
        public void Configure(JasperOptionsBuilder registry)
        {
            registry.Services.AddTransient<IEnvelopePersistor, MartenEnvelopePersistor>();
            registry.Services.AddSingleton<IDurableMessagingFactory, MartenBackedDurableMessagingFactory>();
            registry.Settings.Alter<StoreOptions>(options =>
            {
                options.Storage.Add<PostgresqlEnvelopeStorage>();
                options.Schema.For<ErrorReport>().Duplicate(x => x.MessageType).Duplicate(x => x.ExceptionType);
            });

            registry.Services.AddSingleton<IHostedService, SchedulingAgent>();

            registry.CodeGeneration.Sources.Add(new MartenBackedPersistenceMarker());

            var frameProvider = new MartenSagaPersistenceFrameProvider();
            registry.CodeGeneration.SagaPersistence = frameProvider;
            registry.CodeGeneration.Transactions = frameProvider;
        }
    }

    internal static class MethodVariablesExtensions
    {
        internal static bool IsUsingMartenPersistence(this IMethodVariables method)
        {
            return method.TryFindVariable(typeof(MartenBackedPersistenceMarker), VariableSource.NotServices) != null;
        }
    }

    internal class MartenBackedPersistenceMarker : IVariableSource
    {
        public bool Matches(Type type)
        {
            return type == GetType();
        }

        public Variable Create(Type type)
        {
            return Variable.For<MartenBackedDurableMessagingFactory>();
        }
    }
}

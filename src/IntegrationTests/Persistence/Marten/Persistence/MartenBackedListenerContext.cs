﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports;
using Jasper.Messaging.Transports.Receiving;
using Jasper.Messaging.Transports.Tcp;
using Jasper.Messaging.WorkerQueues;
using Jasper.Persistence.Marten;
using Jasper.Persistence.Marten.Persistence;
using Jasper.Persistence.Marten.Persistence.DbObjects;
using Jasper.Persistence.Marten.Persistence.Operations;
using Jasper.Util;
using Marten;
using NSubstitute;
using Shouldly;
using Xunit;

namespace IntegrationTests.Persistence.Marten.Persistence
{
    public class MartenBackedListenerTests : MartenBackedListenerContext
    {
        [Fact]
        public async Task handling_a_single_not_scheduled_envelope()
        {
            var envelope = notScheduledEnvelope();
            var persisted = (await afterReceivingTheEnvelopes()).Single();

            persisted.Status.ShouldBe(TransportConstants.Incoming);
            persisted.OwnerId.ShouldBe(theSettings.UniqueNodeId);
            persisted.ReceivedAt.ShouldBe(theUri);

            assertEnvelopeWasEnqueued(envelope);
        }

        [Fact]
        public async Task handling_a_single_scheduled_but_expired_envelope()
        {
            var envelope = scheduledButExpiredEnvelope();
            var persisted = (await afterReceivingTheEnvelopes()).Single();

            persisted.Status.ShouldBe(TransportConstants.Incoming);
            persisted.OwnerId.ShouldBe(theSettings.UniqueNodeId);
            persisted.ReceivedAt.ShouldBe(theUri);

            assertEnvelopeWasEnqueued(envelope);
        }

        [Fact]
        public async Task handling_a_single_scheduled_envelope()
        {
            var envelope = scheduledEnvelope();
            var persisted = (await afterReceivingTheEnvelopes()).Single();

            persisted.Status.ShouldBe(TransportConstants.Scheduled);
            persisted.OwnerId.ShouldBe(TransportConstants.AnyNode);
            persisted.ReceivedAt.ShouldBe(theUri);

            assertEnvelopeWasNotEnqueued(envelope);
        }
    }

    public class MartenBackedListenerContext : MartenContext, IDisposable
    {
        protected readonly IList<Envelope> theEnvelopes = new List<Envelope>();
        protected readonly DocumentStore theStore;
        protected readonly Uri theUri = "tcp://localhost:1111".ToUri();
        protected DurableListener theListener;
        protected JasperOptions theSettings;
        protected IWorkerQueue theWorkerQueue;


        public MartenBackedListenerContext()
        {
            theStore = DocumentStore.For(_ =>
            {
                _.Connection(Servers.PostgresConnectionString);
                _.PLV8Enabled = false;
                _.Storage.Add<PostgresqlEnvelopeStorage>();
            });

            theStore.Advanced.Clean.CompletelyRemoveAll();

            theStore.Schema.ApplyAllConfiguredChangesToDatabase();

            theWorkerQueue = Substitute.For<IWorkerQueue>();

            theSettings = new JasperOptions();

            var tables = new EnvelopeTables(theSettings, new StoreOptions());

            var retries = new EnvelopeRetries(new MartenEnvelopePersistor(theStore, tables), TransportLogger.Empty(),
                theSettings);


            theListener = new DurableListener(
                Substitute.For<IListeningAgent>(),
                theWorkerQueue,
                TransportLogger.Empty(), theSettings, retries, new MartenEnvelopePersistor(theStore, tables));
        }

        public void Dispose()
        {
            theStore?.Dispose();
        }

        protected Envelope notScheduledEnvelope()
        {
            var env = new Envelope
            {
                Data = new byte[] {1, 2, 3, 4}
            };

            theEnvelopes.Add(env);

            return env;
        }

        protected Envelope scheduledEnvelope()
        {
            var env = new Envelope
            {
                Data = new byte[] {1, 2, 3, 4},
                ExecutionTime = DateTime.UtcNow.Add(1.Hours())
            };

            theEnvelopes.Add(env);

            return env;
        }

        protected Envelope scheduledButExpiredEnvelope()
        {
            var env = new Envelope
            {
                Data = new byte[] {1, 2, 3, 4},
                ExecutionTime = DateTime.UtcNow.Add(-1.Hours())
            };

            theEnvelopes.Add(env);

            return env;
        }

        protected async Task<IReadOnlyList<Envelope>> afterReceivingTheEnvelopes()
        {
            var status = await theListener.Received(theUri, theEnvelopes.ToArray());

            status.ShouldBe(ReceivedStatus.Successful);

            using (var session = theStore.QuerySession())
            {
                return session.AllIncomingEnvelopes();
            }
        }

        protected void assertEnvelopeWasEnqueued(Envelope envelope)
        {
            theWorkerQueue.Received().Enqueue(envelope);
        }

        protected void assertEnvelopeWasNotEnqueued(Envelope envelope)
        {
            theWorkerQueue.DidNotReceive().Enqueue(envelope);
        }
    }
}

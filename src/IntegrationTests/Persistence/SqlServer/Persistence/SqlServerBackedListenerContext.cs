﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports.Receiving;
using Jasper.Messaging.Transports.Tcp;
using Jasper.Messaging.WorkerQueues;
using Jasper.Persistence.SqlServer;
using Jasper.Persistence.SqlServer.Persistence;
using Jasper.Persistence.SqlServer.Schema;
using Jasper.Util;
using NSubstitute;
using Shouldly;

namespace IntegrationTests.Persistence.SqlServer.Persistence
{
    public class SqlServerBackedListenerContext : SqlServerContext
    {
        protected readonly IList<Envelope> theEnvelopes = new List<Envelope>();
        protected readonly Uri theUri = "tcp://localhost:1111".ToUri();
        protected SqlServerSettings mssqlSettings;
        protected EnvelopeRetries retries;
        protected DurableListener theListener;
        protected SqlServerEnvelopePersistor thePersistor;
        protected JasperOptions theSettings;
        protected IWorkerQueue theWorkerQueue;


        public SqlServerBackedListenerContext()
        {
            new SchemaLoader(Servers.SqlServerConnectionString).RecreateAll();

            theWorkerQueue = Substitute.For<IWorkerQueue>();

            theSettings = new JasperOptions();

            mssqlSettings = new SqlServerSettings
            {
                ConnectionString = Servers.SqlServerConnectionString
            };

            thePersistor = new SqlServerEnvelopePersistor(mssqlSettings);

            retries = new EnvelopeRetries(thePersistor, TransportLogger.Empty(), theSettings);


            theListener = new DurableListener(
                Substitute.For<IListeningAgent>(),
                theWorkerQueue,
                TransportLogger.Empty(), theSettings, retries, thePersistor);
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

            return thePersistor.AllIncomingEnvelopes();
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

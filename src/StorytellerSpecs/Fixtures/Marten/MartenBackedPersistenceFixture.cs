﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using IntegrationTests;
using Jasper;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Tracking;
using Jasper.Persistence.Marten.Persistence.DbObjects;
using Jasper.Storyteller.Logging;
using Marten;
using Marten.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StorytellerSpecs.Fixtures.Marten.App;
using StoryTeller;
using StoryTeller.Grammars.Tables;

namespace StorytellerSpecs.Fixtures.Marten
{
    public class MartenBackedPersistenceFixture : Fixture
    {
        private StorytellerMessageLogger _messageLogger;

        private LightweightCache<string, JasperRuntime> _receivers;
        private DocumentStore _receiverStore;

        private LightweightCache<string, JasperRuntime> _senders;
        private SenderLatchDetected _senderWatcher;
        private DocumentStore _sendingStore;

        public MartenBackedPersistenceFixture()
        {
            Title = "Marten-Backed Durable Messaging";
        }

        public override void SetUp()
        {
            _messageLogger =
                new StorytellerMessageLogger(new MessageHistory(), new LoggerFactory(), new NulloMetrics());

            _messageLogger.Start(Context);

            _senderWatcher = new SenderLatchDetected(new LoggerFactory());

            _receiverStore = DocumentStore.For(_ =>
            {
                _.PLV8Enabled = false;
                _.Connection(Servers.PostgresConnectionString);
                _.DatabaseSchemaName = "receiver";
                _.Storage.Add<PostgresqlEnvelopeStorage>();


                _.Schema.For<TraceDoc>();
            });

            _receiverStore.Advanced.Clean.CompletelyRemoveAll();
            _receiverStore.Schema.ApplyAllConfiguredChangesToDatabase();


            _sendingStore = DocumentStore.For(_ =>
            {
                _.PLV8Enabled = false;
                _.Connection(Servers.PostgresConnectionString);
                _.DatabaseSchemaName = "sender";

                _.Storage.Add<PostgresqlEnvelopeStorage>();
            });

            _sendingStore.Advanced.Clean.CompletelyRemoveAll();
            _sendingStore.Schema.ApplyAllConfiguredChangesToDatabase();

            _receivers = new LightweightCache<string, JasperRuntime>(key =>
            {
                var registry = new ReceiverApp();
                registry.Services.AddSingleton<IMessageLogger>(_messageLogger);

                registry.Hosting.ConfigureLogging(x =>
                {
                    x.AddConsole();
                    x.AddDebug();
                });

                return JasperRuntime.For(registry);
            });

            _senders = new LightweightCache<string, JasperRuntime>(key =>
            {
                var registry = new SenderApp();
                registry.Services.AddSingleton<IMessageLogger>(_messageLogger);

                registry.Services.For<ITransportLogger>().Use(_senderWatcher);


                registry.Hosting.ConfigureLogging(x =>
                {
                    x.AddConsole();
                    x.AddDebug();
                });

                return JasperRuntime.For(registry);
            });
        }

        public override void TearDown()
        {
            _receivers.Each(x => x.Dispose());
            _receivers.ClearAll();

            _senders.Each(x => x.Dispose());
            _senders.ClearAll();

            _messageLogger.BuildReports().Each(x => Context.Reporting.Log(x));

            _receiverStore.Dispose();
            _receiverStore = null;
            _sendingStore.Dispose();
            _sendingStore = null;
        }

        [FormatAs("Start receiver node {name}")]
        public void StartReceiver(string name)
        {
            _receivers.FillDefault(name);
        }

        [FormatAs("Start sender node {name}")]
        public void StartSender([Default("Sender1")] string name)
        {
            _senderWatcher.Reset();
            _senders.FillDefault(name);
        }

        [ExposeAsTable("Send Messages")]
        public Task SendFrom([Header("Sending Node")] [Default("Sender1")]
            string sender, [Header("Message Name")] string name)
        {
            return _senders[sender].Messaging.Send(new TraceMessage {Name = name});
        }

        [FormatAs("Send {count} messages from {sender}")]
        public async Task SendMessages([Default("Sender1")] string sender, int count)
        {
            var runtime = _senders[sender];

            for (var i = 0; i < count; i++)
            {
                var msg = new TraceMessage {Name = Guid.NewGuid().ToString()};
                await runtime.Messaging.Send(msg);
            }
        }

        [FormatAs("The persisted document count in the receiver should be {count}")]
        public int ReceivedMessageCount()
        {
            using (var session = _receiverStore.LightweightSession())
            {
                return session.Query<TraceDoc>().Count();
            }
        }


        [FormatAs("Wait for {count} messages to be processed by the receivers")]
        public async Task WaitForMessagesToBeProcessed(int count)
        {
            using (var session = _receiverStore.QuerySession())
            {
                for (var i = 0; i < 200; i++)
                {
                    var actual = session.Query<TraceDoc>().Count();
                    var envelopeCount = PersistedIncomingCount();


                    if (actual == count && envelopeCount == 0) return;

                    await Task.Delay(250);
                }
            }

            StoryTellerAssert.Fail("All messages were not received");
        }

        [FormatAs("There should be {count} persisted, incoming messages in the receiver storage")]
        public long PersistedIncomingCount()
        {
            using (var conn = _receiverStore.Tenancy.Default.CreateConnection())
            {
                conn.Open();

                return (long) conn.CreateCommand(
                        $"select count(*) from receiver.{PostgresqlEnvelopeStorage.IncomingTableName}")
                    .ExecuteScalar();
            }
        }

        [FormatAs("There should be {count} persisted, outgoing messages in the sender storage")]
        public long PersistedOutgoingCount()
        {
            using (var conn = _sendingStore.Tenancy.Default.CreateConnection())
            {
                conn.Open();

                return (long) conn.CreateCommand(
                        $"select count(*) from sender.{PostgresqlEnvelopeStorage.OutgoingTableName}")
                    .ExecuteScalar();
            }
        }

        [FormatAs("Receiver node {name} stops")]
        public void StopReceiver(string name)
        {
            _receivers[name].Dispose();
            _receivers.Remove(name);
        }

        [FormatAs("Sender node {name} stops")]
        public void StopSender(string name)
        {
            _senders[name].Dispose();
            _senders.Remove(name);
        }
    }


    public class SenderLatchDetected : TransportLogger
    {
        public TaskCompletionSource<bool> Waiter = new TaskCompletionSource<bool>();

        public SenderLatchDetected(ILoggerFactory factory) : base(factory, new NulloMetrics())
        {
        }

        public Task<bool> Received => Waiter.Task;

        public override void CircuitResumed(Uri destination)
        {
            if (destination == ReceiverApp.Listener) Waiter.TrySetResult(true);

            base.CircuitResumed(destination);
        }

        public override void CircuitBroken(Uri destination)
        {
            if (destination == ReceiverApp.Listener) Reset();

            base.CircuitBroken(destination);
        }

        public void Reset()
        {
            Waiter = new TaskCompletionSource<bool>();
        }
    }
}

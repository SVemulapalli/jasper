﻿using System;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper.Bus.ErrorHandling;
using Jasper.Bus.Runtime.Invocation;
using Jasper.Bus.Transports;
using NSubstitute;
using Xunit;

namespace Jasper.Testing.Bus.ErrorHandling
{
    public class ScheduledRetryContinuationTester
    {
        [Fact]
        public async Task do_as_a_delay_w_the_timespan_given()
        {
            var continuation = new ScheduledRetryContinuation(5.Minutes());
            var context = Substitute.For<IEnvelopeContext>();


            var envelope = ObjectMother.Envelope();
            envelope.Callback = Substitute.For<IMessageCallback>();

            var now = DateTime.Today.ToUniversalTime();

            await continuation.Execute(envelope, context, now);


            envelope.Callback.Received().MoveToScheduledUntil(now.AddMinutes(5), envelope);

        }
    }
}

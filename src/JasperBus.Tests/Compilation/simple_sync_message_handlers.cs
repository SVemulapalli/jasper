﻿using System.Threading.Tasks;
using JasperBus.Tests.Runtime;
using Shouldly;
using Xunit;

namespace JasperBus.Tests.Compilation
{
    public class simple_sync_message_handlers : CompilationContext<SyncHandler>
    {
        [Fact]
        public void can_compile_all()
        {
            AllHandlersCompileSuccessfully();
        }

        [Fact]
        public async Task execute_the_simplest_possible_chain()
        {
            var message = new Message1();
            await Execute(message);

            SyncHandler.LastMessage1.ShouldBeSameAs(message);
        }
    }

    public class SyncHandler
    {
        public static Message1 LastMessage1;

        public static void Simple1(Message1 message)
        {
            LastMessage1 = message;
        }
    }
}
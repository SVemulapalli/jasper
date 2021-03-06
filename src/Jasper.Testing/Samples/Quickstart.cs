﻿using System;
using Jasper.Http;
using Microsoft.AspNetCore.Hosting;

namespace Jasper.Testing.Samples
{
    // SAMPLE: MyAppRegistry
    public class MyAppRegistry : JasperRegistry
    {
    }
    // ENDSAMPLE


    // SAMPLE: ServiceBusApp
    public class ServiceBusApp : JasperRegistry
    {
    }
    // ENDSAMPLE

    public static class Program
    {
        public static void EntryPoint()
        {
            // SAMPLE: QuickStart-Add-To-AspNetCore
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseJasper<ServiceBusApp>()
                .Build();

            host.Run();
            // ENDSAMPLE
        }
    }

    // SAMPLE: QuickStart-InvoiceCreated
    public class InvoiceCreated
    {
        public Guid InvoiceId { get; set; }
    }

    public class InvoiceHandler
    {
        public void Handle(InvoiceCreated created)
        {
            // do something here with the created variable...
        }
    }

    // ENDSAMPLE
}

namespace Jasper.Testing.Samples2
{
    // SAMPLE: MyAppRegistry2
    public class MyAppRegistry : JasperRegistry
    {
        public MyAppRegistry()
        {
            Hosting.UseKestrel();
        }
    }

    // ENDSAMPLE
}

﻿using System.Net.Http;
using System.Threading.Tasks;
using Alba;
using Jasper.Http;
using Jasper.Messaging;
using Jasper.TestSupport.Alba;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Http.AspNetCoreIntegration
{
    public class can_bootstrap_a_bus_plus_aspnetcore_app_through_jasper_registry
    {
        [Fact]
        public async Task can_delegate_to_mvc_route_through_Kestrel()
        {
            using (var theRuntime = JasperAlba.For<JasperServerApp>())
            {
                await theRuntime.Scenario(x =>
                {
                    x.Get.Url("/values/5");
                    x.ContentShouldContain("5");
                });
            }
        }

    }

    public class SomeHandler
    {
        public void Handle(SomeMessage message)
        {
        }
    }

    public class SomeMessage
    {
    }

    // SAMPLE: ConfiguringAspNetCoreWithinJasperRegistry
    public class JasperServerApp : JasperRegistry
    {
        public JasperServerApp()
        {
            Handlers.Discovery(x => x.DisableConventionalDiscovery());

            Hosting
                .UseKestrel()
                .UseUrls("http://localhost:5200")
                .UseStartup<Startup>();
        }
    }
    // ENDSAMPLE

    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseJasper();
            app.UseMvc();
            app.Run(c => c.Response.WriteAsync("Hello from a hybrid Jasper application"));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddApplicationPart(GetType().Assembly);


            services.AddDbContext<ApplicationDbContext>(opts => { opts.UseSqlServer("some connection string"); });
            services.AddTransient<IFoo, Foo>();
        }
    }

    public interface IFoo
    {
    }

    public class Foo : IFoo
    {
        public Foo(ApplicationDbContext context)
        {
            Context = context;
        }

        public ApplicationDbContext Context { get; }
    }

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}

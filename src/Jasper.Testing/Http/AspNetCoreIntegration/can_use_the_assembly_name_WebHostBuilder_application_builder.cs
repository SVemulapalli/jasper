using AspNetCoreHosted;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Http.AspNetCoreIntegration
{
    public class can_use_the_assembly_name_WebHostBuilder_application_builder
    {
        [Fact]
        public void can_use_the_app_assembly_coming_from_the_web_host_builder()
        {
            using (var host = Program.CreateWebHostBuilder(new string[0]).Start())
            {
                var builder = host.Services.GetRequiredService<JasperOptionsBuilder>();
                builder.ApplicationAssembly.ShouldBe(typeof(AspNetCoreHosted.Startup).Assembly);
            }



            //var assemblyName = builder.GetSetting(WebHostDefaults.ApplicationKey);

        }
    }
}

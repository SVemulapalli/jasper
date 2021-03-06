using Jasper;
using Jasper.CommandLine;
using Microsoft.AspNetCore.Hosting;
using TestMessages;

namespace Subscriber
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return JasperAgent.Run<SubscriberApp>(args);
        }
    }

    public class SubscriberApp : JasperRegistry
    {
        public SubscriberApp()
        {
            Hosting.UseUrls("http://localhost:5004");

            Transports.LightweightListenerAt(22222);
        }
    }


    public class NewUserHandler
    {
        public void Handle(NewUser newGuy)
        {
        }

        public void Handle(DeleteUser deleted)
        {
        }
    }
}

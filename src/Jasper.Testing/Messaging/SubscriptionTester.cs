using Green;
using Jasper.Messaging.Runtime.Routing;
using Red;
using Shouldly;
using TestMessages;
using Xunit;

namespace Jasper.Testing.Messaging.Runtime.Routing
{
    public class SubscriptionTester
    {
        [Fact]
        public void negative_assembly_test()
        {
            var rule = new Subscription(typeof(NewUser).Assembly);
            rule.Matches(typeof(Message1)).ShouldBeFalse();
            rule.Matches(typeof(Message2)).ShouldBeFalse();
            rule.Matches(GetType()).ShouldBeFalse();
        }

        [Fact]
        public void negative_namespace_test()
        {
            var rule = new Subscription
            {
                Scope = RoutingScope.Namespace,
                Match = typeof(RedMessage1).Namespace
            };

            rule.Matches(typeof(GreenMessage1)).ShouldBeFalse();
            rule.Matches(typeof(GreenMessage2)).ShouldBeFalse();
            rule.Matches(typeof(GreenMessage3)).ShouldBeFalse();
        }

        [Fact]
        public void positive_assembly_test()
        {
            var rule = new Subscription(typeof(NewUser).Assembly);

            rule.Matches(typeof(NewUser)).ShouldBeTrue();
            rule.Matches(typeof(EditUser)).ShouldBeTrue();
            rule.Matches(typeof(DeleteUser)).ShouldBeTrue();
        }

        [Fact]
        public void positive_namespace_test()
        {
            var rule = new Subscription
            {
                Scope = RoutingScope.Namespace,
                Match = typeof(RedMessage1).Namespace
            };

            rule.Matches(typeof(RedMessage1)).ShouldBeTrue();
            rule.Matches(typeof(RedMessage2)).ShouldBeTrue();
            rule.Matches(typeof(RedMessage3)).ShouldBeTrue();
        }
    }
}

namespace Red
{
    public class RedMessage1
    {
    }

    public class RedMessage2
    {
    }

    public class RedMessage3
    {
    }
}

namespace Green
{
    public class GreenMessage1
    {
    }

    public class GreenMessage2
    {
    }

    public class GreenMessage3
    {
    }
}
